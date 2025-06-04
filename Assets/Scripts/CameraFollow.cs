using System.Collections;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The Transform of the object the camera will follow.")]
    public Transform target;

    [Header("Offset & Smoothing")]
    [Tooltip("Offset from the target's position (in local space).")]
    public Vector3 offset = new Vector3(0f, 5f, -10f);
    [Tooltip("How quickly the camera moves to the target position.")]
    [Range(0.01f, 1f)]
    public float smoothSpeed = 0.125f;

    [Header("Manual Move After Dialogue")]
    [Tooltip("Horizontal distance (along +X) to move the camera when triggered.")]
    public float manualOffsetX = 40f;
    [Tooltip("How long (in seconds) the manual move should take to go out and back.")]
    public float manualMoveDuration = 1.5f;

    private bool isManualMoving = false;
    private Vector3 manualStartPos;

    void LateUpdate()
    {
        // If we're in the middle of a manual move, skip the normal follow logic.
        if (isManualMoving) return;

        if (target == null)
            return;

        // Normal follow: interpolated position to target + offset
        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // Always look at the target
        transform.LookAt(target);
    }

    /// <summary>
    /// Call this method right after the specific dialog line finishes.
    /// It will move the camera +manualOffsetX along X, then return it, and resume follow.
    /// </summary>
    public void TriggerManualMove()
    {
        if (isManualMoving || target == null)
            return;

        manualStartPos = transform.position;
        StartCoroutine(ManualMoveRoutine());
    }

    private IEnumerator ManualMoveRoutine()
    {
        isManualMoving = true;

        // Calculate destination: same Y and Z, but +X offset
        Vector3 destPos = manualStartPos + new Vector3(manualOffsetX, 0f, 0f);

        // 1) Move out to destPos over half the duration
        float halfDuration = manualMoveDuration * 0.5f;
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            float t = elapsed / halfDuration;
            float newX = Mathf.Lerp(manualStartPos.x, destPos.x, t);
            Vector3 newPos = new Vector3(newX, manualStartPos.y, manualStartPos.z);
            transform.position = newPos;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = destPos;

        // 2) (Optional) Pause at the offset for a short moment
        yield return new WaitForSeconds(0.5f);

        // 3) Move back to manualStartPos over half the duration
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            float t = elapsed / halfDuration;
            float newX = Mathf.Lerp(destPos.x, manualStartPos.x, t);
            Vector3 newPos = new Vector3(newX, manualStartPos.y, manualStartPos.z);
            transform.position = newPos;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = manualStartPos;

        isManualMoving = false;
    }
}
