using UnityEngine;

public class CameraOrbitBehaviour : MonoBehaviour, ICameraBehaviour
{
    
    [SerializeField] Vector3 offsetPosition;
    [SerializeField] Vector3 offsetRotation;

    public Transform centerTransform;
    public Transform eyeTransform;

    public float moveSpeed = 9;
    public float rotateSpeed = 50f;

    Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    public void OnLateUpdate()
    {
        Quaternion targetRotationQuaternion = Quaternion.LookRotation(centerTransform.position - eyeTransform.position);
        Quaternion offsetRotationQuaternion = Quaternion.Euler(offsetRotation);
        targetRotationQuaternion = targetRotationQuaternion * offsetRotationQuaternion;

        Vector3 rotatedOffsetPos = targetRotationQuaternion * offsetPosition;

        mainCam.transform.rotation =
            Quaternion.RotateTowards(mainCam.transform.rotation, targetRotationQuaternion, rotateSpeed * Time.deltaTime);
        mainCam.transform.position = 
            Vector3.MoveTowards(mainCam.transform.position, eyeTransform.position + rotatedOffsetPos, Time.deltaTime * moveSpeed);
    }
}
