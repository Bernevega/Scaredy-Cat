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
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Start()
    {
        if (spawnPoint == null)
        {
            var sp = GameObject.FindWithTag("Respawn");
            if (sp != null) spawnPoint = sp.transform;
            else Debug.LogError("[WaterRespawn] No spawnPoint set and no 'Respawn' tag found.");
        }

        if (screenFader == null)
            Debug.LogError("[WaterRespawn] No ScreenFader reference set.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isRespawning) return;

        var movement = other.GetComponentInParent<PlayerMovement>();
        if (movement == null) return;

        var rb = movement.GetComponent<Rigidbody>();
        if (rb == null) return;

        _isRespawning = true;
        movement.enabled = false;

        StartCoroutine(HandleRespawn(movement.transform, rb, movement));
    }

    private IEnumerator HandleRespawn(Transform player, Rigidbody rb, PlayerMovement movement)
    {
        yield return new WaitForSeconds(0.1f);

        // Freeze physics and disable gravity/movement
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // Fade to black
        yield return StartCoroutine(screenFader.FadeOut());

        // Move player to spawn point
        player.position = spawnPoint.position;
        player.rotation = spawnPoint.rotation;

        yield return new WaitForFixedUpdate();

        // ✅ Reappear all branches during the black screen
        foreach (var branch in CrakingBranch.AllBranches)
        {
            branch.ReappearNow();
        }

        // Reset animation state
        var animator = movement.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.SetBool("moveInput", false);
            animator.SetBool("isRunning", false);
            animator.SetBool("grounded", true);
            animator.Play("Idle");
        }

        // Fade back in
        yield return StartCoroutine(screenFader.FadeIn());

        // Restore physics
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Reset movement state
        movement.moveState = PlayerMovement.MoveState.Idle;
        movement.blockJump = false;
        movement.blockSprint = false;
        movement.blockRightMovement = false;
        movement.hasJumped = false;
        movement.SetCanMove(true);
        Physics.SyncTransforms();

        // Enable movement script
        movement.enabled = true;
        _isRespawning = false;
    }
}
