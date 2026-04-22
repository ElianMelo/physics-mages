using FishNet.Object;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementController : NetworkBehaviour
{
    // ─── Movement ────────────────────────────────────────────────────────────
    [Header("Movement")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 10f;

    [Header("Dash  (Left Shift)")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.25f;
    public float dashCooldown = 1.0f;
    public float dashSpeedChangeFactor = 8f;

    [Header("Dive")]
    public float diveSpeed = 14f;
    public float diveSpeedChangeFactor = 6f;

    [Header("Jump & Gravity")]
    public float jumpForce = 6f;
    public float gravity = 20f;

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

    // ─── Internal state ──────────────────────────────────────────────────────
    private CharacterController _cc;
    private Animator _animator;
    private PlayerRagdollController _ragdoll;
    private PlayerInput _playerInput;
    private Camera _mainCamera;

    private Vector2 _inputDir;
    private Vector3 _velocity;
    private Vector3 _smoothDampVel;

    private bool _canMove = true;
    private bool _jumping;
    private bool _dashRequested;
    private float _dashCooldownTimer;

    public bool dashing { get; private set; }
    public bool diving;

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

    // ─── Animator hashes ─────────────────────────────────────────────────────
    private static readonly int AnimJump = Animator.StringToHash("Jump");
    private static readonly int AnimDash = Animator.StringToHash("Dash");
    private static readonly int AnimDive = Animator.StringToHash("Dive");
    private static readonly int AnimRunning = Animator.StringToHash("Running");
    private static readonly int AnimFalling = Animator.StringToHash("Falling");
    private static readonly int AnimVerticalVelocity = Animator.StringToHash("VerticalVelocity");
    private static readonly int AnimDashing = Animator.StringToHash("Dashing");
    private static readonly int AnimTakeDamage = Animator.StringToHash("TakeDamage");
    private static readonly int AnimDeath = Animator.StringToHash("Death");

    // ─── Public accessors ────────────────────────────────────────────────────
    public bool IsGrounded => _cc != null && _cc.isGrounded;

    public bool CanMove
    {
        get => _canMove;
        set { _canMove = value; if (!value) ResetMovement(); }
    }

    // =========================================================================
    //  Unity lifecycle
    // =========================================================================

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _ragdoll = GetComponent<PlayerRagdollController>();
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
        if (dashing || diving) return;

        Move();
        SmoothRotateToCamera();
    }

    // =========================================================================
    //  Input (via PlayerInput component → Send Messages)
    // =========================================================================

    public void OnMove(InputValue value)
        => _inputDir = value.Get<Vector2>();

    public void OnJump(InputValue value)
    {
        if (value.isPressed && IsGrounded && !_jumping)
            Jump();
    }

    /// <summary>
    /// Bound to the "Dash" Button action (Left Shift / gamepad west button).
    /// Add a "Dash" action to your Input Action Asset → Player action map.
    /// </summary>
    public void OnDash(InputValue value)
    {
        if (value.isPressed) _dashRequested = true;
    }

    // =========================================================================
    //  Jump
    // =========================================================================

    private void HandleJumpInput()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame && IsGrounded && !_jumping)
            Jump();
    }

    private void Jump()
    {
        _velocity.y = jumpForce;
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

    /// <summary>
    /// Dashes in the current input direction (or character forward when idle).
    /// Speed follows a Sin curve — ramps up then eases out for a punchy feel.
    /// After the dash, walkSpeed momentum bleeds naturally into locomotion via
    /// the existing keepMomentum / SmoothlyLerpMoveSpeed path.
    /// </summary>
    private IEnumerator DashCoroutine()
    {
        dashing = true;
        _dashCooldownTimer = dashCooldown;
        _animator.SetTrigger(AnimDash);

        // Dash direction is camera-relative, same as normal movement
        Vector3 camForward = _mainCamera.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();
        Vector3 camRight = Vector3.Cross(Vector3.up, camForward);

        Vector3 dashDir = _inputDir.sqrMagnitude > 0.01f
            ? (camForward * _inputDir.y + camRight * _inputDir.x).normalized
            : transform.forward;

        // Snap body to face the dash direction instantly
        transform.forward = dashDir;

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            // Sin curve: 0 → peak → 0 — punchy burst with natural ease-out
            float t = elapsed / dashDuration;
            float speed = Mathf.Lerp(walkSpeed, dashSpeed, Mathf.Sin(t * Mathf.PI));

            Vector3 move = dashDir * (speed * Time.deltaTime);
            move.y = (IsGrounded ? -2f : _velocity.y) * Time.deltaTime;
            _cc.Move(move);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Feed residual momentum into the locomotion system
        _velocity.x = dashDir.x * walkSpeed;
        _velocity.z = dashDir.z * walkSpeed;
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
        else if (IsGrounded)
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
    //  Move
    // =========================================================================

    private void Move()
    {
        Vector3 camForward = _mainCamera.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();
        Vector3 camRight = Vector3.Cross(Vector3.up, camForward);

        Vector3 wishDir = camForward * _inputDir.y + camRight * _inputDir.x;

        if (IsGrounded && OnSlope(out Vector3 slopeNormal))
            wishDir = Vector3.ProjectOnPlane(wishDir, slopeNormal).normalized;

        float drag = IsGrounded ? groundDrag : airDrag;
        float speedFactor = IsGrounded ? 1f : airMultiplier;

        Vector3 horizontal = new Vector3(_velocity.x, 0f, _velocity.z);
        horizontal += wishDir * (_moveSpeed * speedFactor * Time.fixedDeltaTime * 10f);

        if (horizontal.magnitude > _moveSpeed)
            horizontal = horizontal.normalized * _moveSpeed;

        horizontal = Vector3.MoveTowards(horizontal, Vector3.zero, drag * Time.fixedDeltaTime);
        _velocity.x = horizontal.x;
        _velocity.z = horizontal.z;

        if (IsGrounded && _velocity.y < 0f)
            _velocity.y = -2f;

        _velocity.y -= gravity * Time.fixedDeltaTime;

        _cc.Move(_velocity * Time.fixedDeltaTime);
    }

    // =========================================================================
    //  Slope detection
    // =========================================================================

    private bool OnSlope(out Vector3 normal)
    {
        normal = Vector3.up;
        if (_jumping) return false;

        float checkDist = (_cc.height * 0.5f) + _cc.stepOffset + 0.05f;
        if (Physics.SphereCast(transform.position + _cc.center,
                               _cc.radius * 0.9f,
                               Vector3.down, out RaycastHit hit, checkDist))
        {
            float angle = Vector3.Angle(Vector3.up, hit.normal);
            if (angle > 0f && angle < maxSlopeAngle)
            {
                normal = hit.normal;
                return true;
            }
        }
        return false;
    }

    // =========================================================================
    //  Rotation
    // =========================================================================

    private void SmoothRotateToCamera()
    {
        if (_inputDir.sqrMagnitude < 0.01f) return;

        Vector3 camForward = _mainCamera.transform.forward;
        camForward.y = 0f;

        transform.forward = Vector3.SmoothDamp(
            transform.forward, camForward,
            ref _smoothDampVel,
            smoothFollowMoveDirectionFactor);
    }

    // =========================================================================
    //  Animations
    // =========================================================================

    private void UpdateAnimations()
    {
        bool isRunning = state == MovementState.running && _inputDir.sqrMagnitude > 0.01f;

        _animator.SetBool(AnimRunning, isRunning);
        _animator.SetBool(AnimDashing, dashing);
        _animator.SetBool(AnimFalling, state == MovementState.airing);
        _animator.SetFloat(AnimVerticalVelocity,
            Mathf.Lerp(0f, 1f, Mathf.Abs(_velocity.y) / 20f));
    }

    // =========================================================================
    //  Helpers
    // =========================================================================

    private void ResetMovement()
    {
        _inputDir = Vector2.zero;
        _velocity = new Vector3(0f, _velocity.y, 0f);
    }

    // =========================================================================
    //  Public API
    // =========================================================================

    public void CallDashAnimation() => _animator.SetTrigger(AnimDash);
    public void CallDiveAnimation() => _animator.SetTrigger(AnimDive);
    public void TakeDamage() => _animator.SetTrigger(AnimTakeDamage);
    public void Death() => _animator.SetTrigger(AnimDeath);

    public void ApplyDashVelocity(Vector3 dashVelocity)
    {
        _velocity.x = dashVelocity.x;
        _velocity.z = dashVelocity.z;
    }

    public void ApplyDiveExitForce(Vector3 direction, float force)
    {
        Vector3 exit = direction.normalized * force;
        _velocity.x = exit.x;
        _velocity.y = Mathf.Max(_velocity.y, exit.y);
        _velocity.z = exit.z;
    }
}