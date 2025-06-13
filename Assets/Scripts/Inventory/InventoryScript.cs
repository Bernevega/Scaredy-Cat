using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;

public class InventoryScript : MonoBehaviour
{
    [SerializeField] int reserve = 1;
    [SerializeField] protected List<Item> inventory;

    private void Awake()
    {
        inventory = new List<Item>(reserve);
        Debug.Log($"[InventoryScript] Awake: Initialized inventory with capacity {reserve}");
    }

    public int GetItem_WithScriptable(Item[] returnArray, ItemScriptable itemInfo)
    {
        Debug.Log($"[InventoryScript] GetItem_WithScriptable: Searching for {itemInfo.name}");
        int itemCount = 0;
        for (int i = 0; i < inventory.Count; i++)
        {
            var it = inventory[i];
            if (it == null)
            {
                Debug.Log($"[InventoryScript] GetItem_WithScriptable: Slot {i} is null, skipping");
                continue;
            }
            if (it.itemInfo == itemInfo)
            {
                Debug.Log($"[InventoryScript] GetItem_WithScriptable: Found match at slot {i}");
                if (returnArray != null && itemCount < returnArray.Length)
                {
                    returnArray[itemCount] = it;
                    Debug.Log($"[InventoryScript] GetItem_WithScriptable: Added to returnArray[{itemCount}]");
                }
                itemCount++;
            }
        }
        Debug.Log($"[InventoryScript] GetItem_WithScriptable: Total found = {itemCount}");
        return itemCount;
    }

    public int GetItem_WithTags(Item[] returnArray, ItemTags itemTags)
    {
        Debug.Log($"[InventoryScript] GetItem_WithTags: Searching for tags {itemTags}");
        int itemCount = 0;
        for (int i = 0; i < inventory.Count; i++)
        {
            var it = inventory[i];
            if (it == null)
            {
                Debug.Log($"[InventoryScript] GetItem_WithTags: Slot {i} is null, skipping");
                continue;
            }
            if (it.itemInfo.tags.HasFlag(itemTags))
            {
                Debug.Log($"[InventoryScript] GetItem_WithTags: Found match at slot {i} (tags {it.itemInfo.tags})");
                if (returnArray != null && itemCount < returnArray.Length)
                {
                    returnArray[itemCount] = it;
                    Debug.Log($"[InventoryScript] GetItem_WithTags: Added to returnArray[{itemCount}]");
                }
                itemCount++;
            }
        }
        Debug.Log($"[InventoryScript] GetItem_WithTags: Total found = {itemCount}");
        return itemCount;
    }

    public void AddItem(Item itemObject)
    {
        if (itemObject == null)
        {
            Debug.LogWarning("[InventoryScript] AddItem: Tried to add null Item");
            return;
        }
        Debug.Log($"[InventoryScript] AddItem(Item): Adding '{itemObject.itemInfo.name}' x{itemObject.stack}");
        bool itemAdded = false;
        if (!itemObject.itemInfo.tags.HasFlag(ItemTags.NonStackable))
        {
            for (int i = 0; i < inventory.Count; i++)
            {
                if (inventory[i].itemInfo == itemObject.itemInfo)
                {
                    inventory[i].AddStack(itemObject.stack);
                    itemAdded = true;
                    Debug.Log($"[InventoryScript] AddItem: Stacked onto slot {i}, new stack = {inventory[i].stack}");
                    break;
                }
            }
        }
        if (!itemAdded)
        {
            inventory.Add(itemObject);
            Debug.Log($"[InventoryScript] AddItem: Added new item to inventory, total slots = {inventory.Count}");
        }
    }

    public void AddItem(ItemScriptable itemScriptable, int amount)
    {
        if (itemScriptable == null)
        {
            Debug.LogWarning("[InventoryScript] AddItem: Tried to add null ItemScriptable");
            return;
        }
        if (amount < 0)
        {
            Debug.Log($"[InventoryScript] AddItem: Negative amount {amount} clamped to 0");
            amount = 0;
        }
        Debug.Log($"[InventoryScript] AddItem(Scriptable): Adding '{itemScriptable.name}' x{amount}");
        bool itemAdded = false;
        if (!itemScriptable.tags.HasFlag(ItemTags.NonStackable))
        {
            for (int i = 0; i < inventory.Count; i++)
            {
                if (inventory[i].itemInfo == itemScriptable)
                {
                    inventory[i].AddStack(amount);
                    itemAdded = true;
                    Debug.Log($"[InventoryScript] AddItem: Stacked onto slot {i}, new stack = {inventory[i].stack}");
                    break;
                }
            }
        }
        if (!itemAdded)
        {
            var instance = Instantiate(itemScriptable.itemPrefab);
            var itemScript = instance.GetComponent<Item>();
            if (itemScript != null)
            {
                itemScript.AddStack(amount - itemScript.stack); // ensure correct initial stack
                inventory.Add(itemScript);
                Debug.Log($"[InventoryScript] AddItem: Instantiated and added '{itemScriptable.name}', total slots = {inventory.Count}");
            }
            else
            {
                Debug.LogError($"[InventoryScript] AddItem: Prefab for '{itemScriptable.name}' has no Item component");
                Destroy(instance);
            }
        }
    }

    public void RemoveItem(Item item, int amount)
    {
        if (item == null)
        {
            Debug.LogWarning("[InventoryScript] RemoveItem: Tried to remove null Item");
            return;
        }
        if (amount == 0)
        {
            Debug.Log("[InventoryScript] RemoveItem: Amount is 0, nothing to do");
            return;
        }
        Debug.Log($"[InventoryScript] RemoveItem: Removing '{item.itemInfo.name}', amount = {amount}");
        for (int i = 0; i < inventory.Count; i++)
        {
            if (inventory[i] == item)
            {
                if (amount > 0)
                {
                    inventory[i].AddStack(-amount);
                    Debug.Log($"[InventoryScript] RemoveItem: Decreased stack to {inventory[i].stack}");
                }
                else if (amount < 0)
                {
                    inventory[i].AddStack(-inventory[i].stack);
                    Debug.Log($"[InventoryScript] RemoveItem: Clearing entire stack");
                }
                if (inventory[i].stack <= 0)
                {
                    inventory.RemoveAt(i);
                    Debug.Log($"[InventoryScript] RemoveItem: Item removed from slot {i}, total slots = {inventory.Count}");
                }
                break;
            }
        }
    }
}
