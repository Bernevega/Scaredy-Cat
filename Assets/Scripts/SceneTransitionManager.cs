using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

[DisallowMultipleComponent]
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Fade Settings")]
    [Tooltip("A full-screen UI Image (black) covering the scene.")]
    public Image fadeImage;
    [Tooltip("Duration for both fade-out and fade-in.")]
    public float fadeDuration = 1f;

    // Internal flag so we don't double-fade
    private bool _isTransitioning = false;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Ensure we have a fade image
        if (fadeImage == null)
            Debug.LogError("[SceneTransitionManager] No fadeImage assigned!", this);

        // Start fully opaque so we can fade in
        SetAlpha(1f);
        fadeImage.raycastTarget = true;

        // Hook the sceneLoaded callback to fade in after loading
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        // Kick off initial fade-in
        StartCoroutine(FadeIn());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // After the new scene is loaded, perform the fade-in
        StartCoroutine(FadeIn());
    }

    /// <summary>
    /// Call this to load the *next* scene in Build Settings (or wrap to first).
    /// </summary>
    public void LoadNextScene()
    {
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next >= SceneManager.sceneCountInBuildSettings)
            next = 0;
        LoadScene(next);
    }

    /// <summary>
    /// Call this to load any scene by build index.
    /// </summary>
    public void LoadScene(int buildIndex)
    {
        if (_isTransitioning) return;
        StartCoroutine(TransitionThenLoad(buildIndex));
    }

    /// <summary>
    /// Call this to load any scene by name.
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (_isTransitioning) return;
        StartCoroutine(TransitionThenLoad(sceneName));
    }

    private IEnumerator TransitionThenLoad(int buildIndex)
    {
        _isTransitioning = true;
        yield return StartCoroutine(FadeOut());
        SceneManager.LoadScene(buildIndex);
    }

    private IEnumerator TransitionThenLoad(string sceneName)
    {
        _isTransitioning = true;
        yield return StartCoroutine(FadeOut());
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeOut()
    {
        // Block raycasts so UI/input is disabled
        fadeImage.raycastTarget = true;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }
        SetAlpha(1f);
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(1f - Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }
        SetAlpha(0f);
        // Allow input again
        fadeImage.raycastTarget = false;
        _isTransitioning = false;
    }

    private void SetAlpha(float a)
    {
        if (fadeImage != null)
        {
            var c = fadeImage.color;
            c.a = a;
            fadeImage.color = c;
        }
    }
}
