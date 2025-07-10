using UnityEngine;

public class TreeCamOrbitcript : MonoBehaviour
{
    [SerializeField] CameraOrbitBehaviour orbitBehaviour;
    CameraController camController;
    Transform playerTransform = null;

    [SerializeField] Transform centerTransform;
    [SerializeField] TriggerEvent stopOrbitTrigger;
    [SerializeField] CameraFollow camFollow;
    Camera mainCam;
    Vector3 previousCamRotation;

    private void Awake()
    {
        mainCam = Camera.main;
        camController = mainCam.GetComponent<CameraController>();
        stopOrbitTrigger.eventOnTrigger += OnTriggerEvent;
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

    private void OnTriggerEvent(Collider coll, Collider other, bool triggerEnter)
    {
        if (coll.gameObject == stopOrbitTrigger.gameObject &&
            other.gameObject == playerTransform.gameObject)
        {
            if (triggerEnter)
            {
                camFollow.target = playerTransform;

                Vector3 targetPosition = playerTransform.position;
                targetPosition += new Vector3(0, 1.5f, 2.0f);
                camFollow.transform.position = targetPosition;
                camFollow.transform.rotation = mainCam.transform.rotation;
                camFollow.targetRotation = new Vector3(30, 180, 0);
                camFollow.RecalculateOffset();
                camFollow.transform.position = mainCam.transform.position;
                camFollow.enabled = true;
                camController.SetCamBehaviour(camFollow);
            }
            else
            {
                camFollow.enabled = false;
                if (playerTransform != null)
                {
                    camController.SetCamBehaviour(orbitBehaviour);
                }
                else
                {
                    camController.SetCamBehaviour(null);
                }
            }
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

        if (camFollow.enabled)
        {
            mainCam.transform.position = camFollow.transform.position;
            mainCam.transform.rotation = camFollow.transform.rotation;
        }
    }
}
