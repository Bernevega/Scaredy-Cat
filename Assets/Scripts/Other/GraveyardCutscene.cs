
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
    [SerializeField] Transform flowerPos;
    [SerializeField] SceneTransitionZone cutsceneTransition;

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
        SecondCameraPan,
    }

    CameraState camState = CameraState.Idle;
    CutsceneState cutState = CutsceneState.Idle;

    private void Awake()
    {
        mainCam = Camera.main;
        triggerCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        SimpleDialogManager.Instance.eventDialogueChanged += OnDialogueAdvance;
    }

    private void OnTriggerEnter(Collider other)
    {
        StartCutscene();
    }

    public void StartCutscene()
    {
        triggerCollider.enabled = false;
        mainCam.GetComponent<CameraController>().SetCamBehaviour(null);
        cutState = CutsceneState.FirstCameraPan;
        camState = CameraState.Moving;

        player = PlayerManager.instance.player.GetComponent<PlayerMovement>();
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        playerRb.linearVelocity = new Vector3(0, playerRb.linearVelocity.y, 0);

        player.speed = 0f;

        cutsceneActivated = true;
        cutsceneTransition.gameObject.SetActive(true);
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
            case CutsceneState.SecondCameraPan:
                SecondCameraPanState();
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

            Rigidbody playerRb = player.GetComponent<Rigidbody>();
            playerRb.isKinematic = true;

            if (kittyStep + 1 < kittyWaypoints.Length)
            {
                player.SetDirection((kittyWaypoints[kittyStep + 1].position - player.transform.position).normalized);
            }
            if (oyenStep + 1 < oyenWaypoints.Length)
            {
                oyen.targetDirection = (oyenWaypoints[oyenStep + 1].position - oyen.transform.position).normalized;
            }

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

            GameObject flowerObject = FindFlowerRecursive(player.transform, "FlowerOnHead");
            if (flowerObject != null)
            {
                flowerObject.transform.parent = null;
                flowerObject.transform.position = flowerPos.transform.position;
            }
        }
    }

    private GameObject FindFlowerRecursive(Transform obj, string childName)
    {
        foreach (Transform child in obj)
        {
            if (child.name == childName)
            {
                return child.gameObject;
            }

            GameObject result = FindFlowerRecursive(child, childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
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
            cutState = CutsceneState.SecondCameraPan;
        }
    }

    private void SecondCameraPanState()
    {
        bool positionReached = false;
        bool rotationReached = false;
        if (mainCam.transform.position != cameraWaypoints[cameraStep].position)
        {
            mainCam.transform.position =
                Vector3.MoveTowards(mainCam.transform.position, cameraWaypoints[cameraStep].position, 0.2f * Time.deltaTime);
        }
        else
        {
            positionReached = true;
        }

        if (mainCam.transform.rotation != cameraWaypoints[cameraStep].rotation)
        {
            mainCam.transform.rotation =
                Quaternion.Lerp(
                    cameraWaypoints[cameraStep - 1].rotation,
                    cameraWaypoints[cameraStep].rotation,
                    1 - ((cameraWaypoints[cameraStep].position - mainCam.transform.position).sqrMagnitude /
                    (cameraWaypoints[cameraStep].position - cameraWaypoints[cameraStep - 1].position).sqrMagnitude)
                );
        }
        else
        {
            rotationReached = true;
        }

        if (rotationReached &&
            positionReached)
        {
            cutState |= CutsceneState.Idle;
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
                player.SetDirection((kittyWaypoints[kittyStep].position - player.transform.position).normalized);
                oyen.targetDirection = (oyenWaypoints[oyenStep].position - oyen.transform.position).normalized;
                cutState = CutsceneState.KittyOyenWalkOff;
            }
        }
    }

    private float Waypoint_GetSqrMagnitude(Vector3 origin, Vector3 target)
    {
        return (target - origin).sqrMagnitude;
    }

    private Vector3 Waypoint_GetDirectionTo(Vector3 origin, Vector3 target)
    {
        return (origin - target).normalized;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        for (int i = 0; i < kittyWaypoints.Length; i++)
        {
            Gizmos.DrawSphere(kittyWaypoints[i].position, 0.075f);
            Gizmos.DrawLine(kittyWaypoints[i].position, kittyWaypoints[i].position + kittyWaypoints[i].forward);
        }

        Gizmos.color = Color.red;

        for (int i = 0; i < oyenWaypoints.Length; i++)
        {
            Gizmos.DrawSphere(oyenWaypoints[i].position, 0.075f);
            Gizmos.DrawLine(oyenWaypoints[i].position, oyenWaypoints[i].position + oyenWaypoints[i].forward);
        }

        Gizmos.color = Color.cyan;

        for (int i = 0; i < cameraWaypoints.Length; i++)
        {
            Gizmos.DrawSphere(cameraWaypoints[i].position, 0.075f);
            Gizmos.DrawLine(cameraWaypoints[i].position, cameraWaypoints[i].position + cameraWaypoints[i].forward);
        }
    }
}
