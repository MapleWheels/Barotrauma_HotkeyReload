using System;
using System.Collections;
using System.Collections.Generic;
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
    private static readonly InvSlotType[] ExclusionItemSlotPositions = 
    {
        InvSlotType.Bag,
        InvSlotType.Card,
        InvSlotType.Head,
        InvSlotType.Headset,
        InvSlotType.InnerClothes,
        InvSlotType.OuterClothes
    };
    
    public static void ReloadHeldItems()
    {
        //?? check if in-game && single player or host in multiplayer and we're not switching maps
        if (!Util.CheckIfValidToInteract())
            return;

        //Check if inventory available
        if (Character.Controlled is null || Character.Controlled.Inventory is null)
            return;

        var charInv = Character.Controlled.Inventory;

        foreach (Item heldItem in Character.Controlled.HeldItems
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
                                ) is { } it )
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
    static bool CompatibleWithInv(this Item item, Item containerParent, int slotIndex) => containerParent.GetComponent<ItemContainer>()?.CanBeContained(item, slotIndex) ?? false;
    static bool CompatibleWithInv(this Item item, Item containerParent) => containerParent.GetComponent<ItemContainer>()?.CanBeContained(item) ?? false;

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

    static bool IsLimbSlotItem(this Item item, Character character)
    {
        foreach (InvSlotType slotType in ExclusionItemSlotPositions)
        {
            if (character.Inventory.GetItemInLimbSlot(slotType) is { } it && item == it)
                return true;
        }
        return false;
    }
}