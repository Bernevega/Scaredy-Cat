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
    }

    public int GetItem_WithScriptable(Item[] returnArray, ItemScriptable itemInfo)
    {
        int itemCount = 0;
        for (int i = 0; i < inventory.Count; i++)
        {
            if (inventory[i] == null) continue;
            if (inventory[i].itemInfo == itemInfo)
            {
                if (returnArray != null && itemCount < returnArray.Length)
                    returnArray[itemCount] = inventory[i];
                itemCount++;
            }
        }
        return itemCount;
    }

    public int GetItem_WithTags(Item[] returnArray, ItemTags itemTags)
    {
        int itemCount = 0;
        for (int i = 0; i < inventory.Count; i++)
        {
            if (inventory[i] == null) continue;
            if (inventory[i].itemInfo.tags.HasFlag(itemTags))
            {
                if (returnArray != null && itemCount < returnArray.Length)
                    returnArray[itemCount] = inventory[i];
                itemCount++;
            }
        }
        return itemCount;
    }

    public void AddItem(Item itemObject)
    {
        bool itemAdded = false;
        if (!itemObject.itemInfo.tags.HasFlag(ItemTags.NonStackable))
        {
            for (int i = 0; i < inventory.Count; i++)
            {
                if (inventory[i].itemInfo == itemObject.itemInfo)
                {
                    inventory[i].AddStack(itemObject.stack);
                    itemAdded = true;
                    break;
                }
            }
        }
        if (!itemAdded)
        {
            inventory.Add(itemObject);
        }
    }

    public void AddItem(ItemScriptable itemScriptable, int amount)
    {
        if (amount < 0) amount = 0;

        bool itemAdded = false;
        if (!itemScriptable.tags.HasFlag(ItemTags.NonStackable))
        {
            for (int i = 0; i < inventory.Count; i++)
            {
                if (inventory[i].itemInfo == itemScriptable)
                {
                    inventory[i].AddStack(amount);
                    itemAdded = true;
                    break;
                }
            }
        }

        if (!itemAdded)
        {
            Item itemScript = Instantiate(itemScriptable.itemPrefab).GetComponent<Item>();

            if (itemScript != null)
            {
                inventory.Add(itemScript);
            }
        }
    }

    public void RemoveItem(Item item, int amount)
    {
        if (amount == 0)
            return;

        for (int i = 0; i < inventory.Count; i++)
        {
            if (inventory[i] == item)
            {
                if (amount > 0)
                    inventory[i].AddStack(-amount);
                else if (amount < 0)
                    inventory[i].AddStack(-inventory[i].stack);
                if (inventory[i].stack <= 0)
                {
                    inventory.RemoveAt(i);
                }
                break;
            }
        }
    }
}
