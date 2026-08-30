using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Mushrooms : MonoBehaviour
{
    [Header("Bounce Settings")]
    [Tooltip("Force applied upwards when the player hits this.")]
    public float bounceForce = 10f;

    [Tooltip("Apply bounce in this direction. If false, bounce is purely upward.")]
    public bool useSurfaceNormal = false;

    private void Reset()
    {
        // Automatically set trigger on collider
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the other object has a Rigidbody (like the player)
        Rigidbody rb = other.attachedRigidbody;
        if (rb == null || rb.isKinematic)
            return;

        // Optional: only affect objects with a PlayerMovement script
        PlayerMovement player = rb.GetComponent<PlayerMovement>();
        if (player == null)
            return;

        // Bounce direction: use normal or default upward
        Vector3 bounceDir = useSurfaceNormal ? transform.up : Vector3.up;

        // Clear downward velocity before applying bounce
        Vector3 velocity = rb.linearVelocity;
        if (velocity.y < 0f) velocity.y = 0f;
        rb.linearVelocity = velocity;

        // Apply bounce
        rb.AddForce(bounceDir.normalized * bounceForce, ForceMode.Impulse);
    }
}
