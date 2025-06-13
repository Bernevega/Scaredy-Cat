using UnityEngine;

public class OutskirtsSceneDirector : MonoBehaviour
{
    [SerializeField] GameObject ghostPaper;

    private void Awake()
    {
        if (Crow.hasBothMonocles)
        {
            ghostPaper.SetActive(true);
        }
    }
}
