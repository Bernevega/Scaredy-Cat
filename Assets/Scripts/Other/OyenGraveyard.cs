using UnityEngine;

public class OyenGraveyard : MonoBehaviour
{
    [SerializeField] GameObject firstDialogActor;
    [SerializeField] AutoRotate rotator;

    [SerializeField] GameObject momCat;
    [SerializeField] AutoRotate momCatRotator;
    [SerializeField] Animator oyenAnimator;

    [SerializeField] Gravestone oyenGravestone;

    [SerializeField] Transform[] waypoints;
    [SerializeField] GraveyardCutscene graveyardCutscene;
    int step = 0;
    float moveSpeed = 0.85f;

    private enum State
    {
        GraveyardWait,
        GraveyardWalk,
        GraveyardWalkWait,
        MomCatWalk,
        MomCatWalkWait,
    }

    State state = State.GraveyardWait;

    private void Start()
    {
        SimpleDialogManager dm = SimpleDialogManager.Instance;
        if (!dm)
        {
            throw new System.Exception("dialogue manager is null!");
        }    

        dm.eventDialogueChanged += OnDialogueChanged;
    }

    private void Update()
    {
        switch(state)
        {
            case State.GraveyardWalk:
                StateGraveyardWalk();
                break;
            case State.MomCatWalk:
                StateMomCatWalk();
                break;

        }
    }

    private void OnDialogueChanged(string sceneID, string key)
    {
        switch (sceneID)
        {
            case "OyenStart":
                OyenStartDialogue(key);
                break;
            case "OyenOJGrave":
                OyenOJGraveDialogue(key);
                break;
            case "MomCall":
                MomCallDialogue(key);
                break;

        }
    }

    private void StateGraveyardWalk()
    {
        if (transform.position != waypoints[step].position)
        {
            transform.position = Vector3.MoveTowards(transform.position, waypoints[step].position, moveSpeed * Time.deltaTime);
            oyenAnimator.SetBool("Walking", true);
        }
        else
        {
            if (step == 2)
            {
                state = State.GraveyardWalkWait;
                rotator.targetDirection = new Vector3(0, 0, -1);
                oyenAnimator.SetBool("Walking", false);
                step++;
            }
            else
            {
                if (step + 1 < waypoints.Length)
                {
                    Vector3 targetDirection = (waypoints[step + 1].position - waypoints[step].position).normalized;
                    rotator.targetDirection = targetDirection;
                    
                }
                step++;
            }
        }
    }

    private void StateMomCatWalk()
    {
        if (momCat.transform.position != waypoints[step].transform.position)
        {
            momCat.transform.position = Vector3.MoveTowards(momCat.transform.position, waypoints[step].position, moveSpeed * Time.deltaTime);
        }
        else
        {
            if (step + 1 < waypoints.Length)
            {
                Vector3 dirToNext = (waypoints[step + 1].position - momCat.transform.position).normalized;
                momCatRotator.targetDirection = dirToNext;
            }
            if (step == 3)
            {
                SimpleDialogManager.Instance.StartDialogue("MomCall");
            }
            else if (step == 4)
            {
                state = State.MomCatWalkWait;
                momCatRotator.targetDirection = waypoints[^1].transform.forward;
            }
            step++;
        }
    }

    private void OyenStartDialogue(string key)
    {
        if (key == null)
        {
            firstDialogActor.SetActive(false);
            state = State.GraveyardWalk;
            rotator.enabled = true;
        }
    }

    private void OyenOJGraveDialogue(string key)
    {
        if (key == null)
        {
            momCat.SetActive(true);
            state = State.MomCatWalk;
            PlayerManager pm = PlayerManager.instance;
            PlayerMovement pmv = pm.player.GetComponent<PlayerMovement>();
            pmv.SetDirection(new Vector3(1, 0, 0));
            pmv.speed = 0;

            oyenGravestone.SetSceneID("OyenOJGrave2");
            if (step + 1 < waypoints.Length)
            {
                Vector3 dirToNext = (waypoints[step + 1].position - momCat.transform.position).normalized;
                momCatRotator.targetDirection = dirToNext;
            }
        }
    }

    private void MomCallDialogue(string key)
    {
        if (key == null)
        {
            graveyardCutscene.StartCutscene();
        }
    }
}
