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
            shakeRoutine = StartCoroutine(Shake());
            StartCoroutine(HideAfterDelay());
        }
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(hideDelay);

        // Stop shaking before hiding
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            visualTarget.transform.localPosition = originalLocalPos;
        }

        visualTarget.SetActive(false);
    }

    private IEnumerator Shake()
    {
        while (true)
        {
            float shakeOffsetX = Mathf.Sin(Time.time * shakeSpeed) * shakeMagnitude;
            float shakeOffsetY = Mathf.Cos(Time.time * shakeSpeed) * shakeMagnitude;

            visualTarget.transform.localPosition = originalLocalPos + new Vector3(shakeOffsetX, shakeOffsetY, 0f);

            yield return null;
        }
    }

    public void ReappearNow()
    {
        visualTarget.SetActive(true);
        visualTarget.transform.localPosition = originalLocalPos;
        hasCollided = false;
    }
}
