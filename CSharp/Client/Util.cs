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
// ReSharper disable MemberCanBePrivate.Global

namespace HotkeyReload;

internal static class Util
{
    internal static bool CheckIfValidToInteract()
    {
        return GameMain.GameSession is not null
               && GameMain.GameSession.IsRunning
               && Screen.Selected is not (null or SubEditorScreen)
               && !Screen.Selected.IsEditor
               && !Submarine.Unloading
               && GUI.KeyboardDispatcher is not null
               && GUI.KeyboardDispatcher.Subscriber is null;
    }

    internal static bool CheckIfCharacterReady(Character? character)
    {
        //Check if character, inventory available
        return character is not null 
               && character.Inventory is not null 
               && !character.IsDead; //Is spectating?
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
}