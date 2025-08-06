using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour, ICameraBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public Vector3 targetRotation;

    [Header("Offset Settings")]
    public Vector3 extraRightOffset = Vector3.zero; // Use x = right, y = up, z = forward
    public float returnSpeed = 5f;

    [Header("Smoothing Settings")]
    [Min(0f)] public float smoothTime = 0.3f;
    [Min(0f)] public float introSmoothTime = 1f;
    public float rotationSpeed = 100f;

    // Internals
    private Vector3 _velocity = Vector3.zero;
    private Vector3 _introVelocity = Vector3.zero;
    private Vector3 _originalOffset;
    private bool _isInIntro = true;
    private float _blendOutTimer = 0f;
    private float _blendDuration = 1f;

    private PlayerMovement _playerMovement;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("[CameraFollow] No target assigned! Disabling script.");
            enabled = false;
            return;
        }

        _playerMovement = target.GetComponent<PlayerMovement>();
        if (_playerMovement == null)
            Debug.LogWarning("[CameraFollow] Target has no PlayerMovement component.");

        // Calculate offset based on current editor placement of camera
        _originalOffset = transform.position - target.position;

        // Add extra offset based on camera-relative direction
        _originalOffset += transform.right * extraRightOffset.x;
        _originalOffset += transform.up * extraRightOffset.y;
        _originalOffset += transform.forward * extraRightOffset.z;

        // Enable interpolation if missing
        var rb = target.GetComponent<Rigidbody>();
        if (rb != null && rb.interpolation == RigidbodyInterpolation.None)
            rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (_playerMovement != null)
            _playerMovement.SetCanMove(false);

        StartCoroutine(EndIntroAfterDelay(5f));
    }

    private IEnumerator EndIntroAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        _isInIntro = false;
        _blendOutTimer = 0f;

        if (_playerMovement != null)
            _playerMovement.SetCanMove(true);
    }

    public void RecalculateOffset()
    {
        _originalOffset = transform.position - target.position;

        // Re-apply the extra offset in case it's changed
        _originalOffset += transform.right * extraRightOffset.x;
        _originalOffset += transform.up * extraRightOffset.y;
        _originalOffset += transform.forward * extraRightOffset.z;
    }

    public void OnActivate() { }

    public void OnLateUpdate()
    {
        if (target == null) return;

        Vector3 baseTargetPos = target.position + _originalOffset;
        float deltaTime = Time.deltaTime;
        float effectiveSmoothTime;

        if (_isInIntro)
        {
            effectiveSmoothTime = introSmoothTime;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                baseTargetPos,
                ref _introVelocity,
                effectiveSmoothTime,
                Mathf.Infinity,
                deltaTime
            );
        }
        else if (_blendOutTimer < _blendDuration)
        {
            _blendOutTimer += deltaTime;
            float t = Mathf.Clamp01(_blendOutTimer / _blendDuration);

            effectiveSmoothTime = Mathf.Lerp(introSmoothTime, smoothTime, t);
            Vector3 blendedVelocity = Vector3.Lerp(_introVelocity, _velocity, t);

            transform.position = Vector3.SmoothDamp(
                transform.position,
                baseTargetPos,
                ref blendedVelocity,
                effectiveSmoothTime,
                Mathf.Infinity,
                deltaTime
            );

            _velocity = blendedVelocity; // Store for future use
        }
        else
        {
            effectiveSmoothTime = smoothTime;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                baseTargetPos,
                ref _velocity,
                effectiveSmoothTime,
                Mathf.Infinity,
                deltaTime
            );
        }

        // Rotation toward targetRotation
        if (transform.rotation.eulerAngles != targetRotation)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.Euler(targetRotation),
                rotationSpeed * deltaTime
            );
        }
    }
}
