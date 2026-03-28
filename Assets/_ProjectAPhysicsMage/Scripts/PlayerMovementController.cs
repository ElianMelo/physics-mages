using System.Collections;
using UnityEngine;

public class PlayerMovementController : MonoBehaviour
{
    [Header("Movement Class")]

    [Header("Rotator")]
    public float rotationSpeed;
    public float rotationFactor;
    public float timeRotateBack;
    public float diveExitForce;
    public float smoothFollowMoveDirectionFactor;
    public float smoothFrictionFactor;
    private float calculatedTimeRotateBack;

    [Header("Movement")]
    private float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;

    public float dashSpeed;
    public float dashSpeedChangeFactor;
    public float diveSpeed;
    public float diveSpeedChangeFactor;

    public float maxYSpeed;

    public float groundDrag;
    public float airDrag;

    [Header("Jumping")]
    public float jumpForce;
    public float airMultiplier;
    private bool jumping;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    private bool grounded;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;

    public Transform orientation;
    public Transform cameraOrientation;
    public Transform slopeDetectorFront;
    public Transform slopeDetectorBack;

    private float horizontalInput;
    private float verticalInput;

    private Vector3 moveDirection;

    private Animator playerAnimator;
    private Rigidbody playerRb;

    private int maxJumps = 2;
    private int jumps;

    private Vector3 smoothDampvelocity = Vector3.zero;

    public MovementState state;

    private IEnumerator SmoothlyLerpMoveSpeedCoroutine;

    private bool _canMove;

    public bool CanMove { 
        get
        {
            return _canMove;
        }
        set
        {
            _canMove = value;
            ResetMovement();
        }
    }

    public bool IsGrounded => grounded;

    public enum MovementState
    {
        idle,
        running,
        dashing,
        airing
    }

    public bool dashing;
    public bool diving;

    private readonly static string JumpAnim = "Jump";
    private readonly static string DashAnim = "Dash";
    private readonly static string DiveAnim = "Dive";

    private readonly static string RunningAnim = "Running";
    private readonly static string FallingAnim = "Falling";
    private readonly static string VerticalVelocityAnim = "VerticalVelocity";
    private readonly static string DashingAnim = "Dashing";

    private readonly static string TakeDamageAnim = "TakeDamage";
    private readonly static string DeathAnim = "Death";

    private void Start()
    {
        calculatedTimeRotateBack = timeRotateBack;
        playerAnimator = GetComponent<Animator>();
        playerRb = GetComponent<Rigidbody>();
        playerRb.freezeRotation = true;
        jumps = maxJumps;

        // PlayerManager.Instance.OnPlayerTakeDamage.AddListener(TakeDamage);
        // PlayerManager.Instance.OnPlayerDeath.AddListener(Death);
    }

    private void OnDestroy()
    {
        // PlayerManager.Instance.OnPlayerTakeDamage.RemoveListener(TakeDamage);
        // PlayerManager.Instance.OnPlayerDeath.RemoveListener(Death);
    }

    private void ResetMovement()
    {
        horizontalInput = 0;
        verticalInput = 0;
        SpeedControl();
        StateHandler();
        playerRb.linearVelocity = new Vector3(
            Mathf.Clamp(playerRb.linearVelocity.x, playerRb.linearVelocity.x, 20f),
            Mathf.Clamp(playerRb.linearVelocity.y, playerRb.linearVelocity.y, 20f),
            Mathf.Clamp(playerRb.linearVelocity.z, playerRb.linearVelocity.z, 20f));
    }

    private void Update()
    {
        //if (GameManager.Instance.currentGameState != GameManager.GameState.Moving) return;
        //if (!_canMove) return;
        CheckAnimation();
        grounded = !Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);
        if (grounded && !jumping)
        {
            jumps = maxJumps;
        }

        GetInputsActions();
        SpeedControl();
        StateHandler();

        playerRb.linearVelocity = new Vector3(
            Mathf.Clamp(playerRb.linearVelocity.x, playerRb.linearVelocity.x, 20f),
            Mathf.Clamp(playerRb.linearVelocity.y, playerRb.linearVelocity.y, 20f),
            Mathf.Clamp(playerRb.linearVelocity.z, playerRb.linearVelocity.z, 20f));

