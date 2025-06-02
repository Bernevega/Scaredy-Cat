using UnityEngine;

public class CatMom : MonoBehaviour
{
    [Tooltip("This must match the top‐level key in your JSON, e.g. \"HomeScene\"")]
    public string sceneID = "HomeScene";

    void Start()
    {
        // Wait one frame (optional) to ensure that SimpleDialogManager.Instance has initialized.
        // If your SimpleDialogManager is on the same GameObject and initializes in Awake(), you can
        // call StartDialogue() directly without any delay.
        SimpleDialogManager.Instance.StartDialogue(sceneID);
    }
}
