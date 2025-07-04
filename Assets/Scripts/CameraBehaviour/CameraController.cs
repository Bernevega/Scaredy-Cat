using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] GameObject startingCameraBehaviour;
    ICameraBehaviour camBehaviour;

    private void Awake()
    {
        if (startingCameraBehaviour != null)
        {
            camBehaviour = startingCameraBehaviour.GetComponent<ICameraBehaviour>();
        }
    }

    private void LateUpdate()
    {
        if (camBehaviour != null)
        {
            camBehaviour.OnLateUpdate();
        }
    }

    public void SetCamBehaviour(ICameraBehaviour camBehaviour)
    { this.camBehaviour = camBehaviour; }
}
