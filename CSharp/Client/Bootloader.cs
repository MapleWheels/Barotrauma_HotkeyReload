using System.Reflection;
using Barotrauma;
using HarmonyLib;
using Microsoft.Xna.Framework.Input;

namespace HotkeyReload;

sealed class Bootloader : ACsMod
{
    static readonly string _patchId = "com.tbn.hotkeyreload";
    private Harmony _instance;
    
    public Bootloader()
    {
        PatchHarmony();
    }

    public void PatchHarmony()
    {
        //manual patch because automatic patcher fails spectacularly and inconsistently
        _instance = new Harmony(_patchId);
        MethodInfo? mi = typeof(Barotrauma.LuaCsSetup).GetMethod("Update", BindingFlags.Instance | BindingFlags.Public);
        MethodInfo? patched = typeof(P_LuaCsSetup_Update).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic);
        _instance.Patch(mi, null, new HarmonyMethod(patched));
    }

    public override void Stop()
    {
        _instance.UnpatchAll();
    }
}