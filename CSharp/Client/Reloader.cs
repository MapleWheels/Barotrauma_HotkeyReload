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

        List<Item> usableItems = new();
        foreach (Item heldItem in Character.Controlled.HeldItems.Where(i=>i.OwnInventory != null && i.OwnInventory.Capacity > 0))
        {
            usableItems.Clear();
            foreach (Item item in charInv.AllItemsMod)
            {
                DebugConsole.LogError($"N-level 0: MainItem {item.Name} | MainCond {item.Condition}");
                if (IncludeInUsableItemList(item, heldItem))
                {
                    DebugConsole.LogError($"N-level 0: Added {item.Name}");
                    usableItems.Add(item);
                }
                else if (item != heldItem)
                {
                    DebugConsole.LogError($"N-level 0: FAILED {item.Name}");
                    NestedInventoryHelper(item, heldItem, usableItems);
                }
            }
#warning debug
            DebugConsole.LogError($"Step 1 reached.");
            foreach (Item item in usableItems)
            {
                DebugConsole.LogError($"Item1: {item.Name}");
            }

            for (int slotIndex = 0; slotIndex < heldItem.OwnInventory.Capacity; slotIndex++)
            {
                bool exitIter = false;
                //There's no item in the slot, so get one in there
                if (heldItem.OwnInventory.GetItemAt(slotIndex) is null)
                {
                    foreach (Item item in usableItems.ToList()) //allow modifications, consider for;RemoveAt(ind) instead for speed.
                    {
                        if (!heldItem.OwnInventory.CanBePutInSlot(item, slotIndex)) //fast out
                            continue;

                        if (heldItem.OwnInventory.TryPutItem(item, slotIndex, false, false, Character.Controlled))
                        {
                            usableItems.Remove(item);
                            if (item.Prefab.MaxStackSize == 1)  //replacement complete
                                exitIter = true;
                            break;
                        }
                    }
                    
#warning debug
                    foreach (Item item in usableItems)
                    {
                        DebugConsole.LogError($"Item2: {item.Name}");
                    }
                    
                    if (exitIter)
                        continue;
                }
                
#warning debug
                foreach (Item item in usableItems)
                {
                    DebugConsole.LogError($"Item3: {item.Name}");
                }
                
                if (heldItem.OwnInventory.GetItemAt(slotIndex) is { Prefab: { } prefab } containedItem)
                {
                    if (prefab.MaxStackSize == 1 && containedItem.Condition <= float.Epsilon)
                    {
                        foreach (Item item in usableItems.ToList()) //allow modifications, consider for;RemoveAt(ind) instead for speed.
                        {
#warning debug
                            DebugConsole.LogError($"Item4: {item.Name}");
                            if (item.Prefab != containedItem.Prefab)    //fast out
                                continue;
#warning debug
                            DebugConsole.LogError($"Item5: {item.Name}");
                            
                            if (heldItem.OwnInventory.TryPutItem(item, slotIndex, true, false, Character.Controlled))
                            {
                                usableItems.Remove(item);
                                exitIter = true;
                                break;
                            }
                        }
                        
                        if (exitIter)
                            continue;
                    }

                    if (prefab.MaxStackSize > 1)
                    {
                        foreach (Item item in usableItems.ToList())
                        {
                            if (item.Prefab != containedItem.Prefab)    //fast out
                                continue;
                            
                            if (heldItem.OwnInventory.TryPutItem(item, slotIndex, false, false, Character.Controlled))
                            {
                                usableItems.Remove(item);
                                if (heldItem.OwnInventory.GetItemsAt(slotIndex)?.Count() >= containedItem.Prefab.MaxStackSize)
                                {
                                    exitIter = true;
                                    break;
                                }
                            }
                            
                            
                        }
                        
                        if (exitIter)
                            continue;
                    }
                }
            }
        }

        
        static bool NestedInventoryHelper(Item item, Item heldItem, List<Item> list, int nestlevel = 1)
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
                    NestedInventoryHelper(item1, heldItem, list, nestlevel + 1);
                }
            }
            return false;   //don't add backpack/bag to list
        }

        static bool IncludeInUsableItemList(Item item, Item heldItem)
        {
            if (item.Condition > 0f)
            {
                DebugConsole.LogError($"ItemCh.Cond: {item.Name}");
            }

            if (item != heldItem)
            {
                DebugConsole.LogError($"ItemCh.HeldItem: {item.Name} | held: {heldItem.Name}");
            }

            if (heldItem.OwnInventory.CanBePut(item))
            {
                DebugConsole.LogError($"ItemCh.CanPut: {item.Name} | held: {heldItem.Name}");
            }

#warning TODO: Find a replacement for CanBePut()
            return item.Condition > 0.0f
                   && item != heldItem
                   && heldItem.OwnInventory.CanBePut(item); //needs to be replaced as it returns false if there is an item in the slot.
        }
    }
}