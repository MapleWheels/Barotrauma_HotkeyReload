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

public static class Util
{
    public static bool CheckIfValidToInteract()
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
}