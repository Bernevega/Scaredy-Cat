using System.Collections;
using UnityEngine;

public class LevelDirector : MonoBehaviour
{
    [SerializeField] Transform[] startPositions;

    public static int targetStartPosition = 0;
    [Tooltip("Dont teleport player to a start position")]
    public bool ignore = false;
    
    private void Start()
    {
        if (!ignore)
            StartCoroutine(TeleportPlayerCoroutine());
    }

    IEnumerator TeleportPlayerCoroutine()
    {
        yield return new WaitForFixedUpdate();
        if (targetStartPosition >= 0 && targetStartPosition < startPositions.Length)
        {
            PlayerManager.instance.player.transform.position = startPositions[targetStartPosition].position;
        }
        else if (targetStartPosition >= 0)
        {
            PlayerManager.instance.player.transform.position = startPositions[0].position;
        }
        
        Camera.main.GetComponent<CameraFollow>().target = PlayerManager.instance.player.transform;
        Camera.main.GetComponent<CameraFollow>().enabled = true;
    }
}
