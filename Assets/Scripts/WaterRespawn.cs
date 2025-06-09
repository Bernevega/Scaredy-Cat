using UnityEngine;
using System.Collections;

public class WaterRespawn : MonoBehaviour
{
    [Tooltip("Drag your spawn-point Transform here, or leave empty to auto-find a GameObject tagged 'SpawnPoint'")]
    public Transform spawnPoint;

    [Tooltip("Reference to your ScreenFader (the full-screen black Image)")]
    public ScreenFader screenFader;

    private void Start()
    {
        if (spawnPoint == null)
        {
            var sp = GameObject.FindGameObjectWithTag("SpawnPoint");
            if (sp != null)
                spawnPoint = sp.transform;
            else
                Debug.LogError("[WaterRespawn] No spawnPoint set and no 'SpawnPoint' tag found.");
        }

        if (screenFader == null)
            Debug.LogError("[WaterRespawn] No ScreenFader reference set.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        StartCoroutine(HandleRespawn(other.transform, other.GetComponent<Rigidbody>()));
    }

    private IEnumerator HandleRespawn(Transform player, Rigidbody rb)
    {
        // 1) Fade to black
        yield return StartCoroutine(screenFader.FadeOut());

        // 2) Teleport & reset velocity
        if (rb != null)
            rb.linearVelocity = Vector3.zero;

        player.position = spawnPoint.position;
        player.rotation = spawnPoint.rotation;

        // (Optional) yield one frame so position sticks before fade in:
        yield return null;

        // 3) Fade back in
        yield return StartCoroutine(screenFader.FadeIn());
    }
}
