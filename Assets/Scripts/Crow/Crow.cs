using UnityEngine;

public class Crow : MonoBehaviour
{
    [SerializeField] Interactable interactable;
    [SerializeField] ItemScriptable monocleScriptable;

    Item[] itemReturnArray = new Item[1];
    bool hasMonocle = false;

    private void Awake()
    {
        interactable.eventOnInteract += OnInteract;
    }

    public void OnInteract(Interactor interactor, Interactable interactable, InteractActionType type)
    {
        if (type == InteractActionType.Interact)
        {
            checkMonocle(interactor);
        }
    }

    public void checkMonocle(Interactor interactor)
    {
        if (!hasMonocle)
        {
            InventoryScript inventoryScript = interactor.GetOwner().GetComponent<InventoryScript>();
            int numItem = inventoryScript.GetItem_WithScriptable(itemReturnArray, monocleScriptable);

            if (numItem > 0)
            {
                inventoryScript.RemoveItem(itemReturnArray[0], -1);
                Destroy(itemReturnArray[0]);
                hasMonocle = true;
                Debug.Log("MONOCLE Given!");
            }
            else
            {
                Debug.Log("NO MONOCLE FOUND!");
            }
        }
    }
}
