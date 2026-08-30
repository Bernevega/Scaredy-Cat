using UnityEngine;

public class TouchForDialogue : MonoBehaviour
{
    [SerializeField] string dialogToStart = "";

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Player" &&
            SimpleDialogManager.Instance)
        {
           SimpleDialogManager.Instance.StartDialogue(dialogToStart);
        }
    }
}
