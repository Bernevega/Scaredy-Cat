using UnityEngine;

public class PlaySoundAfterDelay : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource; // Assign in Inspector
    [SerializeField] private AudioClip soundClip;     // Assign your sound here
    [Tooltip("Time in seconds after scene load before the sound plays")]
    public float delay = 5f;

    private void Start()
    {
        if (audioSource != null && soundClip != null)
        {
            Invoke(nameof(PlaySound), delay);
        }
    }

    private void PlaySound()
    {
        audioSource.PlayOneShot(soundClip);
    }
}
