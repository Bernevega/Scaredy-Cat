using Unity.VisualScripting;
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

    Vector3 prevRotatedOffsetPos = Vector3.zero;
    Vector3 camNonOffsetPos = Vector3.zero;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    public void OnActivate()
    {
        camNonOffsetPos = mainCam.transform.position;
    }

    public void OnLateUpdate()
    {
        Quaternion targetRotationQuaternion = Quaternion.identity;
        Quaternion currentRotationQuaternion = Quaternion.identity;
        Quaternion offsetRotationQuaternion = Quaternion.identity;
        
        Vector3 eyeToCenterDir = (centerTransform.position - eyeTransform.position).normalized;

        if (eyeToCenterDir.sqrMagnitude < 0.00001f)
        {
            eyeToCenterDir = Vector3.down;
        }

        targetRotationQuaternion = Quaternion.LookRotation(eyeToCenterDir); 
        offsetRotationQuaternion = Quaternion.Euler(offsetRotation);
        targetRotationQuaternion = targetRotationQuaternion * offsetRotationQuaternion; // Final target rotation

        Vector3 rotatedOffsetPos = targetRotationQuaternion * offsetPosition;

        mainCam.transform.rotation =
            Quaternion.RotateTowards(mainCam.transform.rotation, targetRotationQuaternion, rotateSpeed * Time.deltaTime);    
        mainCam.transform.position = 
            Vector3.MoveTowards(mainCam.transform.position, eyeTransform.position + rotatedOffsetPos, Time.deltaTime * moveSpeed);
     
        prevRotatedOffsetPos = rotatedOffsetPos;
    }
}
