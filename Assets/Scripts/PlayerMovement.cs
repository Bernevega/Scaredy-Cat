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

    [HideInInspector] public bool blockRightMovement = false;

    private Rigidbody rb;
    private bool isGrounded;
    
    // Tracks if dialog was open in the previous frame
    private bool _wasDialogActive = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
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

        // --- Below here is your normal movement/jump ---

        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // Read inputs
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        // Block rightward input if flagged
        if (blockRightMovement && moveX > 0f)
            moveX = 0f;

        Vector3 moveDir = (transform.right * moveX + transform.forward * moveZ).normalized;

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
}
