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

public static class QuickActions
{
    private static readonly InvSlotType[] InvPriorityOrder =
    {
        InvSlotType.LeftHand, InvSlotType.RightHand,
        InvSlotType.Bag, InvSlotType.InnerClothes,
        InvSlotType.OuterClothes, InvSlotType.Head,
        InvSlotType.None
    };
    
    /// <summary>
    /// Tries to put all items of an inventory a player is interacting with into the player's inventory.
    /// Prioritizes any storages the player is holding in their hands, then most of their limb slots, then their hot bar.
    /// </summary>
    public static void QuickLootAllToPlayerInventory()
    {
        if (!IsReadyToExchangeItems(Character.Controlled))
            return;

        CharacterInventory charInv = Character.Controlled.Inventory;
        
        //SelectedItem Inv
        Item selectedItem = Character.Controlled.SelectedItem;
        
        //Is the selected item in their hands?
        if (selectedItem == charInv.GetItemInLimbSlot(InvSlotType.LeftHand)
            || selectedItem == charInv.GetItemInLimbSlot(InvSlotType.RightHand))
            return;

        foreach (InvSlotType slotType in InvPriorityOrder)
        {
            foreach (Item item in selectedItem.OwnInventory.AllItemsMod)
            {
                charInv.TryPutItem(item, Character.Controlled, new List<InvSlotType> { slotType });
            }
        }
    }
    
    /// <summary>
    /// Tries to refill the stacks of all items of the storage inventory a player is interacting with using
    /// items in the player's inventory.
    /// </summary>
    public static void QuickStackToStorageInventory()
    {
        if (!IsReadyToExchangeItems(Character.Controlled))
            return;
        Character.Controlled.SelectedItem.RefillItemStacksUsingContainer(Character.Controlled.Inventory, true);
    }

    /// <summary>
    /// Tries to refill the stacks of all items in the player's inventory using the items in the storage
    /// inventory the player is interacting with.
    /// </summary>
    public static void QuickStackToPlayerInventory()
    {
        if (!IsReadyToExchangeItems(Character.Controlled))
            return;

        Item selectedItem = Character.Controlled.SelectedItem;
        CharacterInventory charInv = Character.Controlled.Inventory;
        
        for (int index = 0; index < charInv.slots.Length; index++)
        {
            if (charInv.slots[index].FirstOrDefault() is { } item)
            {
                
            }
        }
    }

    private static bool IsReadyToExchangeItems(Character character)
    {
        return Util.CheckIfValidToInteract()
               && Util.CheckIfCharacterReady(character)
               && character.SelectedItem is { OwnInventory.Capacity: > 0 };
    }
    
}