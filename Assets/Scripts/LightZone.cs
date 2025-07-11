// LightPart.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LightZone : MonoBehaviour
{
    [Tooltip("Assign the Labyrinth GameObject with the Labyrinth script here.")]
    public Labyrinth labyrinth;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && labyrinth != null && !labyrinth.reviving)
        {
            labyrinth.PauseFadeTimer();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && labyrinth != null)
        {
            labyrinth.ResumeFadeTimer(other.gameObject);
        }
    }
}
