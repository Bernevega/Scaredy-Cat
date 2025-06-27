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

    bool cutsceneActivated = false;

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
        KittyPlaceFlower,
        KittyPlaceFlower2,
        KittyOyenWalkOff,
    }

    CameraState camState = CameraState.Idle;
    CutsceneState cutState = CutsceneState.Idle;

    private void Awake()
    {
        mainCam = Camera.main;
        camFollow = mainCam.GetComponent<CameraFollow>();
        triggerCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        SimpleDialogManager.Instance.eventDialogueChanged += OnDialogueAdvance;
    }

    private void OnTriggerEnter(Collider other)
    {
        triggerCollider.enabled = false;
        camFollow.enabled = false;
        cutState = CutsceneState.FirstCameraPan;
        camState = CameraState.Moving;

        player = PlayerManager.instance.player.GetComponent<PlayerMovement>();
        player.GetComponent<Rigidbody>().isKinematic = true;
        player.speed = 0f;

        cutsceneActivated = true;
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
            case CutsceneState.KittyPlaceFlower:
                KittyPlaceFlowerState();
                break;
            case CutsceneState.KittyPlaceFlower2:
                KittyPlaceFlower2State();
                break;
            case CutsceneState.KittyOyenWalkOff:
                KittyOyenWalkOffState();
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

            
            player.GetComponent<Rigidbody>().MovePosition(kittyWaypoints[0].position);
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
            if (kittyStep == 0)
                player.transform.position = kittyWaypoints[kittyStep].position;
            else
            player.transform.position =
                Vector3.MoveTowards(player.transform.position, kittyWaypoints[kittyStep].position, 2f * Time.deltaTime);
        }
        else if (kittyStep < 2)
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
        else if (oyenStep < 3)
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
            cutState = CutsceneState.Idle;
            Invoke("KittyOyenWalkWait", 1f);
        }
    }

    private void KittyOyenWalkWait()
    {
        SimpleDialogManager.Instance.StartDialogue("GoodbyeDialogue");
        cutState = CutsceneState.Idle;
    }

    private void TransitionToKittyPlaceFlower()
    {
        cutState = CutsceneState.KittyPlaceFlower;
        if (kittyStep - 1 > 0)
        {
            player.SetDirection((kittyWaypoints[kittyStep].position - kittyWaypoints[kittyStep - 1].position).normalized);
        }
    }

    private void KittyPlaceFlowerState()
    {
        if (player.transform.position != kittyWaypoints[kittyStep].position)
        {
            player.transform.position =
                Vector3.MoveTowards(player.transform.position, kittyWaypoints[kittyStep].position, 2f * Time.deltaTime);
        }
        else
        {
            player.SetDirection(kittyWaypoints[kittyStep].forward);
            cutState = CutsceneState.Idle;
            Invoke("KittyFlowerPlace2StateTransition", 1f);

            kittyStep++;
        }
    }

    private void KittyFlowerPlace2StateTransition()
    {
        cutState = CutsceneState.KittyPlaceFlower2;
        if (kittyStep - 1 > 0)
        {
            player.SetDirection((kittyWaypoints[kittyStep].position - kittyWaypoints[kittyStep - 1].position).normalized);
        }
    }

    private void KittyPlaceFlower2State()
    {
        if (player.transform.position != kittyWaypoints[kittyStep].position)
        {
            player.transform.position =
                Vector3.MoveTowards(player.transform.position, kittyWaypoints[kittyStep].position, 2f * Time.deltaTime);
        }
        else
        {
            player.SetDirection(kittyWaypoints[kittyStep].forward);
            cutState = CutsceneState.Idle;
            Invoke("SayTitleDrop", 1f);

            kittyStep++;
        }
    }

    private void KittyOyenWalkOffState()
    {
        if (kittyStep < kittyWaypoints.Length && player.transform.position != kittyWaypoints[kittyStep].position)
        {
            player.transform.position =
                Vector3.MoveTowards(player.transform.position, kittyWaypoints[kittyStep].position, 2f * Time.deltaTime);
        }
        else if (kittyStep < kittyWaypoints.Length)
        {
            if (kittyStep + 1 < kittyWaypoints.Length)
                player.SetDirection((kittyWaypoints[kittyStep + 1].position - kittyWaypoints[kittyStep].position).normalized);
            
            kittyStep++;

            if (kittyStep == kittyWaypoints.Length)
                player.SetDirection(kittyWaypoints[^1].forward);
        }

        // Oyen Movement
        if (oyenStep < oyenWaypoints.Length && oyen.transform.position != oyenWaypoints[oyenStep].position)
        {
            oyen.transform.position =
                Vector3.MoveTowards(oyen.transform.position, oyenWaypoints[oyenStep].position, 1f * Time.deltaTime);
        }
        else if (oyenStep < oyenWaypoints.Length)
        {
            if (oyenStep + 1 < oyenWaypoints.Length)
            {
                oyen.targetDirection = (oyenWaypoints[oyenStep + 1].position - oyenWaypoints[oyenStep].position).normalized;
            }

            oyenStep++;

            if (oyenStep == oyenWaypoints.Length)
            {
                oyen.targetDirection = oyenWaypoints[^1].forward;
            }
        }
        
        if (kittyStep == kittyWaypoints.Length &&
            oyenStep == oyenWaypoints.Length)
        {
            cutState = CutsceneState.Idle;
        }
    }

    private void SayTitleDrop()
    {
        SimpleDialogManager.Instance.StartDialogue("FinalTitleDrop");
    }

    private void OnDialogueAdvance(string sceneID, string currentKey)
    {
        if (sceneID == "GoodbyeDialogue")
        {
            if (currentKey == null)
            {
                TransitionToKittyPlaceFlower();
            }
        }
        else if (sceneID == "FinalTitleDrop")
        {
            if (currentKey == null)
            {
                player.SetDirection((kittyWaypoints[kittyStep].position - kittyWaypoints[kittyStep].position).normalized);
                oyen.targetDirection = (oyenWaypoints[oyenStep].position - oyenWaypoints[oyenStep].position).normalized;
                cutState = CutsceneState.KittyOyenWalkOff;
            }
        }
    }
}
