using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Barotrauma;
using Barotrauma.Extensions;
using Barotrauma.Items.Components;
using HarmonyLib;

namespace HotkeyReload;

public static class Reloader
{
    /// <summary>
    /// Removes the inventory items of the item currently being held by the player (hands) if the condition is below 0
    /// and will then try to replace the item with another equivalent from the player's inventory, prioritizes trying
    /// to put the same item type. 
    /// </summary>
    public static void ReloadHeldItems()
    {
        //?? check if in-game && single player or host in multiplayer and we're not switching maps
        if (!Util.CheckIfValidToInteract() || !Util.CheckIfCharacterReady(Character.Controlled))
            return;
        
        var charInv = Character.Controlled.Inventory;

        if (Character.Controlled.HeldItems != null)
        {
            ImmutableList<Item> heldItems = Character.Controlled.HeldItems.ToImmutableList();

            foreach (Item heldItem in heldItems
                         .Where(i => i.OwnInventory is
                         {
                             Capacity: > 0,
                             Locked: false
                         }))
            {
                for (int slotIndex = 0; slotIndex < heldItem.OwnInventory.Capacity; slotIndex++)
                {
                    ItemPrefab? prefItemPrefab = null;
                    //item exists in inventory?
                    if (heldItem.OwnInventory.GetItemAt(slotIndex) is Item { Condition: <= 0 } item)
                    {
                        //yes--item condition 0?
                        //yes--remove item, mark replacement type preference
                        prefItemPrefab = item.Prefab;
                        if (!charInv.TryPutItem(item, Character.Controlled, new[] { InvSlotType.Any }))
                            continue;
                    }

                    //if empty find a suitable replacement
                    if (heldItem.OwnInventory.GetItemAt(slotIndex) is null)
                    {
                        if (Character.Controlled.Inventory.FindCompatWithPreference(
                                heldItem, prefItemPrefab, slotIndex, item1 =>
                                    item1.Condition > 0
                                    && !item1.IsLimbSlotItem(Character.Controlled)
                                    && !heldItems.IsAnyOwnerOf(item1)
                            ) is { } it)
                        {
                            if (!heldItem.OwnInventory.TryPutItem(it, slotIndex, true, false, Character.Controlled))
                                continue;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    //is it not full stack?
                    int diff;
                    if (heldItem.OwnInventory.GetItemAt(slotIndex) is { } it1
                        && (diff = heldItem.GetSlotMaxStackSize(it1, slotIndex) -
                                   heldItem.OwnInventory.GetItemsAt(slotIndex).Count()) > 0)
                    {
                        List<Item> refillItems = Character.Controlled.Inventory.FindAllCompatWithPreference(heldItem,
                            it1.Prefab, slotIndex,
                            item1 => item1.Condition > 0
                                     && item1.Prefab.Identifier.Equals(it1.Prefab.Identifier)
                                     && item1.ParentInventory != heldItem.OwnInventory
                                     && !heldItems.IsAnyOwnerOf(item1)
                                     && !item1.IsLimbSlotItem(Character.Controlled));
                        foreach (Item refillItem in refillItems)
                        {
                            if (diff < 1)
                                break;
                            if (heldItem.OwnInventory.TryPutItem(refillItem, slotIndex, false, true,
                                    Character.Controlled))
                                diff -= 1;
                        }
                    }
                }
            }
        }
    }

    
    static List<Item> FindAllCompatWithPreference(this Inventory container, Item heldItem, ItemPrefab? prefab = null, int slotIndex = -1, Func<Item, bool>? predicate = null)
    {
        List<Item> compat = new();
        List<Item> pref = new();
        
        List<Item> iterList = new();
        iterList.BuildIterList(container.AllItemsMod, true, item =>
            item.Condition > 0f
            && (predicate?.Invoke(item) ?? true)
        );
        foreach (Item item in iterList)
        {
            if (!item.CompatibleWithInv(heldItem, slotIndex))
                continue;
            if (prefab is null || item.Prefab.Identifier.Equals(prefab.Identifier))
                pref.Add(item);
            else
                compat.Add(item);
        }
        return pref.Any() ? pref : compat;
    }


    static Item? FindCompatWithPreference(this Inventory container, Item heldItem, ItemPrefab? prefab = null, int slotIndex = -1, Func<Item, bool>? predicate = null)
    {
        Item? foundItem = null;
        List<Item> iterList = new();
        iterList.BuildIterList(container.AllItemsMod, true, item =>
            item.Condition > 0f
            && (predicate?.Invoke(item) ?? true)
            );
        foreach (Item item in iterList)
        {
            DebugConsole.LogError($"FindCompat: {item.Name}");
            if (!item.CompatibleWithInv(heldItem, slotIndex))
                continue;
            if (prefab is null || item.Prefab.Identifier.Equals(prefab.Identifier)) 
                return item;
            foundItem = item;
        }
        return foundItem;
    }

    static List<Item> BuildIterList(this List<Item> list, IEnumerable<Item?> sourceList, bool recursive = true, Func<Item, bool>? predicate = null)
    {
        foreach (Item? item in sourceList)
        {
            if (item is null)
                continue;
            if (predicate is null || predicate(item))
                list.Add(item);
            if (recursive && item.OwnInventory?.Capacity > 0)
                list.BuildIterList(item.OwnInventory.AllItemsMod, true, predicate);
        }
        return list;
    }

    static bool IsAnyOwnerOf(this IEnumerable<Item> owners, Item? item)
    {
        if (item is null)
            return false;
        foreach (Item owner in owners)
        {
            if (
                owner is { OwnInventory: { } ownInventory }
                && item.ParentInventory is { } parentInventory
                && ownInventory.Equals(parentInventory))
                return true;
        }

        return false;
    }
}