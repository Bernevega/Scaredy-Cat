using JetBrains.Annotations;
using UnityEngine;

public class DanceMinigame : MonoBehaviour
{
    [SerializeField] Interactable interactable;
    [SerializeField] GameObject panelObject;
    [SerializeField] GameObject canvasObject;
    [SerializeField] DanceKey[] keyPool;

    public float keyTimer = 2f;
    public int losses = 0;
    public int keysLeft = 20;
    public int activeKeys = 0;

    bool gameActive = false;

    private void Start()
    {
        SimpleDialogManager dm = SimpleDialogManager.Instance;
        if (dm)
        {
            dm.eventDialogueChanged += OnDialogueAdvance;
        }
    }

    private void OnDestroy()
    {
        SimpleDialogManager dm = SimpleDialogManager.Instance;
        if (dm)
        {
            dm.eventDialogueChanged -= OnDialogueAdvance;
        }
    }

    private void OnDialogueAdvance(string sceneID, string currentKey)
    {
        if (sceneID == "AssylaStart" &&
            currentKey == null)
        {
            StartMinigame();
        }
    }

    private void StartMinigame()
    {
        panelObject.SetActive(true);
        interactable.enabled = false;
        gameActive = true;
        PlayerManager.instance.player.GetComponent<PlayerMovement>().enabled = false;

        losses = 0;
        keysLeft = 20;
        activeKeys = 0;
    }

    private void EndMinigame(bool win)
    {
        panelObject.SetActive(false);
        interactable.enabled = true;
        gameActive = false;
        PlayerManager.instance.player.GetComponent<PlayerMovement>().enabled = true;

        if (win)
        {
            Debug.Log("You win the challenge!");
        }
        else
        {
            Debug.Log("You lose the challenge!");
        }
    }

    private void FixedUpdate()
    {
        if (!gameActive)
        {
            return;
        }

        if (keyTimer > 0)
        {
            keyTimer -= Time.deltaTime;
            if (keyTimer <= 0)
            {
                SpawnKey();
            }
        }
        else
        {
            SpawnKey();
        }

        for (int i = 0; i < keyPool.Length; i++)
        {
            if (keyPool[i].gameObject.activeInHierarchy)
            {
                keyPool[i].OnUpdate(Time.deltaTime);

                if (keyPool[i].InFailZone())
                {
                    keyPool[i].gameObject.SetActive(false);
                    FailKey();
                    activeKeys--;
                }
            }
        }
    }

    private void Update()
    {
        if (!gameActive)
        {
            return;
        }

        KeyCode pressedKey = KeyCode.None;
        for (int i = 0; i < DanceKey.randomKeyList.Length; i++)
        {
            if (Input.GetKeyDown(DanceKey.randomKeyList[i]))
            {
                pressedKey = DanceKey.randomKeyList[i];
                break;
            }
        }
        if (pressedKey != KeyCode.None)
        {
            bool success = false;
            for (int i = 0; i < keyPool.Length; i++)
            {
                if (keyPool[i].gameObject.activeInHierarchy == false ||
                    keyPool[i].reqKey != pressedKey)
                {
                    continue;
                }

                if (keyPool[i].InSuccessZone())
                {
                    keyPool[i].gameObject.SetActive(false);
                    success = true;
                    break;
                }
            }

            if (success)
            {
                WinKey();
            }
            else
            {
                FailKey();
            }
        }
    }

    private void FailKey()
    {
        losses++;
        

        if (losses >= 5)
        {
            EndMinigame(false);
        }
        else if (activeKeys == 0 && keysLeft == 0)
        {
            EndMinigame(true);
        }
    }

    public void WinKey()
    {
        activeKeys--;

        if (activeKeys == 0 && keysLeft == 0)
        {
            EndMinigame(true);
        }
    }

    private void SpawnKey()
    {
        if (keysLeft <= 0)
        {
            return;
        }

        for (int i = 0; i < keyPool.Length; i++)
        {
            if (!keyPool[i].gameObject.activeInHierarchy)
            {
                keyPool[i].gameObject.SetActive(true);
                keyPool[i].OnReset();
                float halfScreenX = Screen.width * 0.4f;
                float halfScreenY = Screen.height * 0.35f;
                float randX = Random.Range(-halfScreenX, halfScreenX);
                float randY = Random.Range(-halfScreenY, halfScreenY);

                keyPool[i].transform.position = canvasObject.transform.position + new Vector3(randX, randY, 0);
                activeKeys++;
                keysLeft -= 1;
                break;
            }
        }

        keyTimer = 0.5f + 1f * Mathf.Min(keysLeft / 10f, 1);
    }
}
