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

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void Update()
    {
        if (SimpleDialogManager.Instance != null && SimpleDialogManager.Instance.dialogPanel.activeSelf)
            return;

        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // Read inputs
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        
        // **Block rightward input if flagged**
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

        // Jump
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
}
