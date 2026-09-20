using UnityEngine;

public class DisableCollider : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            GetComponent<Collider>().enabled = false;
        }
    }
}