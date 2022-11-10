using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Barotrauma;
using Barotrauma.Extensions;
using Barotrauma.Items.Components;
using HarmonyLib;

namespace HotkeyReload;

public static class Reloader
{
    public static void ReloadHeldItems()
    {
        //?? check if in-game && single player or host in multiplayer and we're not switching maps
        if (!GameMain.GameSession.IsRunning
            || Screen.Selected is SubEditorScreen
            || Submarine.Unloading)
            return;
        
        //Check if inventory available
        if (Character.Controlled.Inventory is null)
            return;
        
        var charInv = Character.Controlled.Inventory;
        
        foreach (Item heldItem in Character.Controlled.HeldItems
                     .Where(i=>i.OwnInventory is {
                         Capacity: > 0,
                         Locked: false 
                     }))
        {
            for (int slotIndex = 0; slotIndex < heldItem.OwnInventory.Capacity; slotIndex++)
            {
                ItemPrefab? prefItemPrefab = null;
                //item exists in inventory?
                if (heldItem.OwnInventory.GetItemAt(slotIndex) is Item {Condition: <=0} item)
                {
                    //yes--item condition 0?
                    //yes--remove item, mark replacement type preference
                    prefItemPrefab = item.Prefab;
                    if (!charInv.TryPutItem(item, Character.Controlled))
                    {
                        DebugConsole.LogError($"HK_Reload: Unable to remove depleted item from weapon/tool. Cannot continue");
                        continue;
                    }
                }
                
                //if empty find a suitable replacement
                if (heldItem.OwnInventory.GetItemAt(slotIndex) is null)
                {
                    if (Character.Controlled.Inventory.FindCompatWithPreference(
                            prefItemPrefab, slotIndex, item1 => item1.Condition > 0 && item1.ParentInventory != heldItem.OwnInventory) is { } it)
                    {
                        if (!heldItem.OwnInventory.TryPutItem(it, slotIndex, true, false, Character.Controlled))
                        {
                            DebugConsole.LogError($"HK_Reload: Unable to insert replacement item {it.Name} into item {heldItem.Name} in slot#{slotIndex}. Skipping this slot.");
                            continue;
                        }
                    }
                    else
                    {
                        DebugConsole.LogError($"HK_Reload: Unable to find replacement item to put into item {heldItem.Name} in slot#{slotIndex}. Skipping this slot.");
                        continue;
                    }
                }
                
                //is it not full stack?
                if (heldItem.OwnInventory.Capacity > 1
                    // ReSharper disable once HeapView.ClosureAllocation
                    && heldItem.OwnInventory.GetItemAt(slotIndex) is { } it1)
                {
                    int diff = heldItem.GetSlotMaxStackSize(it1, slotIndex) -
                               heldItem.OwnInventory.GetItemsAt(slotIndex).Count();
                    List<Item> refillItems = Character.Controlled.Inventory.FindAllCompatWithPreference(it1.Prefab, slotIndex,
                        item1 => item1.Condition > 0 
                                 && item1.Prefab == it1.Prefab
                                 && item1.ParentInventory != heldItem.OwnInventory);
                    foreach (Item refillItem in refillItems)
                    {
                        if (diff < 1)
                            break;
                        if (heldItem.OwnInventory.TryPutItem(refillItem, slotIndex, false, true, Character.Controlled))
                            diff -= 1;
                    }
                }
            }
            
        }
        
    }

    
    static List<Item> FindAllCompatWithPreference(this Inventory container, ItemPrefab? prefab = null, int slotIndex = -1, Func<Item, bool>? predicate = null)
    {
        List<Item> compat = new List<Item>();
        List<Item> pref = new List<Item>();
        
        foreach (Item item in container.FindAllItems(predicate, true))
        {
            if (!item.CompatibleWithInv(container, slotIndex))
                continue;
            if (prefab is null || prefab == item.Prefab) 
                pref.Add(item);
            compat.Add(item);
        }

        return pref.Any() ? pref : compat;
    }
    
    
    static Item? FindCompatWithPreference(this Inventory container, ItemPrefab? prefab = null, int slotIndex = -1, Func<Item, bool>? predicate = null)
    {
        Item? foundItem = null;
        foreach (Item item in container.FindAllItems(predicate, true))
        {
            if (!item.CompatibleWithInv(container, slotIndex))
                continue;
            if (prefab is null || prefab == item.Prefab) 
                return item;
            foundItem = item;
        }
        return foundItem;
    }

    static int GetSlotMaxStackSize(this Item containerItem, Item storableItem, int slotIndex) =>
        containerItem.GetComponent<ItemContainer>() is { } container 
        && container.CanBeContained(storableItem, slotIndex)
            ? Math.Min(container.MaxStackSize, storableItem.Prefab.MaxStackSize)
            : 0;

    static bool CompatibleWithInv(this Item item, Inventory container, int slotIndex = -1) => container.Owner switch
        {
            Item ownerItem when slotIndex == -1 => CompatibleWithInv(item, ownerItem),
            Item ownerItem => CompatibleWithInv(item, ownerItem, slotIndex),
            Character character when slotIndex == -1 => CompatibleWithInv(item, character),
            Character character => CompatibleWithInv(item, character, slotIndex),
            _ => false
        };

    static bool CompatibleWithInv(this Item item, Character character) => character.Inventory.CanBePut(item);
    static bool CompatibleWithInv(this Item item, Character character, int slotIndex) => character.Inventory.CanBePutInSlot(item, slotIndex);
    static bool CompatibleWithInv(this Item item, Item containerParent, int slotIndex) =>
        containerParent.GetComponent<ItemContainer>()?.CanBeContained(item, slotIndex) ?? false;
    static bool CompatibleWithInv(this Item item, Item containerParent) =>
        containerParent.GetComponent<ItemContainer>()?.CanBeContained(item) ?? false;
    
    static bool IncludeInUsableItemList(Item item, Item heldItem, int slot) => 
        item.Condition > 0.0f
        && item != heldItem
        && item.CompatibleWithInv(heldItem, slot);
    
    
    static bool NestedInventoryHelper(Item item, Item heldItem, List<Item> list, int slot, int nestlevel = 1)
    {
        //it does not have an inventory, exit out
        if (item.OwnInventory is null || item.OwnInventory.Capacity < 1)
            return true;

        //is it something that needs items to function (ie. a tool)? Exclude it.
        if (item.GetComponent<ItemContainer>()?.requiredItems.Any() ?? false)
            return false;

        DebugConsole.LogError($"N-level {nestlevel}: ContainerName {item.Name} | ContainerCond {item.Condition}");

        //it's a backpack/bag, let's search it
        foreach (Item item1 in item.OwnInventory.AllItemsMod)
        {
            DebugConsole.LogError($"N-level {nestlevel}: SubItem {item1.Name} | SubCond {item1.Condition}");

            if (IncludeInUsableItemList(item1, heldItem))
            {
                DebugConsole.LogError($"N-level {nestlevel}: Added {item1.Name}");
                list.Add(item1);
            }
            else if (item1 != heldItem)
            {
                DebugConsole.LogError($"N-level {nestlevel}: FAILED {item1.Name}");
                NestedInventoryHelper(item1, heldItem, list, slot, nestlevel + 1);
            }
        }
        return false;   //don't add backpack/bag to list
    }
}