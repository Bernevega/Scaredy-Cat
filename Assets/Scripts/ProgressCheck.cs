using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ProgressCheck : MonoBehaviour
{
    [Tooltip("Drag in your friend NPCs (each must have a DialogActor)")]
    public DialogActor[] friends;

    [Tooltip("The sceneID of the 'NotYet' dialog in your JSON")]
    public string notYetSceneID = "NotYet";

    private Collider _col;

    void Awake()
    {
        _col = GetComponent<Collider>();
        _col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // If any friend hasn’t been spoken to yet...
        foreach (var friend in friends)
        {
            if (friend == null || !friend.HasInteracted)
            {
                // Block rightward movement
                var pm = other.GetComponent<PlayerMovement>();
                if (pm != null)
                    pm.blockRightMovement = true;

                // Play the "NotYet" dialog
                SimpleDialogManager.Instance.StartDialogue(notYetSceneID);
                return;
            }
        }

        // All friends done: disable this trigger so the player can pass normally
        _col.enabled = false;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // Leaving the check zone: re-enable rightward movement
        var pm = other.GetComponent<PlayerMovement>();
        if (pm != null)
            pm.blockRightMovement = false;
    }
}
