using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour, ICameraBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The Transform of the object the camera will follow.")]
    public Transform target;
    public Vector3 targetRotation;

    [Header("Smoothing Settings")]
    [Tooltip("Time (in seconds) the camera takes to catch up to the target.")]
    [Min(0f)]
    public float smoothTime = 0.3f;
    public float rotationSpeed = 100f;

    // Internals
    private Vector3 _velocity = Vector3.zero;
    private Vector3 _offset;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("[CameraFollow] No target assigned! Disabling script.");
            enabled = false;
            return;
        }

        // Calculate initial offset based on starting positions
        _offset = transform.position - target.position;

        // If the target has a Rigidbody, turn on interpolation for smoother motion
        var rb = target.GetComponent<Rigidbody>();
        if (rb != null && rb.interpolation == RigidbodyInterpolation.None)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    public void RecalculateOffset()
    {
        if (target == null)
        {
            Debug.LogError("[CameraFollow] No target assigned! Disabling script.");
            enabled = false;
            return;
        }

        _offset = transform.position - target.position;

        var rb = target.GetComponent<Rigidbody>();
        if (rb != null && rb.interpolation == RigidbodyInterpolation.None)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    public void OnActivate()
    {

    }

    public void OnLateUpdate()
    {
        if (target == null) return;

        // Where we want the camera to end up this frame
        Vector3 desiredPosition = target.position + _offset;

        // Smoothly move there, framerate‐independent
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref _velocity,
            smoothTime,
            Mathf.Infinity,
            Time.deltaTime
        );

        if (transform.rotation.eulerAngles != targetRotation)
        {
            transform.rotation =
                Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(targetRotation), Time.deltaTime * rotationSpeed);
        }
    }
}
