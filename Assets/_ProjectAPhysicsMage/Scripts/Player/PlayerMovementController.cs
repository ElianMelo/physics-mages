using FishNet.Object;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Rigidbody-based movement controller.
///
/// Setup requirements:
///   • Attach a Rigidbody to the root object.
///       – Freeze Rotation X/Y/Z in the Inspector (rotation is handled manually).
///       – Collision Detection: Continuous Dynamic.
///   • The root object should carry a small CapsuleCollider (or SphereCollider) that
///     covers only the hip/torso area — just enough to prevent clipping through the floor.
///     Limb colliders live on the ragdoll rig child objects; this script doesn't touch them.
///   • Set the groundLayer mask in the Inspector so the SphereCast only hits geometry.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementController : NetworkBehaviour
{
    // ─── Movement ────────────────────────────────────────────────────────────
    [Header("Movement")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 10f;

    [Header("Dash")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.25f;
    public float dashCooldown = 1.0f;
    public float dashSpeedChangeFactor = 8f;

    [Header("Jump & Gravity")]
    public float jumpForce = 6f;
    /// <summary>
    /// Extra downward force applied every FixedUpdate while airborne.
    /// Works on top of Physics.gravity — tune this alongside that global setting.
    /// </summary>
    public float extraGravity = 20f;

    [Header("Drag")]
    public float groundDrag = 10f;
    public float airDrag = 1f;

    [Header("Air Control")]
    [Range(0f, 1f)]
    public float airMultiplier = 0.4f;

    [Header("Slope Handling")]
    public float maxSlopeAngle = 45f;

    [Header("Rotation")]
    public float smoothFollowMoveDirectionFactor = 0.08f;

    [Header("Ground Detection")]
    /// <summary>
    /// Centre of the ground-check sphere in local space.
    /// Place this at the character's foot level.
    /// </summary>
    public Vector3 groundCheckOffset = new Vector3(0f, -0.9f, 0f);
    public float groundCheckRadius = 0.25f;
    /// <summary>How far below groundCheckOffset to cast before giving up.</summary>
    public float groundCheckDistance = 0.15f;
    public LayerMask groundLayer;

    // ─── References ──────────────────────────────────────────────────────────
    private Rigidbody _rb;
    private Animator _animator;
    private PlayerRagdollController _ragdoll;
    private PlayerInput _playerInput;
    private Camera _mainCamera;

    // ─── Input ───────────────────────────────────────────────────────────────
    private Vector2 _inputDir;

    // ─── State flags ─────────────────────────────────────────────────────────
    private bool _canMove = true;
    private bool _isGrounded;
    private bool _jumping;
    private bool _dashRequested;
    private float _dashCooldownTimer;

    public bool dashing { get; private set; }
    public bool diving;

    // ─── Slope normal ────────────────────────────────────────────────────────
    private Vector3 _groundNormal = Vector3.up;

    // ─── Movement state ──────────────────────────────────────────────────────
    public MovementState state;
    public enum MovementState { idle, running, dashing, airing }

    // ─── Speed lerp ──────────────────────────────────────────────────────────
    private float _moveSpeed;
    private float _desiredMoveSpeed;
    private float _lastDesiredMoveSpeed;
    private MovementState _lastState;
    private bool _keepMomentum;
    private float _speedChangeFactor;
    private IEnumerator _lerpCoroutine;

    // ─── Rotation SmoothDamp ─────────────────────────────────────────────────
    private Vector3 _smoothDampVel;

    // ─── Animator hashes ─────────────────────────────────────────────────────
    private static readonly int AnimJump = Animator.StringToHash("Jump");
    private static readonly int AnimDash = Animator.StringToHash("Dash");
    private static readonly int AnimRunning = Animator.StringToHash("Running");
    private static readonly int AnimFalling = Animator.StringToHash("Falling");
    private static readonly int AnimVerticalVelocity = Animator.StringToHash("VerticalVelocity");
    private static readonly int AnimDashing = Animator.StringToHash("Dashing");
    private static readonly int AnimTakeDamage = Animator.StringToHash("TakeDamage");
    private static readonly int AnimDeath = Animator.StringToHash("Death");

    // ─── Public accessors ────────────────────────────────────────────────────
    public bool IsGrounded => _isGrounded;

    public bool CanMove
    {
        get => _canMove;
        set
        {
            _canMove = value;
            if (!value) ResetMovement();
        }
    }

    // =========================================================================
    //  Unity lifecycle
    // =========================================================================

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _ragdoll = GetComponent<PlayerRagdollController>();

        // Prevent the physics engine from tumbling the character.
        // Rotation is controlled manually in SmoothRotateToCamera().
        _rb.freezeRotation = true;
    }

    private void Start()
    {
        _mainCamera = Camera.main;
        _moveSpeed = walkSpeed;
    }

    public override void OnStartClient()
    {
        if (!IsOwner) return;

        _playerInput = GetComponent<PlayerInput>();
        _playerInput.enabled = true;

        PlayerFollower follower = FindAnyObjectByType<PlayerFollower>();
        if (follower != null) follower.SetupPlayer(transform);
    }

    // =========================================================================
    //  Update / FixedUpdate
    // =========================================================================

    private void Update()
    {
        if (!IsOwner) return;

        // ── Debug ragdoll triggers (remove in production) ──────────────────
        if (Keyboard.current.iKey.wasPressedThisFrame) _ragdoll.TriggerFall(Vector3.up, 20f);
        if (Keyboard.current.jKey.wasPressedThisFrame) _ragdoll.TriggerFall(Vector3.left, 20f);
        if (Keyboard.current.lKey.wasPressedThisFrame) _ragdoll.TriggerFall(Vector3.right, 20f);
        if (Keyboard.current.kKey.wasPressedThisFrame) _ragdoll.TriggerFall(Vector3.down, 20f);

        if (_ragdoll.IsStaggered) return;

        _dashCooldownTimer -= Time.deltaTime;

        HandleJumpInput();

        if (_dashRequested && !dashing && !diving && _dashCooldownTimer <= 0f)
            StartCoroutine(DashCoroutine());
        _dashRequested = false;

        StateHandler();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;
        if (_ragdoll.IsStaggered) return;

        CheckGround();

        if (dashing || diving) return;

        ApplyExtraGravity();
        Move();
        SmoothRotateToCamera();
    }

    // =========================================================================
    //  Input (PlayerInput → Send Messages)
    // =========================================================================

    public void OnMove(InputValue value)
        => _inputDir = value.Get<Vector2>();

    public void OnJump(InputValue value)
    {
        if (value.isPressed && _isGrounded && !_jumping)
            Jump();
    }

    public void OnDash(InputValue value)
    {
        if (value.isPressed) _dashRequested = true;
    }

    // =========================================================================
    //  Ground detection
    // =========================================================================

    /// <summary>
    /// Casts a sphere downward from the foot position.
    /// Populates _isGrounded and _groundNormal each FixedUpdate.
    /// Using a SphereCast rather than a point cast gives reliable results on
    /// slightly uneven surfaces without relying on a CharacterController capsule.
    /// </summary>
    private void CheckGround()
    {
        Vector3 origin = transform.TransformPoint(groundCheckOffset);

        if (Physics.SphereCast(
                origin,
                groundCheckRadius,
                Vector3.down,
                out RaycastHit hit,
                groundCheckDistance,
                groundLayer,
                QueryTriggerInteraction.Ignore))
        {
            _isGrounded = true;
            _groundNormal = hit.normal;
        }
        else
        {
            _isGrounded = false;
            _groundNormal = Vector3.up;
        }
    }

    // =========================================================================
    //  Jump
    // =========================================================================

    private void HandleJumpInput()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame && _isGrounded && !_jumping)
            Jump();
    }

    private void Jump()
    {
        // Zero out vertical velocity first so double-jump height is consistent.
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        _rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);

        _jumping = true;
        _animator.SetTrigger(AnimJump);
        StartCoroutine(ResetJumpFlag());
    }

    private IEnumerator ResetJumpFlag()
    {
        yield return new WaitForSeconds(0.35f);
        _jumping = false;
    }

    // =========================================================================
    //  Dash
    // =========================================================================

    private IEnumerator DashCoroutine()
    {
        dashing = true;
        _dashCooldownTimer = dashCooldown;
        _animator.SetTrigger(AnimDash);

        // Camera-relative dash direction, same convention as normal movement.
        Vector3 camForward = _mainCamera.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();
        Vector3 camRight = Vector3.Cross(Vector3.up, camForward);

        Vector3 dashDir = _inputDir.sqrMagnitude > 0.01f
            ? (camForward * _inputDir.y + camRight * _inputDir.x).normalized
            : transform.forward;

        transform.forward = dashDir;

        // Switch to kinematic during the dash so we drive position exactly
        // without fighting the solver on the ground collider.
        _rb.isKinematic = true;

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            float t = elapsed / dashDuration;
            float speed = Mathf.Lerp(walkSpeed, dashSpeed, Mathf.Sin(t * Mathf.PI));

            // Preserve a little gravity feel by keeping Y velocity factored in.
            float yOffset = _isGrounded
                ? -0.05f                                // keep flush to ground
                : _rb.linearVelocity.y * Time.deltaTime;      // fall naturally

            Vector3 delta = dashDir * (speed * Time.deltaTime);
            delta.y = yOffset;
            _rb.MovePosition(_rb.position + delta);

            elapsed += Time.deltaTime;
            yield return null;
        }

        _rb.isKinematic = false;

        // Seed residual momentum into the Rigidbody so locomotion momentum
        // blending in StateHandler / SmoothlyLerpMoveSpeed has something to
        // ease out from.
        _rb.linearVelocity = new Vector3(
            dashDir.x * walkSpeed,
            _rb.linearVelocity.y,
            dashDir.z * walkSpeed);

        dashing = false;
    }

    // =========================================================================
    //  State machine
    // =========================================================================

    private void StateHandler()
    {
        if (dashing)
        {
            state = MovementState.dashing;
            _desiredMoveSpeed = dashSpeed;
            _speedChangeFactor = dashSpeedChangeFactor;
        }
        else if (_isGrounded)
        {
            state = MovementState.running;
            _desiredMoveSpeed = walkSpeed;
        }
        else
        {
            state = MovementState.airing;
            _desiredMoveSpeed = walkSpeed;
        }

        bool speedChanged = !Mathf.Approximately(_desiredMoveSpeed, _lastDesiredMoveSpeed);
        if (_lastState == MovementState.dashing) _keepMomentum = true;

        if (speedChanged)
        {
            if (_lerpCoroutine != null) StopCoroutine(_lerpCoroutine);

            if (_keepMomentum)
            {
                _lerpCoroutine = SmoothlyLerpMoveSpeed();
                StartCoroutine(_lerpCoroutine);
            }
            else
            {
                _moveSpeed = _desiredMoveSpeed;
            }
        }

        _lastDesiredMoveSpeed = _desiredMoveSpeed;
        _lastState = state;
    }

    private IEnumerator SmoothlyLerpMoveSpeed()
    {
        float elapsed = 0f;
        float difference = Mathf.Abs(_desiredMoveSpeed - _moveSpeed);
        float start = _moveSpeed;
        float factor = _speedChangeFactor;

        while (elapsed < difference)
        {
            _moveSpeed = Mathf.Lerp(start, _desiredMoveSpeed, elapsed / difference);
            elapsed += Time.deltaTime * factor;
            yield return null;
        }

        _moveSpeed = _desiredMoveSpeed;
        _speedChangeFactor = 1f;
        _keepMomentum = false;
    }

    // =========================================================================
    //  Move (called from FixedUpdate)
    // =========================================================================

    private void Move()
    {
        if (!_canMove) return;

        // Build camera-relative wish direction.
        Vector3 camForward = _mainCamera.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();
        Vector3 camRight = Vector3.Cross(Vector3.up, camForward);

        Vector3 wishDir = camForward * _inputDir.y + camRight * _inputDir.x;

        // Project onto slope so the character doesn't fight the normal.
        if (_isGrounded && OnSlope())
            wishDir = Vector3.ProjectOnPlane(wishDir, _groundNormal).normalized;

        float drag = _isGrounded ? groundDrag : airDrag;
        float speedFactor = _isGrounded ? 1f : airMultiplier;

        // ── Horizontal velocity ──────────────────────────────────────────────
        // Work in the XZ plane, then re-apply Y so we don't clobber gravity.
        Vector3 currentVel = _rb.linearVelocity;
        Vector3 horizontalVel = new Vector3(currentVel.x, 0f, currentVel.z);

        // Accelerate toward wish direction.
        horizontalVel += wishDir * (_moveSpeed * speedFactor * Time.fixedDeltaTime * 10f);

        // Clamp to move speed.
        if (horizontalVel.magnitude > _moveSpeed)
            horizontalVel = horizontalVel.normalized * _moveSpeed;

        // Apply drag (deceleration toward zero when no input).
        horizontalVel = Vector3.MoveTowards(horizontalVel, Vector3.zero, drag * Time.fixedDeltaTime);

        // Re-combine with the vertical component the physics engine owns.
        _rb.linearVelocity = new Vector3(horizontalVel.x, currentVel.y, horizontalVel.z);
    }

    // =========================================================================
    //  Extra gravity
    // =========================================================================

    /// <summary>
    /// Supplements Physics.gravity with a tunable per-character downward force.
    /// Keeps falling snappy without touching the global gravity setting.
    /// Not applied while grounded to avoid pushing through floors.
    /// </summary>
    private void ApplyExtraGravity()
    {
        if (!_isGrounded)
            _rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
    }

    // =========================================================================
    //  Slope detection
    // =========================================================================

    private bool OnSlope()
    {
        if (_jumping) return false;
        float angle = Vector3.Angle(Vector3.up, _groundNormal);
        return angle > 0f && angle < maxSlopeAngle;
    }

    // =========================================================================
    //  Rotation
    // =========================================================================

    private void SmoothRotateToCamera()
    {
        // if (_inputDir.sqrMagnitude < 0.01f) return;

        Vector3 camForward = _mainCamera.transform.forward;
        camForward.y = 0f;

        Vector3 newForward = Vector3.SmoothDamp(
            transform.forward,
            camForward,
            ref _smoothDampVel,
            smoothFollowMoveDirectionFactor);

        // Rotate the Rigidbody directly to keep physics and transform in sync.
        _rb.MoveRotation(Quaternion.LookRotation(newForward));
    }

    // =========================================================================
    //  Animations
    // =========================================================================

    private void UpdateAnimations()
    {
        bool isRunning = state == MovementState.running && _inputDir.sqrMagnitude > 0.01f;

        _animator.SetFloat("x", _inputDir.x);
        _animator.SetFloat("y", _inputDir.y);
        _animator.SetBool(AnimRunning, isRunning);
        _animator.SetBool(AnimDashing, dashing);
        _animator.SetBool(AnimFalling, state == MovementState.airing);
        _animator.SetFloat(AnimVerticalVelocity,
            Mathf.Lerp(0f, 1f, Mathf.Abs(_rb.linearVelocity.y) / 20f));
    }

    // =========================================================================
    //  Helpers
    // =========================================================================

    private void ResetMovement()
    {
        _inputDir = Vector2.zero;
        _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
    }

    // =========================================================================
    //  Public API
    // =========================================================================

    public void CallDashAnimation() => _animator.SetTrigger(AnimDash);
    public void TakeDamage() => _animator.SetTrigger(AnimTakeDamage);
    public void Death() => _animator.SetTrigger(AnimDeath);

    /// <summary>
    /// Called by the ragdoll/attack system to seed a directional impulse into
    /// the Rigidbody (e.g. being launched by a dash strike).
    /// </summary>
    public void ApplyDashVelocity(Vector3 dashVelocity)
    {
        _rb.linearVelocity = new Vector3(dashVelocity.x, _rb.linearVelocity.y, dashVelocity.z);
    }

    /// <summary>
    /// Switches the Rigidbody between kinematic and dynamic.
    /// Call this from PlayerRagdollController when toggling the ragdoll state:
    ///   - ragdoll ON  → SetKinematic(false) so physics drives the body
    ///   - ragdoll OFF → SetKinematic(true)  so this script drives it again
    /// </summary>
    public void SetKinematic(bool isKinematic)
    {
        _rb.isKinematic = isKinematic;
    }

#if UNITY_EDITOR
    // ─── Gizmos ──────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 origin = transform.TransformPoint(groundCheckOffset);
        Gizmos.DrawWireSphere(origin, groundCheckRadius);
        Gizmos.DrawWireSphere(origin + Vector3.down * groundCheckDistance, groundCheckRadius);
    }
#endif
}