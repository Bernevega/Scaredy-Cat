using System;
using UnityEngine;

public class RotateToPlayerOnDialogue : MonoBehaviour
{
    public SharedString sceneId;

    [Space(10)]
    [SerializeField] AutoRotate rotator;
    Vector3 originalDirection;

    [SerializeField] bool inDialogue = false;

    private void Start()
    {
        SimpleDialogManager.Instance.eventDialogueChanged += OnDialogueAdvance;
        rotator.targetDirection = transform.forward;
    }

    private void OnDestroy()
    {
        SimpleDialogManager.Instance.eventDialogueChanged -= OnDialogueAdvance;
    }

    private void OnDialogueAdvance(string sceneId, string currentKey)
    {
        GameObject player = PlayerManager.PM_GetPlayer();
        if (player == null)
        {
            Debug.Log("THE PLAYER IS NULL");
        }
        if (player != null &&
            sceneId == this.sceneId.value)
        {
            if (inDialogue &&
                currentKey == null)
            {
                rotator.targetDirection = originalDirection;
                inDialogue = false;
            }
            else if (!inDialogue)
            {
                inDialogue = true;

                Vector3 playerPos = player.transform.position;
                playerPos.y = transform.position.y;
                Vector3 dirToPlayer = (playerPos - transform.position);
                dirToPlayer.Normalize();
                rotator.targetDirection = dirToPlayer;

                originalDirection = transform.forward;
                Debug.Log("Rotating");
            }
        }
    }
}

[Serializable]
public class SharedString
{
    public string value;
}