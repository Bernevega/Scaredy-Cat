using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class CrakingBranch : MonoBehaviour
{
    [Tooltip("Seconds to wait before hiding the branch after player collision.")]
    public float hideDelay = 3f;

    [Tooltip("The visual object to hide (e.g. mesh or model GameObject)")]
    public GameObject visualTarget;

    [Tooltip("Shaking strength in local units (e.g. 0.1 = subtle)")]
    public float shakeMagnitude = 0.05f;

    [Tooltip("Shaking speed (higher = faster wiggle)")]
    public float shakeSpeed = 30f;

    private bool hasCollided = false;
    private Coroutine shakeRoutine;
    private Coroutine hideRoutine;
    private Vector3 originalLocalPos;

    public static List<CrakingBranch> AllBranches = new List<CrakingBranch>();

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

        originalLocalPos = visualTarget.transform.localPosition;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasCollided) return;

        if (collision.gameObject.GetComponent<PlayerMovement>() != null)
        {
            hasCollided = true;

            if (shakeRoutine == null)
                shakeRoutine = StartCoroutine(Shake());

            if (hideRoutine == null)
                hideRoutine = StartCoroutine(HideAfterDelay());
        }
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(hideDelay);

        // Stop shaking before hiding
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
            if (visualTarget != null)
                visualTarget.transform.localPosition = originalLocalPos;
        }

        if (visualTarget != null)
            visualTarget.SetActive(false);

        hideRoutine = null;
    }

    private IEnumerator Shake()
    {
        while (true)
        {
            float shakeOffsetX = Mathf.Sin(Time.time * shakeSpeed) * shakeMagnitude;
            float shakeOffsetY = Mathf.Cos(Time.time * shakeSpeed) * shakeMagnitude;

            if (visualTarget != null)
                visualTarget.transform.localPosition = originalLocalPos + new Vector3(shakeOffsetX, shakeOffsetY, 0f);

            yield return null;
        }
    }

    public void ReappearNow()
    {
        // Fully reset a single branch immediately
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
            visualTarget.transform.localPosition = originalLocalPos;
        }

        hasCollided = false;
    }

    /// <summary>
    /// Call this from your respawn code to ensure no previously-touched branches
    /// disappear due to pre-death timers. It cancels any hide timers and resets state.
    /// </summary>
    public static void OnPlayerRespawned()
    {
        for (int i = 0; i < AllBranches.Count; i++)
        {
            var b = AllBranches[i];
            if (b != null)
                b.ResetAfterRespawn();
        }
    }

    private void ResetAfterRespawn()
    {
        // Cancel pending hide
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        // Stop shaking
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        // Ensure visible & reset position/state
        if (visualTarget != null)
        {
            visualTarget.SetActive(true);
            visualTarget.transform.localPosition = originalLocalPos;
        }

        hasCollided = false;
    }
}
