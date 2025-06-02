using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f; // Movement speed
    public float jumpForce = 5f; // Jumping strength

    [Header("Ground Check Settings")]
    public Transform groundCheck; // Ground check (below player's feet)
    public float groundDistance = 0.2f; // Radius of the sphere used to check for ground
    public LayerMask groundMask; // Which layers count as "ground"

    private Rigidbody rb;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Make sure the Rigidbody cannot rotate (so the player doesn't tip over)
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void Update()
    {
        // Check if the player is on the ground by creating a small invisible sphere at the feet
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // Read input axes (WASD / arrow keys).
        float moveX = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right
        float moveZ = Input.GetAxisRaw("Vertical");   // W/S or Up/Down
        
        // Build a movement direction vector relative to the player’s orientation
        Vector3 moveDir = transform.right * moveX + transform.forward * moveZ;
        moveDir.Normalize(); // Make sure diagonal movement isn't faster

        // Compute the desired horizontal velocity (keep the existing vertical velocity)
        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 targetVelocity = moveDir * speed;
        rb.linearVelocity = new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z);

        // Jump: if the player presses Space AND is grounded, apply an upward impulse
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            // Zero out any small downward velocity before jumping
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
}
