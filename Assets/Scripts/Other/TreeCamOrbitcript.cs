using UnityEngine;

public class TreeCamOrbitcript : MonoBehaviour
{
    [SerializeField] CameraOrbitBehaviour orbitBehaviour;
    CameraController camController;
    Transform playerTransform = null;

    [SerializeField] Transform centerTransform;
    Camera mainCam;
    Vector3 previousCamRotation;

    private void Awake()
    {
        mainCam = Camera.main;
        camController = mainCam.GetComponent<CameraController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            previousCamRotation = mainCam.transform.rotation.eulerAngles;
            camController.SetCamBehaviour(orbitBehaviour);
            
            playerTransform = other.transform;
            orbitBehaviour.eyeTransform = playerTransform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            camController.SetCamBehaviour(camController.GetComponent<ICameraBehaviour>());

            playerTransform = null;
            orbitBehaviour.eyeTransform = null;
        }
    }

    private void Update()
    {
        if (playerTransform)
        {
            Vector3 targetPosition = centerTransform.position;
            targetPosition.y = playerTransform.position.y;
            centerTransform.position = targetPosition;
        }
    }
}