        if (state == MovementState.running)
        {
            playerRb.linearDamping = groundDrag;
        }
        else
        {
            playerRb.linearDamping = 0f;
        }
    }

    private void FixedUpdate()
    {
        // if (GameManager.Instance.currentGameState != GameManager.GameState.Moving) return;
        if (dashing) return;
        if (diving) return;
        Move();
        if (moveDirection != Vector3.zero)
        {
            SmoothRotateForward();
        }
    }

    private void SmoothRotateForward()
    {
        transform.forward = Vector3.SmoothDamp(transform.forward, moveDirection, ref smoothDampvelocity, smoothFollowMoveDirectionFactor);
    }

    private void GetInputsActions()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");

        if (Input.GetKeyDown(KeyCode.Space) && jumps > 0)
        {
            jumps -= 1;
            Jump();
        }

    }

    private float desiredMoveSpeed;
    private float lastDesiredMoveSpeed;
    private MovementState lastState;
    private bool keepMomentum;

    private void StateHandler()
    {
        // Mode - Dashing
        if (dashing)
        {
            state = MovementState.dashing;
            desiredMoveSpeed = dashSpeed;
            speedChangeFactor = dashSpeedChangeFactor;
        }
        // Mode - Walking
        else if (grounded)
        {
            state = MovementState.running;
            desiredMoveSpeed = walkSpeed;
        }
        // Mode - Air
        else if (!grounded)
        {
            state = MovementState.airing;
            desiredMoveSpeed = walkSpeed;
        }
        // Mode - Regular
        else
        {
            desiredMoveSpeed = walkSpeed;
        }

        bool desiredMoveSpeedHasChanged = desiredMoveSpeed != lastDesiredMoveSpeed;
        if (lastState == MovementState.dashing) keepMomentum = true;

        if (desiredMoveSpeedHasChanged)
        {
            if (keepMomentum)
            {
                if (SmoothlyLerpMoveSpeedCoroutine != null) StopCoroutine(SmoothlyLerpMoveSpeedCoroutine);
                SmoothlyLerpMoveSpeedCoroutine = SmoothlyLerpMoveSpeed();
                StartCoroutine(SmoothlyLerpMoveSpeedCoroutine);
            }
            else
            {
                if (SmoothlyLerpMoveSpeedCoroutine != null) StopCoroutine(SmoothlyLerpMoveSpeedCoroutine);
                moveSpeed = desiredMoveSpeed;
            }
        }

        lastDesiredMoveSpeed = desiredMoveSpeed;
        lastState = state;
    }

    private float speedChangeFactor;

    private IEnumerator SmoothlyLerpMoveSpeed()
    {
        // smoothly lerp movementSpeed to desired value
        float time = 0;
        float difference = Mathf.Abs(desiredMoveSpeed - moveSpeed);
        float startValue = moveSpeed;

        float boostFactor = speedChangeFactor;

        while (time < difference)
        {
            moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference);
            time += Time.deltaTime * boostFactor;
            yield return null;
        }

        moveSpeed = desiredMoveSpeed;
        speedChangeFactor = 1f;
        keepMomentum = false;
    }

    private void Move()
    {
        if (state == MovementState.dashing) return;
        float currentMoveSpeed = 0f;

        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        moveDirection.y = 0f;
        currentMoveSpeed = moveSpeed;

        // on slope
        if (OnSlope())
        {
            playerRb.AddForce(GetSlopMoveDirection() * currentMoveSpeed * 10f, ForceMode.Force);
        }

        // on ground
        if (grounded)
        {
            playerRb.linearVelocity = Vector3.SmoothDamp(playerRb.linearVelocity, Vector3.zero, ref smoothDampvelocity, smoothFrictionFactor);
            playerRb.AddForce(moveDirection.normalized * currentMoveSpeed * 10f, ForceMode.Force);
        }
        // in air
        else if (!grounded)
        {
            playerRb.AddForce(moveDirection.normalized * currentMoveSpeed * 10f * airMultiplier, ForceMode.Force);
        }

    }

    private void RotatePlayer()
    {
        bool invertYAxis = false;
        float invertFactor = invertYAxis ? 1f : -1f;
        Quaternion targetRotation = transform.rotation *
            Quaternion.Euler(verticalInput * rotationSpeed * invertFactor, 0f, (horizontalInput * rotationSpeed * -1f));
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationFactor);
    }

    private void OnTriggerEnter(Collider other)
    {
    }

    private void OnTriggerExit(Collider other)
    {
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            grounded = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
    }

    private void SpeedControl()
    {
        // limiting speed on slop
        if (OnSlope())
        {
            Vector3 flatVel = new Vector3(playerRb.linearVelocity.x, playerRb.linearVelocity.y, playerRb.linearVelocity.z);

            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                playerRb.linearVelocity = new Vector3(limitedVel.x, limitedVel.y, limitedVel.z);
            }
        }
        else
        {
            Vector3 flatVel = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z);

            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                playerRb.linearVelocity = new Vector3(limitedVel.x, playerRb.linearVelocity.y, limitedVel.z);
            }
        }

        // limit y vel
        if (maxYSpeed != 0 && playerRb.linearVelocity.y > maxYSpeed)
        {
            playerRb.linearVelocity = new Vector3(playerRb.linearVelocity.x, maxYSpeed, playerRb.linearVelocity.z);
        }
    }

    private void Jump()
    {
        playerRb.linearDamping = 0f;
        jumping = true;
        StartCoroutine(Jumping());
        playerAnimator.SetTrigger(JumpAnim);
        // SoundManager.Instance.PlayJumpSound();
        // PlayerManager.Instance.PlayerJumpEvent();
        // playerVFXController.EnableBooster();
        // playerVFXController.DisableBoosterDelayed(0.1f);
        playerRb.linearVelocity = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z);
        playerRb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private IEnumerator Jumping()
    {
        yield return new WaitForSeconds(0.3f);
        jumping = false;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(slopeDetectorFront.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0 && !jumping;
        }
        return false;
    }

    private Vector3 GetSlopMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }

    private void CheckAnimation()
    {
        var isRunning = state == MovementState.running && (verticalInput != 0 || horizontalInput != 0);
        playerAnimator.SetBool(RunningAnim, isRunning);
        playerAnimator.SetBool(DashingAnim, state == MovementState.dashing);
        playerAnimator.SetBool(FallingAnim, state == MovementState.airing);
        playerAnimator.SetFloat(VerticalVelocityAnim, Mathf.Lerp(0, 1, Mathf.Abs(playerRb.linearVelocity.y) / 20));
    }

    public void CallDashAnimation()
    {
        playerAnimator.SetTrigger(DashAnim);
    }

    public void CallDiveAnimation()
    {
        playerAnimator.SetTrigger(DiveAnim);
    }

    public void TakeDamage()
    {
        // ScreenShakeManager.Instance.ShakeScreen();
        // playerVFXController.TriggerShockVFX(transform);
        playerAnimator.SetTrigger(TakeDamageAnim);
    }

    public void Death()
    {
        // ScreenShakeManager.Instance.ShakeScreen();
        // playerVFXController.TriggerShockVFX(transform);
        playerAnimator.SetTrigger(DeathAnim);
    }

}

