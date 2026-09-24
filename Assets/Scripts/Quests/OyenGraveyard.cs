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
    float moveSpeed = 1f;

    // If the grave dialogue finishes before Oyen reaches the grave,
    // remember it and continue only after Oyen finishes walking.
    private bool pendingMomCatSequence = false;
    private bool oyenReachedGrave = false;

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
            throw new System.Exception(
                "dialogue manager is null!"
            );
        }

        dm.eventDialogueChanged += OnDialogueChanged;
    }

    private void OnDestroy()
    {
        if (SimpleDialogManager.Instance != null)
        {
            SimpleDialogManager.Instance.eventDialogueChanged -=
                OnDialogueChanged;
        }
    }

    private void Update()
    {
        switch (state)
        {
            case State.GraveyardWalk:
                StateGraveyardWalk();
                break;

            case State.MomCatWalk:
                StateMomCatWalk();
                break;
        }
    }

    private void OnDialogueChanged(
        string sceneID,
        string key
    )
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
        // Safety check
        if (step < 0 || step >= waypoints.Length)
        {
            oyenAnimator.SetBool("Walking", false);
            return;
        }

        if (
            transform.position !=
            waypoints[step].position
        )
        {
            // Oyen ALWAYS continues walking until
            // the current waypoint has actually been reached.
            transform.position =
                Vector3.MoveTowards(
                    transform.position,
                    waypoints[step].position,
                    moveSpeed * Time.deltaTime
                );

            oyenAnimator.SetBool(
                "Walking",
                true
            );
        }
        else
        {
            // Snap exactly to the waypoint.
            transform.position =
                waypoints[step].position;

            // This is Oyen's final grave waypoint.
            if (step == 2)
            {
                rotator.targetDirection =
                    new Vector3(
                        0f,
                        0f,
                        -1f
                    );

                // Oyen has completely finished walking.
                oyenAnimator.SetBool(
                    "Walking",
                    false
                );

                oyenReachedGrave = true;

                state =
                    State.GraveyardWalkWait;

                step++;

                // If the player already finished the grave
                // dialogue early, continue immediately now
                // that Oyen has reached the grave.
                if (pendingMomCatSequence)
                {
                    StartMomCatSequence();
                }
                else
                {
                    // Otherwise reveal the gravestone normally.
                    if (oyenGravestone != null)
                    {
                        oyenGravestone.gameObject.SetActive(
                            true
                        );
                    }
                }

                return;
            }

            // Rotate toward next waypoint.
            if (step + 1 < waypoints.Length)
            {
                Vector3 targetDirection =
                    (
                        waypoints[step + 1].position -
                        waypoints[step].position
                    ).normalized;

                rotator.targetDirection =
                    targetDirection;
            }

            step++;
        }
    }

    private void StateMomCatWalk()
    {
        if (
            step < 0 ||
            step >= waypoints.Length
        )
        {
            return;
        }

        if (
            momCat.transform.position !=
            waypoints[step].position
        )
        {
            momCat.transform.position =
                Vector3.MoveTowards(
                    momCat.transform.position,
                    waypoints[step].position,
                    moveSpeed * Time.deltaTime
                );
        }
        else
        {
            // Snap exactly to waypoint.
            momCat.transform.position =
                waypoints[step].position;

            if (step + 1 < waypoints.Length)
            {
                Vector3 dirToNext =
                    (
                        waypoints[step + 1].position -
                        momCat.transform.position
                    ).normalized;

                momCatRotator.targetDirection =
                    dirToNext;
            }

            if (step == 3)
            {
                SimpleDialogManager.Instance
                    .StartDialogue(
                        "MomCall"
                    );
            }
            else if (step == 4)
            {
                state =
                    State.MomCatWalkWait;

                momCatRotator.targetDirection =
                    waypoints[^1].forward;
            }

            step++;
        }
    }

    private void OyenStartDialogue(
        string key
    )
    {
        if (key == null)
        {
            if (firstDialogActor != null)
            {
                firstDialogActor.SetActive(
                    false
                );
            }

            state =
                State.GraveyardWalk;

            if (rotator != null)
                rotator.enabled = true;

            // Make sure Oyen starts walking.
            if (oyenAnimator != null)
            {
                oyenAnimator.SetBool(
                    "Walking",
                    true
                );
            }
        }
    }

    private void OyenOJGraveDialogue(
        string key
    )
    {
        if (key != null)
            return;

        // The player has finished this dialogue.
        //
        // IMPORTANT:
        // Do NOT switch to MomCatWalk yet if Oyen
        // hasn't physically reached the grave.
        pendingMomCatSequence = true;

        if (oyenReachedGrave)
        {
            StartMomCatSequence();
        }
    }

    private void StartMomCatSequence()
    {
        pendingMomCatSequence = false;

        if (momCat != null)
            momCat.SetActive(true);

        state =
            State.MomCatWalk;

        PlayerManager pm =
            PlayerManager.instance;

        if (
            pm != null &&
            pm.player != null
        )
        {
            PlayerMovement pmv =
                pm.player.GetComponent<PlayerMovement>();

            if (pmv != null)
            {
                pmv.SetDirection(
                    new Vector3(
                        1f,
                        0f,
                        0f
                    )
                );

                pmv.speed = 0f;
            }
        }

        if (oyenGravestone != null)
        {
            oyenGravestone.gameObject.SetActive(
                false
            );

            oyenGravestone.SetSceneID(
                "OyenOJGrave2"
            );
        }

        if (
            step + 1 <
            waypoints.Length &&
            momCat != null
        )
        {
            Vector3 dirToNext =
                (
                    waypoints[step + 1].position -
                    momCat.transform.position
                ).normalized;

            momCatRotator.targetDirection =
                dirToNext;
        }
    }

    private void MomCallDialogue(
        string key
    )
    {
        if (key == null)
        {
            graveyardCutscene.StartCutscene();
        }
    }
}