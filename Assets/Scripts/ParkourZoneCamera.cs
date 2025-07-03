using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ParkourZoneCamera : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag in your Player’s Transform here.")]
    public Transform player;
    [Tooltip("Drag in your Player’s Rigidbody here.")]
    public Rigidbody playerRigidbody;
    [Tooltip("Drag in your Camera’s Transform (e.g. Camera.main.transform).")]
    public Transform cameraTransform;

    [Header("Behind‐Player Offsets")]
    [Tooltip("How far behind the player the camera sits.")]
    public float behindDistance = 5f;
    [Tooltip("How high above the player the camera sits.")]
    public float heightOffset = 2f;

    [Header("Smoothing")]
    [Tooltip("Seconds to smooth the camera’s position.")]
    [Min(0f)] public float positionSmoothTime = 0.2f;
    [Tooltip("Seconds to smooth the camera’s rotation.")]
    [Min(0f)] public float rotationSmoothTime = 0.2f;

    [Header("Jump‐Yaw Settings")]
    [Tooltip("Vertical speed (m/s) above which the camera will begin yawing right.")]
    public float jumpVelocityThreshold = 1f;
    [Tooltip("Vertical speed (m/s) at which the camera hits its max yaw.")]
    public float maxJumpVelocity = 5f;
    [Tooltip("Maximum yaw angle (degrees) when at or above maxJumpVelocity.")]
    public float maxJumpYawAngle = 15f;

    // internal
    bool   _inZone = false;
    Vector3 _posSmoothVel;

    void Reset()
    {
        // ensure the collider is a trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        // you can also compare tags/layers if you prefer
        if (other.transform == player || other.attachedRigidbody == playerRigidbody)
            _inZone = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.transform == player || other.attachedRigidbody == playerRigidbody)
            _inZone = false;
    }

    void LateUpdate()
    {
        if (!_inZone || player == null || cameraTransform == null || playerRigidbody == null)
            return;

        // —— 1) Position: keep behind & above the player ——
        Vector3 desiredPos = player.position
                           - player.forward * behindDistance
                           + Vector3.up * heightOffset;

        cameraTransform.position = Vector3.SmoothDamp(
            cameraTransform.position,
            desiredPos,
            ref _posSmoothVel,
            positionSmoothTime
        );

        // —— 2) Base rotation: look at the player —— 
        Vector3 lookTarget = player.position + Vector3.up * (heightOffset * 0.5f);
        Quaternion baseRot = Quaternion.LookRotation(
            lookTarget - cameraTransform.position,
            Vector3.up
        );

        // —— 3) Compute extra yaw based on vertical speed —— 
        float vy = playerRigidbody.linearVelocity.y;
        float yawOffset = 0f;
        if (vy > jumpVelocityThreshold)
        {
            // t goes from 0→1 as vy climbs from threshold→max
            float t = Mathf.InverseLerp(jumpVelocityThreshold, maxJumpVelocity, vy);
            yawOffset = Mathf.Lerp(0f, maxJumpYawAngle, t);
        }

        // —— 4) Apply yaw to the right (around world-up) —— 
        Quaternion yawQ = Quaternion.AngleAxis(yawOffset, Vector3.up);
        Quaternion desiredRot = yawQ * baseRot;

        // —— 5) Smoothly rotate there —— 
        cameraTransform.rotation = Quaternion.Slerp(
            cameraTransform.rotation,
            desiredRot,
            Time.deltaTime / rotationSmoothTime
        );
    }
}
