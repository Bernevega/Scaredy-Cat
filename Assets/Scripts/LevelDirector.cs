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
        {
            Debug.Log("TRYING TO TELEPORT PLAYER");
            StartCoroutine(TeleportPlayerCoroutine());
            
        }
            
    }

    IEnumerator TeleportPlayerCoroutine()
    {
        Debug.Log("TELEPORTED PLAYER TO A START POSITION");
        yield return new WaitForFixedUpdate();
        if (targetStartPosition >= 0 && targetStartPosition < startPositions.Length)
        {
            GameObject player = PlayerManager.instance.player;
            Rigidbody playerRb = player.GetComponent<Rigidbody>();

            playerRb.linearVelocity = Vector3.zero;
            playerRb.MovePosition(startPositions[targetStartPosition].position);
            
        }
        else if (targetStartPosition >= 0)
        {
            PlayerManager.instance.player.transform.position = startPositions[0].position;
        }
        
        Camera.main.GetComponent<CameraFollow>().target = PlayerManager.instance.player.transform;
        Camera.main.GetComponent<CameraFollow>().enabled = true;
    }
}
