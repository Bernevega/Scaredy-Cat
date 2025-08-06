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
    [SerializeField] private AudioClip[] walkSoundEffects;
    [SerializeField] private AudioClip jumpSoundEffect;
    [SerializeField] private AudioClip landSoundEffect;

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
    public bool hasJumped = false;
    public MoveState moveState = MoveState.Idle;

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

        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        animator.SetBool("grounded", isGrounded);
        if (isGrounded)
            hasJumped = false;

        // Dialog check
        var dialogMgr = SimpleDialogManager.Instance;
        bool dialogActive = dialogMgr != null
                            && dialogMgr.dialogPanel != null
                            && dialogMgr.dialogPanel.activeSelf;

        if (dialogActive)
        {
            _wasDialogActive = true;
            canMove = false;

            // Stop residual movement
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

            // Play idle animation
            animator.SetBool("moveInput", false);
            animator.SetBool("isRunning", false);
            animator.Play("Idle");
            return;
        }

        if (_wasDialogActive)
        {
            _wasDialogActive = false;
            canMove = true;

            // Clear lingering velocity
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

            // Reset move state if grounded
            if (isGrounded)
                moveState = MoveState.Idle;

            // Smooth rotation reset
            StopAllCoroutines();
            if (rotationReturnDuration > 0f)
                StartCoroutine(RotateModelOverTime(rotationReturnDuration));
            else
                StartCoroutine(RotateModelAtSpeed());
            return;
        }

        if (!canMove) return;

        if (speed > 0f)
        {
            // Read inputs
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveZ = Input.GetAxisRaw("Vertical");

            if (blockRightMovement && moveX > 0f)
                moveX = 0f;

            bool hasInput = moveX != 0f || moveZ != 0f;
            animator.SetBool("moveInput", hasInput);

            // Direction relative to camera
            Vector3 camF = new Vector3(mainCam.transform.forward.x, 0f, mainCam.transform.forward.z).normalized;
            Vector3 camR = Vector3.Cross(Vector3.up, camF);
            Vector3 moveDir = (camF * moveZ + camR * moveX).normalized;
            if (moveDir != Vector3.zero)
                direction = moveDir;

            // Sprint
            float currentSpeed = speed;
            bool isRunning = false;
            if (hasInput && Input.GetKey(KeyCode.LeftShift) && !blockSprint)
            {
                currentSpeed *= runMultiplier;
                isRunning = true;
            }
            animator.SetBool("isRunning", isRunning);

            // Apply movement
            rb.linearVelocity = new Vector3(moveDir.x * currentSpeed, rb.linearVelocity.y, moveDir.z * currentSpeed);

            // Jump
            if (moveState == MoveState.Idle && !blockJump && Input.GetButtonDown("Jump") && isGrounded && !hasJumped)
            {
                animator.SetTrigger("jump");
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z); // reset Y velocity
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);      // instant jump
                hasJumped = true;
                moveState = MoveState.Jumping;
            }
        }

        // Rotate model or object
        Transform targetTransform = model != null ? model.transform : transform;
        Quaternion desiredRot = Quaternion.LookRotation(direction, Vector3.up);
        if (targetTransform.rotation != desiredRot)
            targetTransform.rotation = Quaternion.RotateTowards(
                targetTransform.rotation,
                desiredRot,
                1080f * Time.deltaTime
            );
    }

    public void SetDirection(Vector3 dir) => direction = dir.normalized;
    public void SetCanMove(bool b) => canMove = b;

    public void WakeUp() { moveState = MoveState.WakingUp; animator.SetTrigger("WakeUp"); }

    public void AnimStringEvent(string str)
    {
        switch (str)
        {
            case "JumpLiftOff": JumpLiftOff(); break;
            case "LandStart":   LandStart();   break;
            case "LandEnd":     LandEnd();     break;
            case "WakeUpEnd":   WakeUpEnd();   break;
            case "FootStep":    MakeFootstep(); break;
        }
    }

    void JumpLiftOff()
    {
        GameObject soundPlayer = ObjectPool.instance.objPool_GetObject("2DSoundPlayer");
        if (soundPlayer)
        {
            SoundPlayer spScript = soundPlayer.GetComponent<SoundPlayer>();
            spScript.transform.position = transform.position;

            PlaySoundInfo soundInfo = new PlaySoundInfo(jumpSoundEffect);
            soundInfo.pitch = Random.Range(0.7f, 1.3f);
            soundInfo.volume = 0.5f;

            spScript.PlaySound(soundInfo);
        }
    }

    void LandStart()
    {
        moveState = MoveState.Landing;

        GameObject soundPlayer = ObjectPool.instance.objPool_GetObject("2DSoundPlayer");
        if (soundPlayer)
        {
            SoundPlayer spScript = soundPlayer.GetComponent<SoundPlayer>();
            spScript.transform.position = transform.position;

            PlaySoundInfo soundInfo = new PlaySoundInfo(landSoundEffect);
            soundInfo.pitch = Random.Range(0.7f, 1.3f);
            soundInfo.volume = 0.5f;

            spScript.PlaySound(soundInfo);
        }
    }

    void LandEnd()
    {
        moveState = MoveState.Idle;
    }

    void WakeUpEnd()
    {
        moveState = MoveState.Idle;
        Debug.Log("Wake up end");
    }

    void MakeFootstep()
    {
        int footstepToPlay = Random.Range(0, walkSoundEffects.Length);
        AudioClip footStepClip = walkSoundEffects[footstepToPlay];
        GameObject soundPlayer = ObjectPool.instance.objPool_GetObject("2DSoundPlayer");
        if (soundPlayer)
        {
            SoundPlayer spScript = soundPlayer.GetComponent<SoundPlayer>();
            spScript.transform.position = transform.position;

            PlaySoundInfo soundInfo = new PlaySoundInfo(footStepClip);
            soundInfo.pitch = Random.Range(0.7f, 1.3f);
            soundInfo.volume = 0.5f;

            spScript.PlaySound(soundInfo);
        }
    }

    private IEnumerator RotateModelOverTime(float duration)
    {
        Transform t = model != null ? model.transform : transform;
        Quaternion start = t.rotation;
        Quaternion end = Quaternion.LookRotation(direction, Vector3.up);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float tNorm = elapsed / duration;
            tNorm = tNorm * tNorm * (3f - 2f * tNorm);
            t.rotation = Quaternion.Slerp(start, end, tNorm);
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.rotation = end;
    }

    private IEnumerator RotateModelAtSpeed()
    {
        Transform t = model != null ? model.transform : transform;
        Quaternion end = Quaternion.LookRotation(direction, Vector3.up);
        while (Quaternion.Angle(t.rotation, end) > 0.1f)
        {
            t.rotation = Quaternion.RotateTowards(t.rotation, end, rotationReturnSpeed * Time.deltaTime);
            yield return null;
        }
        t.rotation = end;
    }
}
