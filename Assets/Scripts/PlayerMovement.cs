using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
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

    private Rigidbody rb;
    private Vector3 direction = new Vector3(0, 0, 1);
    private Camera mainCam;
    private bool isGrounded;
    private bool canMove = true; 
    
    // Tracks if dialog was open in the previous frame
    private bool _wasDialogActive = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        mainCam = Camera.main;
    }

    void Update()
    {
        // 1) See if dialog is up right now
        var dialogMgr = SimpleDialogManager.Instance;
        bool dialogActive = dialogMgr != null
                            && dialogMgr.dialogPanel != null
                            && dialogMgr.dialogPanel.activeSelf;

        // 2) If dialog is active, mark that and bail out
        if (dialogActive)
        {
            _wasDialogActive = true;
            return;
        }

        // 3) If dialog just closed this frame, skip this Update entirely
        if (_wasDialogActive)
        {
            _wasDialogActive = false;
            return;
        }

        if (model.transform.forward != direction)
        {
            model.transform.rotation = Quaternion.RotateTowards(model.transform.rotation, Quaternion.LookRotation(direction, Vector3.up), 1080f * Time.deltaTime);
        }

        if (!canMove) return;

        // --- Below here is your normal movement/jump ---

        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // Read inputs
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        // Block rightward input if flagged
        if (blockRightMovement && moveX > 0f)
            moveX = 0f;

        Vector3 cameraXZ = new Vector3(mainCam.transform.forward.x, 0, mainCam.transform.forward.z).normalized;
        Vector3 cameraXZRight = Vector3.Cross(Vector3.up, cameraXZ);
        Vector3 moveDir = (cameraXZ * moveZ + cameraXZRight * moveX).normalized;
        if (moveDir != Vector3.zero)
        {
            direction = moveDir;
        }

        // Running
        float currentSpeed = speed;
        if (Input.GetKey(KeyCode.LeftShift))
            currentSpeed *= runMultiplier;

        // Apply velocity
        Vector3 targetVel = new Vector3(moveDir.x * currentSpeed,
                                        rb.linearVelocity.y,
                                        moveDir.z * currentSpeed);
        rb.linearVelocity = targetVel;

        // Jump (will never fire on the same frame the dialog closed)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            // reset y‐velocity then impulse
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    public void SetDirection(Vector3 direction)
    { this.direction = Vector3.Normalize(direction); }
    public void SetCanMove(bool b)
    { canMove = b; }
}
