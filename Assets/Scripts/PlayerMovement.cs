using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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
    public float rotationReturnDuration = 0.5f;
    public float rotationReturnSpeed = 1080f;

    [Header("Scene Start")]
    [Tooltip("If checked, triggers WakeUp() automatically when the scene starts.")]
    [SerializeField] private bool playWakeUpOnSceneStart = true;
    [Tooltip("Optional small delay before triggering WakeUp on scene start.")]
    [SerializeField] private float wakeUpSceneStartDelay = 0f;

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

    public Animator GetAnimator() { return animator; }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        mainCam = Camera.main;

        Collider col = GetComponent<Collider>();
        var slideMat = new PhysicsMaterial("SlideMat")
        {
            staticFriction = 0f,
            dynamicFriction = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum
        };
        col.material = slideMat;

        if (animEvents) animEvents.stringEvent += AnimStringEvent;

        // Auto-play wake up at scene start (optional)
        if (playWakeUpOnSceneStart)
        {
            if (wakeUpSceneStartDelay > 0f)
                StartCoroutine(CoDelayedWakeUp(wakeUpSceneStartDelay));
            else
                WakeUp();
        }
    }

    private IEnumerator CoDelayedWakeUp(float delay)
    {
        yield return new WaitForSeconds(delay);
        WakeUp();
    }

    void Update()
    {
        if (mainCam == null) mainCam = Camera.main;

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        animator.SetBool("grounded", isGrounded);
        if (isGrounded) hasJumped = false;

        var dialogMgr = SimpleDialogManager.Instance;
        bool dialogActive = dialogMgr != null
                            && dialogMgr.dialogPanel != null
                            && dialogMgr.dialogPanel.activeSelf;

        if (dialogActive)
        {
            _wasDialogActive = true;
            canMove = false;
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            animator.SetBool("moveInput", false);
            animator.SetBool("isRunning", false);
            animator.Play("Idle");
            return;
        }

        if (_wasDialogActive)
        {
            _wasDialogActive = false;
            canMove = true;
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            if (isGrounded) moveState = MoveState.Idle;
            StopAllCoroutines();
            if (rotationReturnDuration > 0f)
                StartCoroutine(RotateModelOverTime(rotationReturnDuration));
            else
                StartCoroutine(RotateModelAtSpeed());
            return;
        }

        if (!canMove) return;

        // --- Input values ---
        float kbX = Input.GetAxisRaw("Horizontal");
        float kbZ = Input.GetAxisRaw("Vertical");
        bool jumpKb = Input.GetButtonDown("Jump");
        bool sprintKb = Input.GetKey(KeyCode.LeftShift);
        bool interactKb = Input.GetKeyDown(KeyCode.E);
        bool toggleQuestKb = Input.GetKeyDown(KeyCode.Q);

        float gpX = 0f, gpZ = 0f;
        bool jumpGp = false;
        bool sprintGp = false;
        bool interactGp = false;
        bool toggleQuestGp = false;

#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            gpX = stick.x;
            gpZ = stick.y;
            jumpGp = Gamepad.current.buttonSouth.wasPressedThisFrame;
            sprintGp = Gamepad.current.leftStickButton.isPressed;
            interactGp = Gamepad.current.buttonEast.wasPressedThisFrame;
            toggleQuestGp = Gamepad.current.buttonNorth.wasPressedThisFrame;

            // Toggle Quest with Gamepad
            if (toggleQuestGp)
            {
                QuestManager.instance?.ToggleQuestPanel();
            }
        }

        if (Keyboard.current != null && toggleQuestKb)
        {
            QuestManager.instance?.ToggleQuestPanel();
        }
#else
        if (toggleQuestKb)
        {
            QuestManager.instance?.ToggleQuestPanel();
        }
