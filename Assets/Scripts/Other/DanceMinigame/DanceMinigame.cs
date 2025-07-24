using JetBrains.Annotations;
using UnityEngine;

public class DanceMinigame : MonoBehaviour
{
    [SerializeField] Interactable interactable;
    [SerializeField] DialogueOnInteract assyla;
    [SerializeField] GameObject panelObject;
    [SerializeField] GameObject canvasObject;
    [SerializeField] DanceKey[] keyPool;
    [SerializeField] GameObject[] lives;
    [SerializeField] GameObject hatObject;

    public float keyTimer = 2f;
    public int losses = 0;
    public int keysLeft = 25;
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
        if ((sceneID == "AssylaDance" && currentKey == null) ||
            (sceneID == "AssylaRestart" && currentKey == null))
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
        QuestManager.instance.SetUIEnabled(false);

        losses = 0;
        keysLeft = 25;
        activeKeys = 0;

        for (int i = 0; i < keyPool.Length; i++)
        {
            keyPool[i].OnReset();
            keyPool[i].gameObject.SetActive(false);
        }

        for (int i = 0; i < lives.Length; i++)
        {
            if (i < lives.Length - losses)
            {
                lives[i].SetActive(true);
            }
            else
            {
                lives[i].SetActive(false);
            }
        }
    }

    private void EndMinigame(bool win)
    {
        panelObject.SetActive(false);
        interactable.enabled = true;
        gameActive = false;
        PlayerManager.instance.player.GetComponent<PlayerMovement>().enabled = true;
        QuestManager.instance.SetUIEnabled(true);

        SimpleDialogManager dm = SimpleDialogManager.Instance;

        if (win)
        {
            dm.StartDialogue("AssylaGive");
            assyla.sceneId.value = "AssylaThank";
            hatObject.SetActive(false);
        }
        else
        {
            dm.StartDialogue("AssylaBetterLuck");
            assyla.sceneId.value = "AssylaRestart";
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

        DanceKey latestKey = null;
        if (pressedKey != KeyCode.None)
        {
            float keyScale = 9999999f;
            bool success = false;
            for (int i = 0; i < keyPool.Length; i++)
            {
                if (keyPool[i].gameObject.activeInHierarchy == true &&
                    keyPool[i].GetScale() < keyScale)
                {
                    latestKey = keyPool[i];
                    keyScale = keyPool[i].GetScale();
                }

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
                if (latestKey)
                {
                    latestKey.gameObject.SetActive(false);
                }
                FailKey();
            }
        }
    }

    private void FailKey()
    {
        losses++;
        
        for (int i = 0; i < lives.Length; i++)
        {
            if (i < lives.Length - losses)
            {
                lives[i].SetActive(true);
            }
            else
            {
                lives[i].SetActive(false);
            }
        }

        if (losses >= lives.Length)
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
                float randX = 0;
                float randY = 0;

                for (int x = 0; x < 5; x++)
                {
                    bool validPos = true;
                    randX = Random.Range(-halfScreenX, halfScreenX);
                    randY = Random.Range(-halfScreenY, halfScreenY);

                    for (int key = 0; key < keyPool.Length; key++)
                    {
                        if (keyPool[i] == keyPool[key] ||
                            keyPool[i].gameObject.activeInHierarchy == false)
                        {
                            continue;
                        }

                        if ((keyPool[i].transform.position - new Vector3(randX, randY, 0)).sqrMagnitude <
                            ((halfScreenY * 0.2f) * (halfScreenY * 0.2f)))
                        {
                            validPos = false;
                        }
                    }

                    if (validPos)
                    {
                        break;
                    }
                }

                keyPool[i].transform.position = canvasObject.transform.position + new Vector3(randX, randY, 0);
                activeKeys++;
                keysLeft -= 1;
                break;
            }
        }

        keyTimer = 0.7f + 1f * Mathf.Min(Mathf.Max(keysLeft - 10, 0) / 20f, 1);
    }
}
