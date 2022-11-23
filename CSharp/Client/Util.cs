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
// ReSharper disable MemberCanBePrivate.Global

namespace HotkeyReload;

internal static class Util
{
    internal static bool CheckIfValidToInteract()
    {
        if ( GameMain.GameSession is null 
             || !GameMain.GameSession.IsRunning
             || Screen.Selected is null or SubEditorScreen
             || Screen.Selected.IsEditor
             || Submarine.Unloading
             || GUI.KeyboardDispatcher is not null)
            return false;
        return true;
    }

    internal static bool CheckIfCharacterReady(Character? character)
    {
        //Check if character, inventory available
        if (character is null 
            || character.Inventory is null
            || character.IsDead) //Is spectating?
            return false;
        return true;
    }

    private static readonly InvSlotType[] ExclusionItemSlotPositions = 
    {
        InvSlotType.Bag,
        InvSlotType.Card,
        InvSlotType.Head,
        InvSlotType.Headset,
        InvSlotType.InnerClothes,
        InvSlotType.OuterClothes
    };

    internal static int GetSlotMaxStackSize(this Item containerItem, Item storableItem, int slotIndex) =>
        containerItem.GetComponent<ItemContainer>() is { } container 
        && container.CanBeContained(storableItem, slotIndex)
            ? Math.Min(container.MaxStackSize, storableItem.Prefab.MaxStackSize)
            : 0;

    #warning TODO: Implement character slot limit finder.
    internal static int GetSlotMaxStackSize(this Character targetCharacter, Item storableItem, int slotIndex)
    {
        throw new NotImplementedException();
    }

    internal static bool CompatibleWithInv(this Item item, Inventory container, int slotIndex = -1) => container.Owner switch
    {
        Item ownerItem when slotIndex == -1 => CompatibleWithInv(item, ownerItem),
        Item ownerItem => CompatibleWithInv(item, ownerItem, slotIndex),
        Character character when slotIndex == -1 => CompatibleWithInv(item, character),
        Character character => CompatibleWithInv(item, character, slotIndex),
        _ => false
    };

    internal static bool CompatibleWithInv(this Item item, Character character) => character.Inventory.CanBePut(item);
    internal static bool CompatibleWithInv(this Item item, Character character, int slotIndex) => character.Inventory.CanBePutInSlot(item, slotIndex);
    internal static bool CompatibleWithInv(this Item item, Item containerParent, int slotIndex) => containerParent.GetComponent<ItemContainer>()?.CanBeContained(item, slotIndex) ?? false;
    internal static bool CompatibleWithInv(this Item item, Item containerParent) => containerParent.GetComponent<ItemContainer>()?.CanBeContained(item) ?? false;
    
    
    internal static bool IsLimbSlotItem(this Item item, Character character)
    {
        foreach (InvSlotType slotType in ExclusionItemSlotPositions)
        {
            if (character.Inventory.GetItemInLimbSlot(slotType) is { } it && item == it)
                return true;
        }
        return false;
    }
    
    internal static List<Item> FindAllCompatWithPreference(this CharacterInventory container, Item targetContainer, 
        ItemPrefab? prefab = null, int slotIndex = -1, Func<Item, bool>? predicate = null)
    {
        List<Item> compat = new();
        List<Item> pref = new();
        List<Item> iterList = new();
        
        //So the bag seems to be the last thing that's searched for items when we want it to be the first.
        //Let's do a custom list order.
        foreach (InvSlotType slotType in LimbsSearchOrder)
        {
            iterList.AddAllItemsInLimbSlot(container, slotType, true, predicate);
        }

        foreach (Item item in iterList)
        {
            if (!item.CompatibleWithInv(targetContainer, slotIndex))
                continue;
            if (prefab is null || item.Prefab.Identifier.Equals(prefab.Identifier))
                pref.Add(item);
            else
                compat.Add(item);
        }
        return pref.Any() ? pref : compat;
    }

