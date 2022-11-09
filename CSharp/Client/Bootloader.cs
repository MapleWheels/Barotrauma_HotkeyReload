using Barotrauma;
using Microsoft.Xna.Framework.Input;

namespace HotkeyReload;

sealed class Bootloader : ACsMod
{
    private readonly string PatchId_GameMain_Update = "com.tbn.patch.GameMain.Update";
    
    public Bootloader() : base()
    {
        InitClient();
    }

    void InitClient()
    {
        LuaCsHook.instance.Patch(
            PatchId_GameMain_Update,
            nameof(Barotrauma.GameMain),
            nameof(GameMain.Update),
            ((instance, _) => this.Update()),
            LuaCsHook.HookMethodType.After
        );
    }

    void Update()
    {
        if (Barotrauma.PlayerInput.KeyHit(Keys.R))
            Reloader.ReloadHeldItems();
    }

    public override void Stop()
    {
        LuaCsHook.instance.RemovePatch(PatchId_GameMain_Update,
            nameof(Barotrauma.GameMain),
            nameof(GameMain.Update),
            LuaCsHook.HookMethodType.After);
    }
}