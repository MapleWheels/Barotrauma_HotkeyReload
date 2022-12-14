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
        if (!Util.CheckIfValidToInteract() || !Util.CheckIfCharacterReady(Character.Controlled))
            return;
        
        //Check if player is currently interacting with another inventory and it's valid for this action.
        if (Character.Controlled.SelectedItem is null
            || Character.Controlled.SelectedItem.OwnInventory is null
            || Character.Controlled.SelectedItem.OwnInventory.Capacity < 1)
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
        throw new NotImplementedException();
    }

    /// <summary>
    /// Tries to refill the stacks of all items in the player's inventory using the items in the storage
    /// inventory the player is interacting with.
    /// </summary>
    public static void QuickStackToPlayerInventory()
    {
        throw new NotImplementedException();
    }    
    
}