    internal static List<Item> FindAllCompatWithPreference(this Inventory container, Item targetContainer,
        ItemPrefab? prefab = null, int slotIndex = -1, Func<Item, bool>? predicate = null)
    {
        List<Item> compat = new();
        List<Item> pref = new();
        List<Item> iterList = new();

        iterList.BuildIterList(container.AllItemsMod, true, predicate); //No preference, just make the list

        foreach (Item item in iterList)
        {
            if (!item.CompatibleWithInv(targetContainer, slotIndex))
                continue;
            if (prefab is null || item.Prefab.Identifier.Equals(prefab.Identifier))
                pref.Add(item);
            else
                compat.Add(item);
        }
        return pref.Any() ? pref : compat;
    }
    
    internal static List<Item> FindAllCompatWithPreference(this Inventory container, Character targetCharacter,
        ItemPrefab? prefab = null, int slotIndex = -1, Func<Item, bool>? predicate = null)
    {
        List<Item> compat = new();
        List<Item> pref = new();
        List<Item> iterList = new();

        iterList.BuildIterList(container.AllItemsMod, true, predicate); //No preference, just make the list

        foreach (Item item in iterList)
        {
            if (!item.CompatibleWithInv(targetCharacter, slotIndex))
                continue;
            if (prefab is null || item.Prefab.Identifier.Equals(prefab.Identifier))
                pref.Add(item);
            else
                compat.Add(item);
        }
        return pref.Any() ? pref : compat;
    }

    internal static Item? FindCompatWithPreference(this CharacterInventory container, Item targetContainer, 
        ItemPrefab? prefab = null, int slotIndex = -1, Func<Item, bool>? predicate = null)
    {
        Item? foundItem = null;
        List<Item> iterList = new();

        //So the bag seems to be the last thing that's searched for items when we want it to be the first.
        //Let's do a custom list order.
        foreach (InvSlotType slotType in LimbsSearchOrder)
        {
            iterList.AddAllItemsInLimbSlot(container, slotType, true, predicate);
        }
        
        foreach (Item item in iterList)
        {
            if (!item.CompatibleWithInv(targetContainer, slotIndex))
                continue;
            if (prefab is null || item.Prefab.Identifier.Equals(prefab.Identifier)) 
                return item;
            foundItem = item;
        }
        return foundItem;
    }

    internal static Item? FindCompatWithPreference(this Inventory container, Item targetContainer, 
        ItemPrefab? prefab = null, int slotIndex = -1, Func<Item, bool>? predicate = null)
    {
        Item? foundItem = null;
        List<Item> iterList = new();

        iterList.BuildIterList(container.AllItemsMod, true, predicate);
        
        foreach (Item item in iterList)
        {
            if (!item.CompatibleWithInv(targetContainer, slotIndex))
                continue;
            if (prefab is null || item.Prefab.Identifier.Equals(prefab.Identifier)) 
                return item;
            foundItem = item;
        }
        return foundItem;
    }
    
    internal static Item? FindCompatWithPreference(this Inventory container, Character targetCharacter, 
        ItemPrefab? prefab = null, int slotIndex = -1, Func<Item, bool>? predicate = null)
    {
        Item? foundItem = null;
        List<Item> iterList = new();

        iterList.BuildIterList(container.AllItemsMod, true, predicate);
        
        foreach (Item item in iterList)
        {
            if (!item.CompatibleWithInv(targetCharacter, slotIndex))
                continue;
            if (prefab is null || item.Prefab.Identifier.Equals(prefab.Identifier)) 
                return item;
            foundItem = item;
        }
        return foundItem;
    }
    
    internal static List<Item> BuildIterList(this List<Item> list, IEnumerable<Item?> sourceList, bool recursive = true, 
        Func<Item, bool>? predicate = null, int nestLimit = 10, int currentNestLim = 0)
    {
        if (currentNestLim > nestLimit)
            return list;
        currentNestLim++;
        foreach (Item? item in sourceList)
        {
            if (item is null)
                continue;
            if (predicate is null || predicate(item))
                list.Add(item);
            if (recursive && item.OwnInventory?.Capacity > 0)
                list.BuildIterList(item.OwnInventory.AllItemsMod, true, predicate, nestLimit, currentNestLim);
        }
        return list;
    }

