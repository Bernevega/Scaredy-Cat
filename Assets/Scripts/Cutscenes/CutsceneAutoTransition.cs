using UnityEngine;
using UnityEngine.SceneManagement;

public class CutsceneAutoTransition : MonoBehaviour
{
    [SerializeField] AnimEventInvoker animEvent;
    [SerializeField] string nextScene;

    private void Awake()
    {
        animEvent.stringEvent += OnCutsceneEnd;
    }

    private void OnCutsceneEnd(string eventName)
    {
        Invoke("LoadNextScene", 1f);
        
    }

    private void LoadNextScene()
    {
        SceneManager.LoadScene(nextScene);
    }
}
