using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class WaterRespawn : MonoBehaviour
{
    [Tooltip("Drag your spawn-point Transform here, or leave empty to auto-find a GameObject tagged 'SpawnPoint'")]
    public Transform spawnPoint;

    [Tooltip("Reference to your ScreenFader (the full-screen black Image)")]
    public ScreenFader screenFader;

    private bool _isRespawning = false;

    private void Reset()
    {
        // ensure the water collider is set as a trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Start()
    {
        if (spawnPoint == null)
        {
            var sp = GameObject.FindWithTag("SpawnPoint");
            if (sp != null) spawnPoint = sp.transform;
            else Debug.LogError("[WaterRespawn] No spawnPoint set and no 'SpawnPoint' tag found.");
        }

        if (screenFader == null)
            Debug.LogError("[WaterRespawn] No ScreenFader reference set.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isRespawning) 
            return;

        // Find the player's movement component (could be on a parent)
        var movement = other.GetComponentInParent<PlayerMovement>();
        if (movement == null) 
            return;

        var rb = movement.GetComponent<Rigidbody>();
        if (rb == null) 
            return;

        _isRespawning = true;

        // 1) Disable player input immediately
        movement.enabled = false;

        // 2) Start the respawn sequence (physics disabling is delayed)
        StartCoroutine(HandleRespawn(movement.transform, rb, movement));
    }

    private IEnumerator HandleRespawn(Transform player, Rigidbody rb, PlayerMovement movement)
    {
        // 1) Wait 0.2 seconds before disabling physics
        yield return new WaitForSeconds(0.2f);

        // 2) Freeze physics & zero out any current velocity
        rb.isKinematic     = true;
        rb.linearVelocity        = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 3) Fade to black
        yield return StartCoroutine(screenFader.FadeOut());

        // 4) Teleport & orient
        player.position = spawnPoint.position;
        player.rotation = spawnPoint.rotation;

        // 5) Wait one physics step so teleport “sticks”
        yield return new WaitForFixedUpdate();

        // 6) Reset any leftover velocity & unfreeze physics
        rb.linearVelocity      = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic   = false;

        // 7) One-frame buffer
        yield return null;

        // 8) Fade back in
        yield return StartCoroutine(screenFader.FadeIn());

        // 9) Re-enable player movement & clear flag
        movement.enabled   = true;
        _isRespawning      = false;
    }
}
