using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class LightZone : MonoBehaviour
{
    [Tooltip("Assign the Labyrinth GameObject with the Labyrinth script here.")]
    public Labyrinth labyrinth;

    private readonly HashSet<Collider> playerColliders =
        new HashSet<Collider>();

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerMovement pm =
            other.GetComponentInParent<PlayerMovement>();

        if (pm == null || !pm.CompareTag("Player"))
            return;

        // Already tracking this collider
        if (!playerColliders.Add(other))
            return;

        // Only pause when the FIRST player collider enters
        if (playerColliders.Count == 1 &&
            labyrinth != null &&
            !labyrinth.reviving)
        {
            labyrinth.PauseFadeTimer();
        }
    }

    void OnTriggerExit(Collider other)
    {
        PlayerMovement pm =
            other.GetComponentInParent<PlayerMovement>();

        if (pm == null || !pm.CompareTag("Player"))
            return;

        if (!playerColliders.Remove(other))
            return;

        // Only resume once ALL player colliders have left
        if (playerColliders.Count == 0 &&
            labyrinth != null)
        {
            labyrinth.ResumeFadeTimer(pm.gameObject);
        }
    }
}