using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour, ICameraBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The Transform of the object the camera will follow.")]
    public Transform target;
    [Tooltip("Euler angles for the camera’s rotation.")]
    public Vector3 targetRotation;

    [Header("Offset Settings")]
    [Tooltip("How far to the right (in world-units) the camera should start.")]
    public Vector3 extraRightOffset = Vector3.zero;
    [Tooltip("Speed at which the camera moves from extra offset back to the original.")]
    public float returnSpeed = 5f;

    [Header("Smoothing Settings")]
    [Tooltip("Time (in seconds) the camera takes to catch up to the target normally.")]
    [Min(0f)] public float smoothTime = 0.3f;
    [Tooltip("Time (in seconds) the camera takes to catch up to the target during the intro smoothing.")]
    [Min(0f)] public float introSmoothTime = 1f;
    [Tooltip("Rotation speed (degrees/sec).")]
    public float rotationSpeed = 100f;

    // internals
    private Vector3 _velocity = Vector3.zero;
    private Vector3 _introVelocity = Vector3.zero;
    private Vector3 _originalOffset;
    private bool _hasAppliedExtra = false;

    // NEW: reference to your PlayerMovement component
    private PlayerMovement _playerMovement;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("[CameraFollow] No target assigned! Disabling script.");
            enabled = false;
            return;
        }

        // cache the PlayerMovement so we can toggle movement
        _playerMovement = target.GetComponent<PlayerMovement>();
        if (_playerMovement == null)
            Debug.LogWarning("[CameraFollow] Target has no PlayerMovement component – can't block movement during intro.");

        // store the offset we actually want long-term
        _originalOffset = transform.position - target.position;

        // immediately place camera at original + extra
        transform.position = target.position + _originalOffset + extraRightOffset;
        _hasAppliedExtra = true;

        // enable interpolation on rigidbodies for smooth follow
        var rb = target.GetComponent<Rigidbody>();
        if (rb != null && rb.interpolation == RigidbodyInterpolation.None)
            rb.interpolation = RigidbodyInterpolation.Interpolate;

        // block player movement during the intro
        if (_playerMovement != null)
            _playerMovement.SetCanMove(false);
    }

    public void RecalculateOffset()
    {
        _originalOffset = transform.position - target.position;
    }

    public void OnActivate() { }

    public void OnLateUpdate()
    {
        if (target == null) return;

        // determine the target position without extra offset
        Vector3 baseTargetPos = target.position + _originalOffset;

        if (_hasAppliedExtra)
        {
            // Intro smoothing phase
            transform.position = Vector3.SmoothDamp(
                transform.position,
                baseTargetPos,
                ref _introVelocity,
                introSmoothTime,
                Mathf.Infinity,
                Time.deltaTime
            );

            // Once we've essentially reached it, switch to normal smoothing
            if (Vector3.Distance(transform.position, baseTargetPos) < 0.01f)
            {
                _hasAppliedExtra = false;

                // Re-enable player movement now that the intro is done
                if (_playerMovement != null)
                    _playerMovement.SetCanMove(true);
            }
        }
        else
        {
            // Normal smooth-damp follow
            transform.position = Vector3.SmoothDamp(
                transform.position,
                baseTargetPos,
                ref _velocity,
                smoothTime,
                Mathf.Infinity,
                Time.deltaTime
            );
        }

        // rotation
        if (transform.rotation.eulerAngles != targetRotation)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.Euler(targetRotation),
                rotationSpeed * Time.deltaTime
            );
        }
    }
}
