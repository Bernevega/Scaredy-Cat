using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager instance { get; private set; }
    public GameObject player;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            if (player)
            {
                DontDestroyOnLoad(player);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static GameObject PM_GetPlayer()
    {
        if (instance)
            return instance.player;
        return null;
    }
}
