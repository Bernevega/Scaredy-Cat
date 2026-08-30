using UnityEngine;

public class LookAtPlayer : MonoBehaviour
{
    [Tooltip("The player Transform the NPC should look at.")]
    public Transform player;

    [Tooltip("Maximum distance at which the NPC looks at the player.")]
    public float lookRadius = 1.5f;

    [Tooltip("How fast the NPC rotates.")]
    public float rotationSpeed = 5f;

    [Tooltip("Only rotate around the Y axis.")]
    public bool onlyRotateOnYAxis = true;

    private Quaternion originalRotation;

    private void Awake()
    {
        originalRotation = transform.rotation;
    }

    private void Update()
    {
        if (player == null)
        {
            ReturnToOriginalRotation();
            return;
        }

        Vector3 direction = player.position - transform.position;

        // Return to the original rotation if the player is outside the radius.
        if (direction.sqrMagnitude > lookRadius * lookRadius)
        {
            ReturnToOriginalRotation();
            return;
        }

        if (onlyRotateOnYAxis)
            direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void ReturnToOriginalRotation()
    {
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            originalRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}