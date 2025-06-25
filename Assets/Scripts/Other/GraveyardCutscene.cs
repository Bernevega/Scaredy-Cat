using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class GraveyardCutscene : MonoBehaviour
{
    Camera mainCam;
    CameraFollow camFollow;
    Collider triggerCollider;

    [SerializeField] AutoRotate oyen;
    PlayerMovement player;

    [SerializeField] Transform[] cameraWaypoints;
    [SerializeField] Transform[] oyenWaypoints;
    [SerializeField] Transform[] kittyWaypoints;

    int cameraStep = 0;
    int oyenStep = 0;
    int kittyStep = 0;

    enum CameraState
    {
        Moving,
        Idle,
    }

    enum CutsceneState
    {
        Idle,
        FirstCameraPan,
        KittyOyenWalk1,
    }

    CameraState camState = CameraState.Idle;
    CutsceneState cutState = CutsceneState.Idle;

    private void Awake()
    {
        mainCam = Camera.main;
        camFollow = mainCam.GetComponent<CameraFollow>();
        triggerCollider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        triggerCollider.enabled = false;
        camFollow.enabled = false;
        cutState = CutsceneState.FirstCameraPan;
        camState = CameraState.Moving;

        player = PlayerManager.instance.player.GetComponent<PlayerMovement>();
        player.GetComponent<Rigidbody>().isKinematic = true;
        player.SetCanMove(false);
    }

    private void Update()
    {
        switch (cutState)
        {
            case CutsceneState.FirstCameraPan:
                FirstCameraPanState();
                break;
            case CutsceneState.KittyOyenWalk1:
                KittyOyenWalk1State(); 
                break;
        }
    }

    private void FirstCameraPanState()
    {
        if (mainCam.transform.position != cameraWaypoints[cameraStep].position)
        {
            mainCam.transform.position =
                Vector3.MoveTowards(mainCam.transform.position, cameraWaypoints[cameraStep].position, 2f * Time.deltaTime);
        }
        else
        {
            camState = CameraState.Idle;
            cutState = CutsceneState.KittyOyenWalk1;

            player.transform.position = kittyWaypoints[0].position;
            player.SetDirection((kittyWaypoints[1].position - player.transform.position).normalized);

            oyen.transform.position = oyenWaypoints[0].position;
            oyen.targetDirection = (oyenWaypoints[0].position - oyen.transform.position).normalized;

            cameraStep++;
        }
    }

    private void KittyOyenWalk1State()
    {
        if (kittyStep < 2 && player.transform.position != kittyWaypoints[kittyStep].position)
        {
            player.transform.position =
                Vector3.MoveTowards(player.transform.position, kittyWaypoints[kittyStep].position, 2f * Time.deltaTime);
        }
        else
        {
            if (kittyStep == 1)
            {
                player.SetDirection(kittyWaypoints[kittyStep].forward);
            }
            else if (kittyStep + 1 < kittyWaypoints.Length)
            {
                player.SetDirection((kittyWaypoints[kittyStep + 1].position - kittyWaypoints[kittyStep].position).normalized);
            }

            kittyStep++;
        }

        // Oyen Movement
        if (oyenStep < 3 && oyen.transform.position != oyenWaypoints[oyenStep].position)
        {
            oyen.transform.position =
                Vector3.MoveTowards(oyen.transform.position, oyenWaypoints[oyenStep].position, 1f * Time.deltaTime);
        }
        else
        {
            if (oyenStep == 2)
            {
                oyen.targetDirection = oyenWaypoints[oyenStep].forward;
            }
            else if (oyenStep + 1 < oyenWaypoints.Length)
            {
                oyen.targetDirection = (oyenWaypoints[oyenStep + 1].position - oyenWaypoints[oyenStep].position).normalized;
            }

            oyenStep++;
        }

        if (oyenStep == 3 && kittyStep == 2)
        {
            Debug.Log("OYEN STEP AND KITTY STEP IS 1");
            cutState = CutsceneState.Idle;
        }
    }
}
