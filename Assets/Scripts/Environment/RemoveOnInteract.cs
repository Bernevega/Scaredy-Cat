using UnityEngine;

public class RemoveOnInteract : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.F))
        {
            Destroy(gameObject);
        }
    }
}