using UnityEngine;

public class ForestSceneStart : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PlayerManager pm = PlayerManager.instance;
        GameObject playerObj = null;
        PlayerMovement playerMoveScript = null;
        if (pm)
            playerObj = pm.player;
        if (playerObj)
            playerMoveScript = playerObj.GetComponent<PlayerMovement>();
        if (playerMoveScript)
        {
            playerMoveScript.WakeUp();
            playerMoveScript.blockSprint = false;
        }
            
    }
}
