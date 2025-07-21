using UnityEngine;

public class LookAtPlayer : MonoBehaviour
{
    [Tooltip("The player Transform the NPC should look at.")]
    public Transform player;

    [Tooltip("How fast the NPC rotates to look at the player.")]
    public float rotationSpeed = 5f;

    [Tooltip("Only rotate around Y axis (e.g., for humanoids).")]
    public bool onlyRotateOnYAxis = true;

    void Update()
    {
        if (player == null) return;

        Vector3 direction = player.position - transform.position;
        
        if (onlyRotateOnYAxis)
            direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
