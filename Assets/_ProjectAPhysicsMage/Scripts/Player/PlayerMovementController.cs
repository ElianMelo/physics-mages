using FishNet.Object;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PlayerMovementController — Unity 6.3 LTS
/// Uses CharacterController instead of Rigidbody.
///
/// ─── Recommended CharacterController component settings ───────────────────────
///   Slope Limit      : 45
///   Step Offset      : 0.3
///   Skin Width        : 0.08
///   Min Move Distance : 0.001
///   Center           : (0, 1, 0)      ← matches a 2-unit tall capsule
///   Radius           : 0.4
///   Height           : 2.0
/// ──────────────────────────────────────────────────────────────────────────────
///
/// ─── Recommended Inspector values (serialised fields below) ──────────────────
///   walkSpeed                 : 6
///   sprintSpeed               : 10          (unused until sprint action added)
///   dashSpeed                 : 20
///   dashSpeedChangeFactor     : 8
///   diveSpeed                 : 14
///   diveSpeedChangeFactor     : 6
///   jumpForce                 : 6           (initial vertical speed, m/s)
///   gravity                   : 20          (downward acceleration, m/s²)
///   airMultiplier             : 0.4
///   groundDrag                : 10          (horizontal deceleration on ground)
///   airDrag                   : 1           (horizontal deceleration in air)
///   smoothFollowMoveDirection : 0.08
///   rotationSpeed             : 720         (deg/s, used as SmoothDamp maxSpeed)
///   maxSlopeAngle             : 45
/// ──────────────────────────────────────────────────────────────────────────────
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovementController : NetworkBehaviour
{
    // ─── Movement ────────────────────────────────────────────────────────────
    [Header("Movement")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 10f;

    [Header("Dash")]
    public float dashSpeed = 20f;
    public float dashSpeedChangeFactor = 8f;

    [Header("Dive")]
    public float diveSpeed = 14f;
    public float diveSpeedChangeFactor = 6f;

    [Header("Jump & Gravity")]
    public float jumpForce = 6f;   // m/s initial vertical velocity
    public float gravity = 20f;  // m/s² downward acceleration

    [Header("Drag")]
    public float groundDrag = 10f;    // horizontal deceleration when grounded
    public float airDrag = 1f;     // horizontal deceleration in air

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
    private Vector3 _velocity;          // full 3-D velocity tracked manually
    private Vector3 _smoothDampVel;

    private bool _canMove = true;
    private bool _jumping;

    public bool dashing;
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

        // ── Debug ragdoll keys (keep parity with original) ──────────────────
        if (Keyboard.current.iKey.wasPressedThisFrame) _ragdoll.TriggerFall(Vector3.up, 20f);
        if (Keyboard.current.jKey.wasPressedThisFrame) _ragdoll.TriggerFall(Vector3.left, 20f);
        if (Keyboard.current.lKey.wasPressedThisFrame) _ragdoll.TriggerFall(Vector3.right, 20f);
        if (Keyboard.current.kKey.wasPressedThisFrame) _ragdoll.TriggerFall(Vector3.down, 20f);

        if (_ragdoll.IsStaggered) return;

        HandleJumpInput();
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
    {
        _inputDir = value.Get<Vector2>();
    }

    // =========================================================================
    //  Jump
    // =========================================================================

    private void HandleJumpInput()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame && IsGrounded && !_jumping)
        {
            Jump();
        }
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
        // ── Horizontal input ────────────────────────────────────────────────
        Vector3 camForward = _mainCamera.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();
        Vector3 camRight = Vector3.Cross(Vector3.up, camForward);   // no .right to stay flat

        Vector3 wishDir = camForward * _inputDir.y + camRight * _inputDir.x;

        // ── Slope projection ────────────────────────────────────────────────
        if (IsGrounded && OnSlope(out Vector3 slopeNormal))
            wishDir = Vector3.ProjectOnPlane(wishDir, slopeNormal).normalized;

        // ── Apply horizontal velocity ────────────────────────────────────────
        float drag = IsGrounded ? groundDrag : airDrag;
        float speedFactor = IsGrounded ? 1f : airMultiplier;

        Vector3 horizontal = new Vector3(_velocity.x, 0f, _velocity.z);
        horizontal += wishDir * (_moveSpeed * speedFactor * Time.fixedDeltaTime * 10f);

        // Speed cap
        if (horizontal.magnitude > _moveSpeed)
            horizontal = horizontal.normalized * _moveSpeed;

        // Drag
        horizontal = Vector3.MoveTowards(horizontal, Vector3.zero, drag * Time.fixedDeltaTime);

        _velocity.x = horizontal.x;
        _velocity.z = horizontal.z;

        // ── Gravity ──────────────────────────────────────────────────────────
        if (IsGrounded && _velocity.y < 0f)
            _velocity.y = -2f;          // small negative keeps CC grounded on slopes

        _velocity.y -= gravity * Time.fixedDeltaTime;

        // ── Move ─────────────────────────────────────────────────────────────
        _cc.Move(_velocity * Time.fixedDeltaTime);
    }

    // =========================================================================
    //  Slope detection
    // =========================================================================

    private bool OnSlope(out Vector3 normal)
    {
        normal = Vector3.up;
        if (_jumping) return false;

        // SphereCast from the base of the capsule
        float checkDist = (_cc.height * 0.5f) + _cc.stepOffset + 0.05f;
        if (Physics.SphereCast(transform.position + _cc.center,
                               _cc.radius * 0.9f,
                               Vector3.down, out RaycastHit hit,
                               checkDist))
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
        if (_inputDir.sqrMagnitude < 0.01f) return;     // only rotate while moving

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
        _animator.SetBool(AnimDashing, state == MovementState.dashing);
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
        _velocity = new Vector3(0f, _velocity.y, 0f);  // preserve gravity
    }

    // =========================================================================
    //  Public API (called by dash/dive controllers, combat, etc.)
    // =========================================================================

    public void CallDashAnimation() => _animator.SetTrigger(AnimDash);
    public void CallDiveAnimation() => _animator.SetTrigger(AnimDive);
    public void TakeDamage() => _animator.SetTrigger(AnimTakeDamage);
    public void Death() => _animator.SetTrigger(AnimDeath);

    /// <summary>
    /// Called by the dash controller to inject horizontal velocity directly.
    /// </summary>
    public void ApplyDashVelocity(Vector3 dashVelocity)
    {
        _velocity.x = dashVelocity.x;
        _velocity.z = dashVelocity.z;
    }

    /// <summary>
    /// Called by the dive controller when exiting a dive.
    /// </summary>
    public void ApplyDiveExitForce(Vector3 direction, float force)
    {
        Vector3 exit = direction.normalized * force;
        _velocity.x = exit.x;
        _velocity.y = Mathf.Max(_velocity.y, exit.y);
        _velocity.z = exit.z;
    }
}