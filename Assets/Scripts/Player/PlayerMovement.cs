using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

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

    [Header("Movement Constraints")]
    [Tooltip("If enabled, the player can only move left/right relative to the camera.")]
    public bool leftRightOnly = false;

    [SerializeField] private GameObject model;
    [SerializeField] private Animator animator;
    [SerializeField] private AnimEventInvoker animEvents;
    [SerializeField] private AudioClip[] walkSoundEffects;
    [SerializeField] private AudioClip jumpSoundEffect;
    [SerializeField] private AudioClip landSoundEffect;

    [Header("Audio Routing")]
    [Tooltip("Mixer group where all Player SFX should be routed.")]
    public AudioMixerGroup soundsOutput;

    [HideInInspector] public bool blockRightMovement = false;
    [HideInInspector] public bool blockJump = false;
    [HideInInspector] public bool blockSprint = false;

    [Header("Rotation Reset Settings")]
    public float rotationReturnDuration = 0.5f;
    public float rotationReturnSpeed = 1080f;

    [Header("Scene Start")]
    [Tooltip("If checked, triggers WakeUp() automatically when the scene starts.")]
    [SerializeField] private bool playWakeUpOnSceneStart = true;

    [Tooltip("Optional delay before triggering WakeUp.")]
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
        WakingUp
    }

    public Animator GetAnimator()
    {
        return animator;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        mainCam = Camera.main;

        Collider col = GetComponent<Collider>();

        PhysicsMaterial slideMat = new PhysicsMaterial("SlideMat")
        {
            staticFriction = 0f,
            dynamicFriction = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum
        };

        col.material = slideMat;

        if (animEvents != null)
            animEvents.stringEvent += AnimStringEvent;

        if (playWakeUpOnSceneStart)
        {
            if (wakeUpSceneStartDelay > 0f)
                StartCoroutine(CoDelayedWakeUp(wakeUpSceneStartDelay));
            else
                WakeUp();
        }

        SetDirection(transform.forward);
    }

    private IEnumerator CoDelayedWakeUp(float delay)
    {
        yield return new WaitForSeconds(delay);
        WakeUp();
    }

    private void Update()
    {
        if (mainCam == null)
            mainCam = Camera.main;

        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundDistance,
            groundMask
        );

        animator.SetBool("grounded", isGrounded);

        if (isGrounded)
            hasJumped = false;

        SimpleDialogManager dialogMgr = SimpleDialogManager.Instance;

        bool dialogActive =
            dialogMgr != null &&
            dialogMgr.dialogPanel != null &&
            dialogMgr.dialogPanel.activeSelf;

        if (dialogActive)
        {
            _wasDialogActive = true;
            canMove = false;

            rb.linearVelocity = new Vector3(
                0f,
                rb.linearVelocity.y,
                0f
            );

            animator.SetBool("moveInput", false);
            animator.SetBool("isRunning", false);
            animator.Play("Idle");

            return;
        }

        if (_wasDialogActive)
        {
            _wasDialogActive = false;
            canMove = true;

            rb.linearVelocity = new Vector3(
                0f,
                rb.linearVelocity.y,
                0f
            );

            if (isGrounded)
                moveState = MoveState.Idle;

            StopAllCoroutines();

            if (rotationReturnDuration > 0f)
                StartCoroutine(
                    RotateModelOverTime(rotationReturnDuration)
                );
            else
                StartCoroutine(RotateModelAtSpeed());

            return;
        }

        if (!canMove)
            return;

        // Keyboard input
        float kbX = Input.GetAxisRaw("Horizontal");
        float kbZ = Input.GetAxisRaw("Vertical");

        bool jumpKb = Input.GetButtonDown("Jump");
        bool sprintKb = Input.GetKey(KeyCode.LeftShift);
        bool interactKb = Input.GetKeyDown(KeyCode.E);
        bool toggleQuestKb = Input.GetKeyDown(KeyCode.Q);

        // Q toggles QuestInfoPanel.
        if (toggleQuestKb)
        {
            QuestManager.instance?.ToggleQuestPanel();
        }

        Vector2 rawMove = new Vector2(
            kbX,
            kbZ
        );

        if (rawMove.sqrMagnitude > 1f)
            rawMove.Normalize();

        float moveX = rawMove.x;
        float moveZ = rawMove.y;

        if (leftRightOnly)
            moveZ = 0f;

        bool hasInput = moveX != 0f || moveZ != 0f;
        bool jumpPressed = jumpKb;

        bool sprintInput =
            sprintKb &&
            !blockSprint;

        bool interact = interactKb;

        if (speed > 0f)
        {
            animator.SetBool("moveInput", hasInput);

            Vector3 camF = new Vector3(
                mainCam.transform.forward.x,
                0f,
                mainCam.transform.forward.z
            ).normalized;

            Vector3 camR = Vector3.Cross(
                Vector3.up,
                camF
            );

            Vector3 moveDir =
                camF * moveZ +
                camR * moveX;

            if (moveDir.sqrMagnitude > 0f)
                moveDir.Normalize();

            if (moveDir != Vector3.zero)
                direction = moveDir;

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

            // Prevent movement toward the right when blocked.
            if (blockRightMovement && moveX > 0f)
            {
                moveDir = Vector3.zero;
            }

            rb.linearVelocity = new Vector3(
                moveDir.x * currentSpeed,
                rb.linearVelocity.y,
                moveDir.z * currentSpeed
            );

            if (
                moveState == MoveState.Idle &&
                !blockJump &&
                jumpPressed &&
                isGrounded &&
                !hasJumped
            )
            {
                animator.SetTrigger("jump");

                rb.linearVelocity = new Vector3(
                    rb.linearVelocity.x,
                    0f,
                    rb.linearVelocity.z
                );

                rb.AddForce(
                    Vector3.up * jumpForce,
                    ForceMode.Impulse
                );

                hasJumped = true;
                moveState = MoveState.Jumping;
            }
        }

        if (interact)
            animator.SetTrigger("interact");

        Transform modelTransform =
            model != null
                ? model.transform
                : transform;

        Quaternion desiredRotation =
            Quaternion.LookRotation(
                direction,
                Vector3.up
            );

        if (modelTransform.rotation != desiredRotation)
        {
            modelTransform.rotation =
                Quaternion.RotateTowards(
                    modelTransform.rotation,
                    desiredRotation,
                    1080f * Time.deltaTime
                );
        }
    }

    public void SetDirection(Vector3 dir)
    {
        direction = dir.normalized;
    }

    public void SetCanMove(bool value)
    {
        canMove = value;
    }

    public void WakeUp()
    {
        moveState = MoveState.WakingUp;
        animator.SetTrigger("WakeUp");
        canMove = false;
    }

    public void AnimStringEvent(string eventName)
    {
        switch (eventName)
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

            case "FootStep":
                MakeFootstep();
                break;
        }
    }

    private void JumpLiftOff()
    {
        GameObject soundPlayer =
            ObjectPool.instance.objPool_GetObject(
                "2DSoundPlayer"
            );

        if (soundPlayer != null)
        {
            SoundPlayer sp =
                soundPlayer.GetComponent<SoundPlayer>();

            sp.transform.position = transform.position;

            PlaySoundInfo info =
                new PlaySoundInfo(jumpSoundEffect)
                {
                    pitch = Random.Range(0.7f, 1.3f),
                    volume = 0.5f,
                    mixer = soundsOutput
                };

            sp.PlaySound(info);
        }
    }

    private void LandStart()
    {
        moveState = MoveState.Landing;

        GameObject soundPlayer =
            ObjectPool.instance.objPool_GetObject(
                "2DSoundPlayer"
            );

        if (soundPlayer != null)
        {
            SoundPlayer sp =
                soundPlayer.GetComponent<SoundPlayer>();

            sp.transform.position = transform.position;

            PlaySoundInfo info =
                new PlaySoundInfo(landSoundEffect)
                {
                    pitch = Random.Range(0.7f, 1.3f),
                    volume = 0.5f,
                    mixer = soundsOutput
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
        if (walkSoundEffects == null ||
            walkSoundEffects.Length == 0)
        {
            return;
        }

        int index =
            Random.Range(0, walkSoundEffects.Length);

        AudioClip clip = walkSoundEffects[index];

        GameObject soundPlayer =
            ObjectPool.instance.objPool_GetObject(
                "2DSoundPlayer"
            );

        if (soundPlayer != null)
        {
            SoundPlayer sp =
                soundPlayer.GetComponent<SoundPlayer>();

            sp.transform.position = transform.position;

            PlaySoundInfo info =
                new PlaySoundInfo(clip)
                {
                    pitch = Random.Range(0.7f, 1.3f),
                    volume = 0.5f,
                    mixer = soundsOutput
                };

            sp.PlaySound(info);
        }
    }

    private IEnumerator RotateModelOverTime(float duration)
    {
        Transform modelTransform =
            model != null
                ? model.transform
                : transform;

        Quaternion startRotation =
            modelTransform.rotation;

        Quaternion endRotation =
            Quaternion.LookRotation(
                direction,
                Vector3.up
            );

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float normalizedTime =
                elapsed / duration;

            normalizedTime =
                normalizedTime *
                normalizedTime *
                (3f - 2f * normalizedTime);

            modelTransform.rotation =
                Quaternion.Slerp(
                    startRotation,
                    endRotation,
                    normalizedTime
                );

            elapsed += Time.deltaTime;
            yield return null;
        }

        modelTransform.rotation = endRotation;
    }

    private IEnumerator RotateModelAtSpeed()
    {
        Transform modelTransform =
            model != null
                ? model.transform
                : transform;

        Quaternion endRotation =
            Quaternion.LookRotation(
                direction,
                Vector3.up
            );

        while (
            Quaternion.Angle(
                modelTransform.rotation,
                endRotation
            ) > 0.1f
        )
        {
            modelTransform.rotation =
                Quaternion.RotateTowards(
                    modelTransform.rotation,
                    endRotation,
                    rotationReturnSpeed *
                    Time.deltaTime
                );

            yield return null;
        }

        modelTransform.rotation = endRotation;
    }
}