using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public InventoryScript inventoryScript;

    private void Awake()
    {
        Instance = this;

        if (inventoryScript == null)
        {
            inventoryScript = GetComponent<InventoryScript>();
        }
    }
}
