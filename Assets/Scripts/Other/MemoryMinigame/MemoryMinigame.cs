using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class MemoryMinigame : MonoBehaviour
{
    [SerializeField] DialogueOnInteract nerd;
    [SerializeField] GameObject bowtieObject;

    [SerializeField] Transform[] circlesTransform;
    [SerializeField] SpriteRenderer circleIndicator;
    [SerializeField] SpriteRenderer correctIndicator;
    [SerializeField] Sprite correctSprite;
    [SerializeField] Sprite wrongSprite;
    Transform playerTransform;
    Transform steppedCircle;

    Color[] circleColors =
    {
        Color.red,
        Color.green, 
        Color.blue,
        Color.yellow,
    };
    [SerializeField] int[] chosenColors;

    [SerializeField] float circleRadius;

    [SerializeField] bool gameStarted;

    public int numColors;
    public int numColorsLeft;

    public float interval = 1f;

    private enum GameState
    {
        Idle,
        ShowingColors,
        SelectingColors,
    }

    GameState gameState = GameState.Idle;

    private void Awake()
    {
        chosenColors = new int[numColors + 5];
    }

    private void Start()
    {
        SimpleDialogManager.Instance.eventDialogueChanged += OnDialogueAdvance;
    }

    private void OnDestroy()
    {
        SimpleDialogManager.Instance.eventDialogueChanged -= OnDialogueAdvance;
    }

    private void OnDialogueAdvance(string sceneId, string currKey)
    {
        if (gameStarted)
        {
            return;
        }

        if ((sceneId == "NerdStart" && currKey == null) ||
            (sceneId == "NerdRestart" && currKey == null))
        {
            StartGame();
        }
    }
    private void StartGame()
    {
        gameStarted = true;
        numColors = 4;
        numColorsLeft = numColors;
        interval = 1f;
        gameState = GameState.Idle;

        GameObject playerObj = PlayerManager.PM_GetPlayer();
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
       
    }

    private void EndGame(bool win)
    {
        gameStarted = false;
        playerTransform = null;
        circleIndicator.gameObject.SetActive(false);
        correctIndicator.gameObject.SetActive(false);
        gameState = GameState.Idle;

        if (win)
        {
            StartCoroutine(StartDialogueCoroutine(0.1f, "NerdWin"));
            bowtieObject.SetActive(false);
            nerd.sceneId = "NerdThank";
        }
        else
        {
            StartCoroutine(StartDialogueCoroutine(0.1f, "NerdOver"));
            nerd.sceneId = "NerdRestart";
        }
    }

    private void FixedUpdate()
    {
        if (!gameStarted)
        {
            return;
        }

        switch (gameState)
        {
            case GameState.Idle:
                GameStateIdle();
                break;
            case GameState.ShowingColors:
                GameStateShowing();
                break;
            case GameState.SelectingColors:
                GameStateSelecting();
                break;
        }
    }

    private void GameStateSelecting()
    {
        if (steppedCircle == null)
        {
            float distToPlayer = 0;
            for (int i = 0; i < circlesTransform.Length; i++)
            {
                distToPlayer = (playerTransform.position - circlesTransform[i].position).sqrMagnitude;
                if (distToPlayer < circleRadius * circleRadius)
                {
                    steppedCircle = circlesTransform[i];
                    OnCircleStep(circlesTransform[i]);
                    break;
                }
            }
        }
        else
        {
            float distToPlayer = (playerTransform.position - steppedCircle.position).sqrMagnitude;
            if (distToPlayer > circleRadius * circleRadius)
            {
                steppedCircle = null;
            }
        }
    }

    private void GameStateShowing()
    {
        interval -= Time.fixedDeltaTime;
        if (interval <= 0)
        {
            gameState = GameState.Idle;
            circleIndicator.gameObject.SetActive(false);

            interval = .25f;
            
        }
        
    }

    private void GameStateIdle()
    {
        interval -= Time.fixedDeltaTime;
        if (interval <= 0 && numColorsLeft <= 0)
        {
            gameState = GameState.SelectingColors;
            circleIndicator.gameObject.SetActive(false);
            numColorsLeft = numColors;
        }
        else if (interval <= 0)
        {
            gameState = GameState.ShowingColors;

            int randCol = Random.Range(0, circleColors.Length);
            chosenColors[numColors - numColorsLeft] = randCol;
            circleIndicator.color = circleColors[randCol];
            circleIndicator.gameObject.SetActive(true);
            numColorsLeft -= 1;
            interval = 1f;
        }
        
    }

    private void OnCircleStep(Transform circleTransform)
    {
        int chosenColIndex = 0;
        bool correctColor = false;
        for (int i = 0; i < circlesTransform.Length; i++)
        {
            if (circlesTransform[i] == circleTransform &&
                i == chosenColors[numColors - numColorsLeft])
            {
                correctColor = true;
                chosenColIndex = chosenColors[i];
                break;
            }
        }

        numColorsLeft -= 1;

        if (correctColor)
        {
            circleIndicator.color = circleColors[chosenColIndex];
            circleIndicator.gameObject.SetActive(true);
            correctIndicator.sprite = correctSprite;
            correctIndicator.gameObject.SetActive(true);

            if (numColorsLeft <= 0)
            {
                EndGame(true);
            }
        }
        else
        {
            circleIndicator.color = circleColors[chosenColIndex];
            circleIndicator.gameObject.SetActive(true);
            correctIndicator.sprite = wrongSprite;
            correctIndicator.gameObject.SetActive(true);
            
            EndGame(false);
        }

        
    }

    IEnumerator StartDialogueCoroutine(float time, string sceneId)
    {
        yield return new WaitForSeconds(time);
        SimpleDialogManager.Instance.StartDialogue(sceneId);
    }
}
