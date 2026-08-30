using UnityEngine;

public class LoadZone : MonoBehaviour
{

    [SerializeField] GameObject[] gameObjects;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag != "Player")
            return;

        for (int i = 0; i < gameObjects.Length; i++)
        {
            if (gameObjects[i] != null)
            {
                gameObjects[i].SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag != "Player")
            return;

        for (int i = 0; i < gameObjects.Length; i++)
        {
            if (gameObjects[i] != null)
            {
                gameObjects[i].SetActive(false);
            }
        }
    }
}
