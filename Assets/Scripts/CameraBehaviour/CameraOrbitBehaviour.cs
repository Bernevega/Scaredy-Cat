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

    Vector3 dirToCamNonOffset = Vector3.up;
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

        
        Vector3 camToEyeDiff = camNonOffsetPos - eyeTransform.position;
        camToEyeDiff = Vector3.RotateTowards(camToEyeDiff, -eyeToCenterDir, Mathf.Deg2Rad * 100f * Time.deltaTime, 0f);
        camNonOffsetPos = eyeTransform.position + camToEyeDiff;
        camNonOffsetPos = Vector3.MoveTowards(camNonOffsetPos, eyeTransform.position - eyeToCenterDir * 0.001f, moveSpeed * Time.deltaTime);
        

        Vector3 camNonOffsetToEyeDir = eyeTransform.position - camNonOffsetPos;
        camNonOffsetToEyeDir.Normalize();

        Debug.DrawLine(eyeTransform.position, camNonOffsetPos, Color.cyan);
        Debug.DrawLine(camNonOffsetPos, camNonOffsetPos + camNonOffsetToEyeDir, Color.red);

        targetRotationQuaternion = Quaternion.LookRotation(camNonOffsetToEyeDir); 
        //if (camToEyeDiff.sqrMagnitude < 1f)
            offsetRotationQuaternion = Quaternion.Euler(offsetRotation);
        targetRotationQuaternion = targetRotationQuaternion * offsetRotationQuaternion; // Final target rotation

        Vector3 rotatedOffsetPos = Vector3.zero;
        //if (camToEyeDiff.sqrMagnitude < 1f)
            rotatedOffsetPos = targetRotationQuaternion * offsetPosition;



        mainCam.transform.rotation =
            Quaternion.RotateTowards(mainCam.transform.rotation, targetRotationQuaternion, rotateSpeed * Time.deltaTime);    
        mainCam.transform.position = 
            Vector3.MoveTowards(mainCam.transform.position, camNonOffsetPos + rotatedOffsetPos, 2 * Time.deltaTime * moveSpeed);

    }
}
