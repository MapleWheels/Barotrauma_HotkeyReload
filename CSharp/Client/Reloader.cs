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
        if (GameMain.Client == null || !GameMain.GameSession.IsRunning || Screen.Selected is SubEditorScreen || Submarine.Unloading)
             return;
        
        //Check if inventory available
        if (Character.Controlled.Inventory == null)
            return;

        var charInv = Character.Controlled.Inventory;

        List<Item> usableItems = new();
        foreach (Item heldItem in Character.Controlled.HeldItems.Where(i=>i.OwnInventory != null && i.OwnInventory.Capacity > 0))
        {
            usableItems.Clear();
            usableItems.AddRange(charInv.AllItemsMod.Where(
                item => IncludeInUsableItemList(item, heldItem)
                        && NestedInventoryHelper(item, heldItem, usableItems)
            ));

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
                    if (exitIter)
                        continue;
                }
                
                if (heldItem.OwnInventory.GetItemAt(slotIndex) is { Prefab: { } prefab } containedItem)
                {
                    if (prefab.MaxStackSize == 1 && containedItem.Condition <= float.Epsilon)
                    {
                        foreach (Item item in usableItems.ToList()) //allow modifications, consider for;RemoveAt(ind) instead for speed.
                        {
                            if (item.Prefab != containedItem.Prefab)    //fast out
                                continue;
                                
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

        
        static bool NestedInventoryHelper(Item item, Item heldItem, List<Item> list)
        {
            if (item.OwnInventory is null || item.OwnInventory.Capacity < 1)
                return true;
            
            if (item.GetComponent<ItemContainer>()?.requiredItems.Any() ?? false)
                return false;
            
            list.AddRange(item.OwnInventory.AllItemsMod.Where(
                    item => IncludeInUsableItemList(item, heldItem)
                            && NestedInventoryHelper(item, heldItem, list)
                ));
            return false;
        }

        static bool IncludeInUsableItemList(Item item, Item heldItem) =>
            item.Condition > float.Epsilon
            && item != heldItem
            && heldItem.OwnInventory.CanBePut(item);
    }
}