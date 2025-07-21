using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class CrakingBranch : MonoBehaviour
{
    [Tooltip("Time in seconds to fully fade out.")]
    public float fadeDuration = 3f;

    [Tooltip("Time in seconds before reappearing.")]
    public float reappearDelay = 5f;

    [Tooltip("The object to visually fade and hide (usually the mesh).")]
    public GameObject visualTarget;

    private bool hasCollided = false;
    private Renderer rend;
    private Material mat;
    private Color originalColor;

    private void Start()
    {
        if (visualTarget == null)
            visualTarget = gameObject;

        rend = visualTarget.GetComponent<Renderer>();
        mat = rend.material;
        originalColor = mat.color;

        Color startColor = originalColor;
        startColor.a = 1f;
        mat.color = startColor;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasCollided) return;

        if (collision.gameObject.GetComponent<PlayerMovement>() != null)
        {
            hasCollided = true;
            StartCoroutine(FadeOutAndRestore());
        }
    }

    private IEnumerator FadeOutAndRestore()
    {
        float elapsed = 0f;
        Color startColor = mat.color;
        Color endColor = startColor;
        endColor.a = 0f;

        // Fade out
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            mat.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        rend.enabled = false; // Hide the visuals (no deactivation)
        yield return new WaitForSeconds(reappearDelay);

        rend.enabled = true;
        mat.color = originalColor; // Restore fully visible color
        hasCollided = false; // Ready for next collision
    }
}
