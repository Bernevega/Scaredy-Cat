using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The Transform of the object the camera will follow.")]
    public Transform target;

    [Header("Offset & Initial Setup")]
    [Tooltip("How quickly the camera moves toward the target. Lower = more lag.")]
    [Range(0.01f, 1f)]
    public float smoothSpeed = 0.125f;

    [Header("Lag Settings")]
    [Tooltip("How long (in seconds) the camera takes to catch up to the target.")]
    public float followSmoothTime = 0.3f;

    private Vector3 _initialOffset;
    private Vector3 _velocity = Vector3.zero;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("[CameraFollow] No target assigned!");
            enabled = false;
            return;
        }
        // Remember the offset you placed in the Inspector
        _initialOffset = transform.position - target.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Desired position is target + your inspector-defined offset
        Vector3 desiredPos = target.position + _initialOffset;

        // First, do a quick Lerp to loosely track (preserves your smoothSpeed behavior)
        Vector3 intermediatePos = Vector3.Lerp(transform.position, desiredPos, smoothSpeed);

        // Then apply a SmoothDamp to introduce a bit of chase-lag
        transform.position = Vector3.SmoothDamp(
            transform.position,
            intermediatePos,
            ref _velocity,
            followSmoothTime
        );

        // NOTE: We do NOT modify rotation, so your inspector-set rotation remains.
    }
}
