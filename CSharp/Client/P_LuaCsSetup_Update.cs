using System;
using Microsoft.Xna.Framework;
using Barotrauma;
using HarmonyLib;
using Microsoft.Xna.Framework.Input;

namespace HotkeyReload;

[HarmonyPatch(typeof(Barotrauma.LuaCsSetup), "Update")]
public class P_LuaCsSetup_Update
{
    static void Postfix()
    {
        if (!Barotrauma.PlayerInput.KeyHit(Keys.R))
            return;
        Reloader.ReloadHeldItems();
    }
}