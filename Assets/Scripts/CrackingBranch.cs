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

    private bool hasCollided = false;

    // Static list to track all CrakingBranch instances
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
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasCollided) return;

        if (collision.gameObject.GetComponent<PlayerMovement>() != null)
        {
            hasCollided = true;
            StartCoroutine(HideAfterDelay());
        }
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(hideDelay);
        visualTarget.SetActive(false);
    }

    public void ReappearNow()
    {
        visualTarget.SetActive(true);
        hasCollided = false;
    }
}
