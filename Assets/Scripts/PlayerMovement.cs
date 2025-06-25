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

    [SerializeField] GameObject model;

    [HideInInspector] public bool blockRightMovement = false;
    [HideInInspector] public bool blockJump = false;  
    [HideInInspector] public bool blockSprint = false;

    private Rigidbody rb;
    private Camera mainCam;
<<<<<<< Updated upstream
    private bool isGrounded;
    private bool canMove = true; 
    
    // Tracks if dialog was open in the previous frame
=======
    private Vector3 direction = Vector3.forward;
    private bool isGrounded = false;
>>>>>>> Stashed changes
    private bool _wasDialogActive = false;
    private bool hasJumped = false;

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
    }

    void Update()
    {
        // 1) Dialog check
        var dialogMgr = SimpleDialogManager.Instance;
        bool dialogActive = dialogMgr != null
                            && dialogMgr.dialogPanel != null
                            && dialogMgr.dialogPanel.activeSelf;

        if (dialogActive)
        {
            _wasDialogActive = true;
            // Stop any residual horizontal movement when dialog opens
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        if (_wasDialogActive)
        {
            _wasDialogActive = false;
            // Ensure no lingering horizontal velocity after dialog closes
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

<<<<<<< Updated upstream
        if (model.transform.forward != direction)
        {
            model.transform.rotation = Quaternion.RotateTowards(model.transform.rotation, Quaternion.LookRotation(direction, Vector3.up), 1080f * Time.deltaTime);
        }

        if (!canMove) return;

        // --- Below here is your normal movement/jump ---

        // Ground check
=======
        // 2) Ground check
>>>>>>> Stashed changes
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded)
            hasJumped = false;

        // 3) Read inputs
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        // Block rightward input if flagged
        if (blockRightMovement && moveX > 0f)
            moveX = 0f;

<<<<<<< Updated upstream
        Vector3 cameraXZ = new Vector3(mainCam.transform.forward.x, 0, mainCam.transform.forward.z).normalized;
        Vector3 cameraXZRight = Vector3.Cross(Vector3.up, cameraXZ);
        Vector3 moveDir = (cameraXZ * moveZ + cameraXZRight * moveX).normalized;
        if (moveDir != Vector3.zero)
        {
            direction = moveDir;
        }
=======
        // Calculate movement direction relative to camera
        Vector3 camF = new Vector3(mainCam.transform.forward.x, 0, mainCam.transform.forward.z).normalized;
        Vector3 camR = Vector3.Cross(Vector3.up, camF);
        Vector3 moveDir = (camF * moveZ + camR * moveX).normalized;
>>>>>>> Stashed changes

        if (moveDir != Vector3.zero)
            direction = moveDir;
        transform.forward = Vector3.RotateTowards(transform.forward, direction, Mathf.Deg2Rad * 1080f * Time.deltaTime, 0f);

        // 4) Sprint (blocked in labyrinth)
        float currentSpeed = speed;
        if (Input.GetKey(KeyCode.LeftShift) && !blockSprint)
            currentSpeed *= runMultiplier;

        // 5) Apply horizontal movement, preserve vertical velocity
        rb.linearVelocity = new Vector3(moveDir.x * currentSpeed, rb.linearVelocity.y, moveDir.z * currentSpeed);

        // 6) Jump (only once until grounded again)
        if (!blockJump && Input.GetButtonDown("Jump") && isGrounded && !hasJumped)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            hasJumped = true;
        }
    }

    public void SetDirection(Vector3 direction)
    { this.direction = Vector3.Normalize(direction); }
    public void SetCanMove(bool b)
    { canMove = b; }
}
