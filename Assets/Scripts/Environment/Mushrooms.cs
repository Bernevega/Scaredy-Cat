using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(Collider))]
public class Mushrooms : MonoBehaviour
{
    [Header("Bounce Settings")]
    [Tooltip("Force applied upwards when the player hits this.")]
    public float bounceForce = 10f;

    [Tooltip("Apply bounce in this direction. If false, bounce is purely upward.")]
    public bool useSurfaceNormal = false;

    [Header("Sound")]
    [SerializeField] private AudioClip bounceSound;

    [Range(0f, 1f)]
    [SerializeField] private float bounceVolume = 1f;

    [SerializeField] private Vector2 pitchRange =
        new Vector2(0.9f, 1.1f);

    [Header("Audio Routing")]
    [Tooltip("Choose the Audio Mixer Group, e.g. Sounds, Music, etc.")]
    [SerializeField] private AudioMixerGroup audioOutput;

    private void Reset()
    {
        // Automatically set trigger on collider
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the other object has a Rigidbody
        Rigidbody rb = other.attachedRigidbody;

        if (rb == null || rb.isKinematic)
            return;

        // Only affect the player
        PlayerMovement player =
            rb.GetComponent<PlayerMovement>();

        if (player == null)
            return;

        // Bounce direction
        Vector3 bounceDir =
            useSurfaceNormal
                ? transform.up
                : Vector3.up;

        // Clear downward velocity before applying bounce
        Vector3 velocity = rb.linearVelocity;

        if (velocity.y < 0f)
            velocity.y = 0f;

        rb.linearVelocity = velocity;

        // Apply bounce
        rb.AddForce(
            bounceDir.normalized * bounceForce,
            ForceMode.Impulse
        );

        // Play bounce sound
        PlayBounceSound();
    }

    private void PlayBounceSound()
    {
        if (bounceSound == null)
            return;

        GameObject soundPlayer =
            ObjectPool.instance.objPool_GetObject(
                "2DSoundPlayer"
            );

        if (soundPlayer == null)
            return;

        SoundPlayer sp =
            soundPlayer.GetComponent<SoundPlayer>();

        if (sp == null)
            return;

        sp.transform.position = transform.position;

        PlaySoundInfo info =
            new PlaySoundInfo(bounceSound)
            {
                pitch = Random.Range(
                    pitchRange.x,
                    pitchRange.y
                ),

                volume = bounceVolume,

                mixer = audioOutput
            };

        sp.PlaySound(info);
    }
}