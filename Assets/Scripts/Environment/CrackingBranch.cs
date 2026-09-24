using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class CrakingBranch : MonoBehaviour
{
    [Header("Branch Settings")]
    [Tooltip("Seconds to wait before hiding the branch after player collision.")]
    public float hideDelay = 3f;

    [Tooltip("The visual object to hide (e.g. mesh or model GameObject)")]
    public GameObject visualTarget;

    [Tooltip("Shaking strength in local units (e.g. 0.1 = subtle)")]
    public float shakeMagnitude = 0.05f;

    [Tooltip("Shaking speed (higher = faster wiggle)")]
    public float shakeSpeed = 30f;

    [Header("Audio")]
    [Tooltip("Sound played when the player first steps on the branch.")]
    [SerializeField] private AudioClip crackingSound;

    [Tooltip("Sound played when the branch breaks/disappears.")]
    [SerializeField] private AudioClip breakingSound;

    [Header("Cracking Sound")]
    [Range(0f, 1f)]
    [SerializeField] private float crackingVolume = 1f;

    [SerializeField] private Vector2 crackingPitchRange =
        new Vector2(0.95f, 1.05f);

    [Header("Breaking Sound")]
    [Range(0f, 1f)]
    [SerializeField] private float breakingVolume = 1f;

    [SerializeField] private Vector2 breakingPitchRange =
        new Vector2(0.95f, 1.05f);

    [Header("Audio Routing")]
    [Tooltip("Choose the Audio Mixer Group, e.g. Sounds, Music, etc.")]
    [SerializeField] private AudioMixerGroup audioOutput;

    private bool hasCollided = false;
    private Coroutine shakeRoutine;
    private Coroutine hideRoutine;
    private Vector3 originalLocalPos;

    public static List<CrakingBranch> AllBranches =
        new List<CrakingBranch>();

    private void Awake()
    {
        AllBranches.Add(this);
    }

    private void OnDestroy()
    {
        AllBranches.Remove(this);
    }

    private void Start()
    {
        if (visualTarget == null)
            visualTarget = gameObject;

        originalLocalPos =
            visualTarget.transform.localPosition;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasCollided)
            return;

        if (
            collision.gameObject.GetComponent<PlayerMovement>() != null
        )
        {
            hasCollided = true;

            // Play initial cracking sound.
            PlayCrackingSound();

            if (shakeRoutine == null)
                shakeRoutine = StartCoroutine(Shake());

            if (hideRoutine == null)
                hideRoutine =
                    StartCoroutine(HideAfterDelay());
        }
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(hideDelay);

        // Stop shaking before breaking.
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;

            if (visualTarget != null)
            {
                visualTarget.transform.localPosition =
                    originalLocalPos;
            }
        }

        // Play the breaking sound.
        PlayBreakingSound();

        if (visualTarget != null)
            visualTarget.SetActive(false);

        hideRoutine = null;
    }

    private IEnumerator Shake()
    {
        while (true)
        {
            float shakeOffsetX =
                Mathf.Sin(Time.time * shakeSpeed) *
                shakeMagnitude;

            float shakeOffsetY =
                Mathf.Cos(Time.time * shakeSpeed) *
                shakeMagnitude;

            if (visualTarget != null)
            {
                visualTarget.transform.localPosition =
                    originalLocalPos +
                    new Vector3(
                        shakeOffsetX,
                        shakeOffsetY,
                        0f
                    );
            }

            yield return null;
        }
    }

    private void PlayCrackingSound()
    {
        if (crackingSound == null)
            return;

        PlaySound(
            crackingSound,
            crackingVolume,
            crackingPitchRange
        );
    }

    private void PlayBreakingSound()
    {
        if (breakingSound == null)
            return;

        PlaySound(
            breakingSound,
            breakingVolume,
            breakingPitchRange
        );
    }

    private void PlaySound(
        AudioClip clip,
        float volume,
        Vector2 pitchRange
    )
    {
        if (clip == null)
            return;

        if (ObjectPool.instance == null)
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
            new PlaySoundInfo(clip)
            {
                pitch = Random.Range(
                    pitchRange.x,
                    pitchRange.y
                ),

                volume = volume,

                mixer = audioOutput
            };

        sp.PlaySound(info);
    }

    public void ReappearNow()
    {
        // Fully reset a single branch immediately.
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        if (visualTarget != null)
        {
            visualTarget.SetActive(true);

            visualTarget.transform.localPosition =
                originalLocalPos;
        }

        hasCollided = false;
    }

    /// <summary>
    /// Call this from your respawn code to ensure no previously-touched
    /// branches disappear due to pre-death timers.
    /// </summary>
    public static void OnPlayerRespawned()
    {
        for (int i = 0; i < AllBranches.Count; i++)
        {
            CrakingBranch b = AllBranches[i];

            if (b != null)
                b.ResetAfterRespawn();
        }
    }

    private void ResetAfterRespawn()
    {
        // Cancel pending hide.
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        // Stop shaking.
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        // Ensure visible and reset position/state.
        if (visualTarget != null)
        {
            visualTarget.SetActive(true);

            visualTarget.transform.localPosition =
                originalLocalPos;
        }

        hasCollided = false;
    }
}