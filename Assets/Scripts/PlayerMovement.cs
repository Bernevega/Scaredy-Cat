// PlayerMovement.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float runMultiplier = 1.5f;
    public float jumpForce = 5f;

    [Header("Ground Check Settings")]
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;

    [SerializeField] private GameObject model;
    [SerializeField] private Animator animator;
    [SerializeField] private AnimEventInvoker animEvents;

    [HideInInspector] public bool blockRightMovement = false;
    [HideInInspector] public bool blockJump = false;  
    [HideInInspector] public bool blockSprint = false;

    [Header("Rotation Reset Settings")]
    [Tooltip("Seconds to ease model back to last direction when dialog ends. If ≤0, uses rotationReturnSpeed.")]
    public float rotationReturnDuration = 0.5f;
    [Tooltip("Degrees per second to rotate model if rotationReturnDuration ≤ 0.")]
    public float rotationReturnSpeed = 1080f;

    private Rigidbody rb;
    private Camera mainCam;
    private Vector3 direction = Vector3.forward;
    private bool isGrounded = true;
    private bool canMove = true;
    private bool _wasDialogActive = false;
    private bool hasJumped = false;
    private MoveState moveState = MoveState.Idle; 

    public enum MoveState : byte
    {
        Idle,
        Walk,
        Jumping,
        Landing,
        WakingUp,
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        mainCam = Camera.main;

        // Zero‐friction so the player slides along walls
        Collider col = GetComponent<Collider>();
        var slideMat = new PhysicsMaterial("SlideMat")
        {
            staticFriction = 0f,
            dynamicFriction = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum
        };
        col.material = slideMat;

        animEvents.stringEvent += AnimStringEvent;
    }

    void Update()
    {
        if (mainCam == null)
            mainCam = Camera.main;

        // 2) Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded)
        {
            hasJumped = false;
            animator.SetBool("grounded", true);
        }
        else
        {
            animator.SetBool("grounded", false);
        }

        // 1) Dialog check
        var dialogMgr = SimpleDialogManager.Instance;
        bool dialogActive = dialogMgr != null
                            && dialogMgr.dialogPanel != null
                            && dialogMgr.dialogPanel.activeSelf;

        if (dialogActive)
        {
            _wasDialogActive = true;
            canMove = false;
            // Stop residual horizontal movement
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        if (_wasDialogActive)
        {
            _wasDialogActive = false;
            canMove = true;
            // Clear any lingering velocity
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

            // ── Smoothly rotate model back to last direction ──
            StopAllCoroutines();
            if (rotationReturnDuration > 0f)
                StartCoroutine(RotateModelOverTime(rotationReturnDuration));
            else
                StartCoroutine(RotateModelAtSpeed());
            // ───────────────────────────────────────────────────

            return;
        }

        if (!canMove) return;

        if (speed > 0f)
        {
            // 3) Read inputs
            float moveX = 0;
            float moveZ = 0;
            if (moveState == MoveState.Idle)
            {
                moveX = Input.GetAxisRaw("Horizontal");
                moveZ = Input.GetAxisRaw("Vertical");
            }

            if (blockRightMovement && moveX > 0f)
                moveX = 0f;
            if (moveX == 0 && moveZ == 0)
            {
                animator.SetBool("moveInput", false);
            }
            else
            {
                animator.SetBool("moveInput", true);
            }

            // 4) Calculate move direction relative to camera
            Vector3 camF = new Vector3(mainCam.transform.forward.x, 0f, mainCam.transform.forward.z).normalized;
            Vector3 camR = Vector3.Cross(Vector3.up, camF);
            Vector3 moveDir = (camF * moveZ + camR * moveX).normalized;
            if (moveDir != Vector3.zero)
                direction = moveDir;

            // 5) Sprint (blocked in labyrinth) & running animation
            float currentSpeed = speed;
            bool isRunning = false;
            if (Input.GetKey(KeyCode.LeftShift) && !blockSprint && moveDir != Vector3.zero)
            {
                currentSpeed *= runMultiplier;
                isRunning = true;
            }
            animator.SetBool("isRunning", isRunning);

            // 6) Apply horizontal movement (preserve vertical velocity)
            rb.linearVelocity = new Vector3(moveDir.x * currentSpeed, rb.linearVelocity.y, moveDir.z * currentSpeed);

            if (moveState == MoveState.Idle)
            {
                // 7) Jump (only once until grounded again)
                if (!blockJump && Input.GetButtonDown("Jump") && isGrounded && !hasJumped)
                {
                    moveState = MoveState.Jumping;
                    hasJumped = true;
                    animator.SetTrigger("jump");
                }
            }   
        }

        // 8) Rotate the visible model (or the whole object if no model assigned)
        if (model != null)
        {
            if (model.transform.forward != direction)
                model.transform.rotation = Quaternion.RotateTowards(
                    model.transform.rotation,
                    Quaternion.LookRotation(direction, Vector3.up),
                    1080f * Time.deltaTime
                );
        }
        else
        {
            if (transform.forward != direction)
                transform.forward = Vector3.RotateTowards(
                    transform.forward,
                    direction,
                    Mathf.Deg2Rad * 1080f * Time.deltaTime,
                    0f
                );
        }
    }

    /// <summary>
    /// Externally set the facing direction (normalized).
    /// </summary>
    public void SetDirection(Vector3 dir)
    {
        direction = dir.normalized;
    }

    /// <summary>
    /// Enable or disable all movement (useful for cutscenes, dialogs, etc.).
    /// </summary>
    public void SetCanMove(bool b)
    {
        canMove = b;
    }

    public void WakeUp()
    {
        moveState = MoveState.WakingUp;
        animator.SetTrigger("WakeUp");
    }

    public void AnimStringEvent(string str)
    {
        switch (str)
        {
            case "JumpLiftOff":
                JumpLiftOff();
                break;
            case "LandStart":
                LandStart();
                break;
            case "LandEnd":
                LandEnd();
                break;
            case "WakeUpEnd":
                WakeUpEnd(); 
                break;
        }
    }

    public void JumpLiftOff()
    {
        moveState = MoveState.Idle;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    public void LandStart()
    {
        moveState = MoveState.Landing;
    }

    public void LandEnd()
    {
        moveState = MoveState.Idle;
    }

    public void WakeUpEnd()
    {
        moveState = MoveState.Idle;
        Debug.Log("Wake up end");
    }

    // ───────────────────────────────────────────────────────────────────────────
    // ROTATION COROUTINES (for dialog-end reset)
    private IEnumerator RotateModelOverTime(float duration)
    {
        Transform target = model != null ? model.transform : transform;
        Quaternion startRot = target.rotation;
        Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // ease-in/out
            t = t * t * (3f - 2f * t);
            target.rotation = Quaternion.Slerp(startRot, targetRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        target.rotation = targetRot;
    }

    private IEnumerator RotateModelAtSpeed()
    {
        Transform target = model != null ? model.transform : transform;
        Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);

        while (Quaternion.Angle(target.rotation, targetRot) > 0.1f)
        {
            target.rotation = Quaternion.RotateTowards(
                target.rotation,
                targetRot,
                rotationReturnSpeed * Time.deltaTime
            );
            yield return null;
        }
        target.rotation = targetRot;
    }
    // ───────────────────────────────────────────────────────────────────────────
}
