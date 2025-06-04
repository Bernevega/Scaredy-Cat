using UnityEngine;

/// <summary>
/// Attach this to any NPC. When the Player (tagged “Player”) comes within 'triggerRadius',
/// the dialog starts once and never repeats.
/// </summary>
public class DialogActor : MonoBehaviour
{
    [Tooltip("Must match a top-level key in your JSON, e.g. \"HomeScene\", \"FriendsScene\"")]
    public string sceneID;

    [Tooltip("How close the Player has to be to trigger this NPC’s dialog (in world units).")]
    public float triggerRadius = 3f;

    private bool hasInteracted = false;
    private Transform playerTransform;

    void Start()
    {
        // Find the Player by tag once, at startup
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            playerTransform = playerGO.transform;
        }
        else
        {
            Debug.LogWarning($"DialogActor ({name}) cannot find GameObject tagged 'Player'");
        }
    }

    void Update()
    {
        if (hasInteracted || playerTransform == null) return;

        // Check distance to player
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist <= triggerRadius)
        {
            hasInteracted = true;
            SimpleDialogManager.Instance.StartDialogue(sceneID);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize the triggerRadius in the Scene view
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
