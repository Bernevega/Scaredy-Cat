using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class SceneTransitionZone : MonoBehaviour
{
    [Tooltip("Exact name of the next Scene to load (as in Build Settings)")]
    public string nextSceneName;

    [Tooltip("Reference to your full-screen ScreenFader component")]
    public ScreenFader screenFader;

    [Tooltip("If true, only the player can trigger this; otherwise any collider works")]
    public bool onlyPlayer = true;

    private Collider _col;
    private bool _isTransitioning = false;

    private void Awake()
    {
        _col = GetComponent<Collider>();
        _col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isTransitioning) return;

        if (onlyPlayer && !other.CompareTag("Player"))
            return;

        StartCoroutine(FadeAndLoad());
    }

    private IEnumerator FadeAndLoad()
    {
        _isTransitioning = true;

        // 1) Fade to black
        if (screenFader != null)
            yield return StartCoroutine(screenFader.FadeOut());
        else
            Debug.LogWarning("[SceneTransitionZone] No ScreenFader assigned; skipping fade.");

        // 2) Load the scene
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
        else
            Debug.LogError("[SceneTransitionZone] nextSceneName is empty!");
    }
}
