using System.Reflection;
using System.Runtime.CompilerServices;
using Barotrauma;
using Barotrauma.LuaCs;
using Barotrauma.LuaCs.Data;
using Barotrauma.LuaCs.Events;
using HarmonyLib;
using HarmonyLib.Public.Patching;
using Microsoft.Xna.Framework.Input;

[assembly: IgnoresAccessChecksTo("Barotrauma")]
[assembly: IgnoresAccessChecksTo("BarotraumaCore")]

namespace HotkeyReload;

public sealed class Bootloader : IAssemblyPlugin, IEventUpdate
{
    internal ISettingControl
        KeybindReload,
        KeybindQuickLootAll,
        KeybindQuickStackToPlayer,
        KeybindQuickStackToStorage;

    static Bootloader()
    {
        Util.RegisterCompatibilityRule("plasmacutter",
            (heldItem, storableItem) => storableItem.HasTag("oxygensource"));
        Util.RegisterCompatibilityRule("weldingtool",
            (_, storableItem) => storableItem.HasTag("weldingtoolfuel"));
        Util.RegisterCompatibilityRule("flamer",
            (heldItem, storableItem) => storableItem.HasTag("weldingtoolfuel"));
    }
    
    //Injected
    public IConfigService ConfigService { get; set; }
    public IEventService EventService { get; set; }

    private static readonly ContentPackage SelfPackage = ContentPackageManager.RegularPackages
            .First(p => p.Name.ToLowerInvariant().Trim().StartsWith("hotkeyreload"));
    
    private void RegisterConfig()
    {
        ConfigService.TryGetConfig(SelfPackage, nameof(KeybindReload), out KeybindReload);
        ConfigService.TryGetConfig(SelfPackage, nameof(KeybindQuickLootAll), out KeybindQuickLootAll);
        ConfigService.TryGetConfig(SelfPackage, nameof(KeybindQuickStackToPlayer), out KeybindQuickStackToPlayer);
        ConfigService.TryGetConfig(SelfPackage,nameof(KeybindReload), out KeybindQuickStackToStorage);
    }

    private void RegisterPatches()
    {
        /*PatchManager.RegisterPatches(new List<PatchManager.PatchData>()
        {
            new PatchManager.PatchData(
                AccessTools.Method(typeof(Barotrauma.LuaCsSetup), "Update"),
                null,
                new HarmonyMethod(AccessTools.Method(typeof(P_LuaCsSetup_Update), "Postfix"))
                )
        });*/
        EventService.Subscribe<IEventUpdate>(this);
    }
    
    

    public void Initialize()
    {
    }

    public void OnLoadCompleted()
    {
        RegisterConfig();
        RegisterPatches();
    }

    public void PreInitPatching()
    {
    }

    public void Dispose()
    {
    }

    public void OnUpdate(double fixedDeltaTime)
    {
        if (this.KeybindReload?.IsHit() ?? false)
            Reloader.ReloadHeldItems();
        
        if (this.KeybindQuickLootAll?.IsHit() ?? false)
            QuickActions.QuickLootAllToPlayerInventory();
        
        if (this.KeybindQuickStackToPlayer?.IsHit() ?? false)
            QuickActions.QuickStackToPlayerInventory();
        
        if (this.KeybindQuickStackToStorage?.IsHit() ?? false)
            QuickActions.QuickStackToStorageInventory();
    }
}