    internal static List<Item> AddAllItemsInLimbSlot(this List<Item> items, CharacterInventory inventory, InvSlotType type, bool recursive = false, Func<Item, bool>? predicate = null)
    {
        for (int index = 0; index < inventory.SlotTypes.Length; index++)
        {
            if (inventory.SlotTypes[index] == type && inventory.slots[index].FirstOrDefault() is { } item)
            {
                if (predicate is null || predicate.Invoke(item))
                    items.Add(item);
                if (recursive && item.OwnInventory is { Capacity: > 0 } itemInv)
                    items.BuildIterList(itemInv.AllItemsMod, true, predicate);
            }
        }

        return items;
    }

    
    internal static void RefillItemStacksUsingContainer(this Character target, Inventory sourceContainer,
        bool recursive = true)
    {
        if (target.Inventory is null
            || target.Inventory.Locked
            || sourceContainer.Locked
            || sourceContainer.Capacity < 1
            || !sourceContainer.AllItemsMod.Any())
            return;

        CharacterInventory targetContainer = target.Inventory;

        for (int index = 0; index < targetContainer.Capacity; index++)
        {
            //try to fill nested inventory items first
            if (recursive)
            {
                foreach (Item item in targetContainer.GetItemsAt(index).ToImmutableList())
                {
                    if (item is { OwnInventory.Capacity: > 0})
                        item.RefillItemStacksUsingContainer(sourceContainer, recursive);
                }
            }
            //fill self
            if (targetContainer.GetItemAt(index) is { } item1)
            {
                int diff = target.GetSlotMaxStackSize(item1, index) - targetContainer.GetItemsAt(index).Count();
                if (diff > 0)
                {
                    List<Item> refillItems = sourceContainer.FindAllCompatWithPreference(target, item1.Prefab, index,
                        item2 => item2.Prefab.Identifier.Equals(item1.Prefab.Identifier));
                    foreach (Item refillItem in refillItems)
                    {
                        if (targetContainer.TryPutItem(refillItem, index, false, true, Character.Controlled))
                            diff--;
                        else
                            break;  //ain't working out or max stack(?)
                        if (diff < 1)
                            break;
                    }
                }
            }
        }
    }

    internal static void RefillItemStacksUsingContainer(this Item target, Inventory sourceContainer, bool recursive = true)
    {
        if (target.OwnInventory is null
            || target.OwnInventory.Locked
            || sourceContainer.Locked
            || sourceContainer.Capacity < 1
            || !sourceContainer.AllItemsMod.Any())
            return;
        
        Inventory targetContainer = target.OwnInventory;
        
        for (int index = 0; index < targetContainer.Capacity; index++)
        {
            //try to fill nested inventory items first
            if (recursive)
            {
                foreach (Item item in targetContainer.GetItemsAt(index).ToImmutableList())
                {
                    if (item is { OwnInventory.Capacity: > 0})
                        item.RefillItemStacksUsingContainer(sourceContainer, recursive);
                }
            }
            //fill self
            if (targetContainer.GetItemAt(index) is { } item1)
            {
                int diff = target.GetSlotMaxStackSize(item1, index) - targetContainer.GetItemsAt(index).Count();
                if (diff > 0)
                {
                    List<Item> refillItems = sourceContainer.FindAllCompatWithPreference(target, item1.Prefab, index,
                        item2 => item2.Prefab.Identifier.Equals(item1.Prefab.Identifier));
                    foreach (Item refillItem in refillItems)
                    {
                        if (targetContainer.TryPutItem(refillItem, index, false, true, Character.Controlled))
                            diff--;
                        else
                            break;  //ain't working out or max stack(?)
                        if (diff < 1)
                            break;
                    }
                }
            }
        }
    }
    
    

    internal static readonly Barotrauma.InvSlotType[] LimbsSearchOrder = 
    {
        InvSlotType.Bag,
        InvSlotType.InnerClothes,
        InvSlotType.OuterClothes,
        InvSlotType.Head,
        InvSlotType.Headset,
        InvSlotType.Card,
        InvSlotType.None
    };
}