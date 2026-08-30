using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ProgressCheck : MonoBehaviour
{
    [Tooltip("Drag in your friend NPCs.")]
    public DialogActor[] friends;

    [Tooltip("The sceneID of the 'NotYet' dialogue.")]
    public string notYetSceneID = "NotYet";

    private Collider _col;

    private void Awake()
    {
        _col = GetComponent<Collider>();

        // A solid collider prevents the player from passing through.
        _col.isTrigger = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        foreach (DialogActor friend in friends)
        {
            if (friend == null || !friend.HasInteracted)
            {
                PlayerMovement playerMovement =
                    collision.gameObject.GetComponent<PlayerMovement>();

                if (playerMovement != null)
                    playerMovement.blockRightMovement = true;

                SimpleDialogManager.Instance.StartDialogue(notYetSceneID);
                return;
            }
        }

        // Everyone has been spoken to, so remove the barrier.
        _col.enabled = false;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        PlayerMovement playerMovement =
            collision.gameObject.GetComponent<PlayerMovement>();

        if (playerMovement != null)
            playerMovement.blockRightMovement = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        PlayerMovement playerMovement =
            collision.gameObject.GetComponent<PlayerMovement>();

        if (playerMovement != null)
            playerMovement.blockRightMovement = false;
    }
}