#endif
        
        // --- Combine Inputs ---
        Vector2 rawMove = new Vector2(kbX + gpX, kbZ + gpZ);
        if (rawMove.sqrMagnitude > 1f) rawMove.Normalize();
        float moveX = rawMove.x;
        float moveZ = rawMove.y;
        bool hasInput = rawMove.sqrMagnitude > 0f;
        bool jumpPressed = jumpKb || jumpGp;
        bool sprintInput = (sprintKb || sprintGp) && !blockSprint;
        bool interact = interactKb || interactGp;

        

        if (speed > 0)
        {
            animator.SetBool("moveInput", hasInput);

            Vector3 camF = new Vector3(mainCam.transform.forward.x, 0f, mainCam.transform.forward.z).normalized;
            Vector3 camR = Vector3.Cross(Vector3.up, camF);
            Vector3 moveDir = (camF * moveZ + camR * moveX).normalized;
            if (moveDir != Vector3.zero) direction = moveDir;

            float currentSpeed = speed;
            if (hasInput && sprintInput)
            {
                currentSpeed *= runMultiplier;
                animator.SetBool("isRunning", true);
            }
            else
            {
                animator.SetBool("isRunning", false);
            }

            rb.linearVelocity = new Vector3(moveDir.x * currentSpeed, rb.linearVelocity.y, moveDir.z * currentSpeed);

            if (moveState == MoveState.Idle && !blockJump && jumpPressed && isGrounded && !hasJumped)
            {
                animator.SetTrigger("jump");
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                hasJumped = true;
                moveState = MoveState.Jumping;
            }
        }

        if (interact)
        {
            animator.SetTrigger("interact");
        }

        Transform t = model != null ? model.transform : transform;
        Quaternion desired = Quaternion.LookRotation(direction, Vector3.up);
        if (t.rotation != desired)
            t.rotation = Quaternion.RotateTowards(t.rotation, desired, 1080f * Time.deltaTime);
    }

    public void SetDirection(Vector3 dir) => direction = dir.normalized;
    public void SetCanMove(bool b) => canMove = b;

    public void WakeUp()
    {
        moveState = MoveState.WakingUp;
        animator.SetTrigger("WakeUp");
        canMove = false;
    }

    public void AnimStringEvent(string str)
    {
        switch (str)
        {
            case "JumpLiftOff": JumpLiftOff(); break;
            case "LandStart": LandStart(); break;
            case "LandEnd": LandEnd(); break;
            case "WakeUpEnd": WakeUpEnd(); break;
            case "FootStep": MakeFootstep(); break;
        }
    }

    private void JumpLiftOff()
    {
        var soundPlayer = ObjectPool.instance.objPool_GetObject("2DSoundPlayer");
        if (soundPlayer)
        {
            var sp = soundPlayer.GetComponent<SoundPlayer>();
            sp.transform.position = transform.position;
            var info = new PlaySoundInfo(jumpSoundEffect)
            {
                pitch = Random.Range(0.7f, 1.3f),
                volume = 0.5f
            };
            sp.PlaySound(info);
        }
    }

    private void LandStart()
    {
        moveState = MoveState.Landing;
        var soundPlayer = ObjectPool.instance.objPool_GetObject("2DSoundPlayer");
        if (soundPlayer)
        {
            var sp = soundPlayer.GetComponent<SoundPlayer>();
            sp.transform.position = transform.position;
            var info = new PlaySoundInfo(landSoundEffect)
            {
                pitch = Random.Range(0.7f, 1.3f),
                volume = 0.5f
            };
            sp.PlaySound(info);
        }
    }

    private void LandEnd()
    {
        moveState = MoveState.Idle;
    }

    private void WakeUpEnd()
    {
        moveState = MoveState.Idle;
        canMove = true;
        Debug.Log("Wake up end");
    }

    private void MakeFootstep()
    {
        int idx = Random.Range(0, walkSoundEffects.Length);
        var clip = walkSoundEffects[idx];
        var soundPlayer = ObjectPool.instance.objPool_GetObject("2DSoundPlayer");
        if (soundPlayer)
        {
            var sp = soundPlayer.GetComponent<SoundPlayer>();
            sp.transform.position = transform.position;
            var info = new PlaySoundInfo(clip)
            {
                pitch = Random.Range(0.7f, 1.3f),
                volume = 0.5f
            };
            sp.PlaySound(info);
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
