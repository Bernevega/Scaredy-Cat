using UnityEngine;

[CreateAssetMenu(fileName = "ItemScriptable", menuName = "Scriptable Objects/ItemScriptable")]
public class ItemScriptable : ScriptableObject
{
    public GameObject itemPrefab;
    public ItemTags tags;

    private void OnValidate()
    {
        if (itemPrefab != null)
        {
            Item itemScript = itemPrefab.GetComponent<Item>();
            if (itemScript != null)
            {
                itemScript.itemInfo = this;
            }
        }
    }
}