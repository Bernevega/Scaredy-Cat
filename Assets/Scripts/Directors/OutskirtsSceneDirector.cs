using UnityEngine;

public class OutskirtsSceneDirector : MonoBehaviour
{
    [SerializeField] GameObject ghostPaper;
    [SerializeField] GameObject logBarrier;

    private void Awake()
    {
        if (Crow.hasBothMonocles)
        {
            ghostPaper.SetActive(true);
            logBarrier.SetActive(true);
        }
    }
}
