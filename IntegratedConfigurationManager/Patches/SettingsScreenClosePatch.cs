using System;
using System.Reflection;
using EFT.UI.Settings;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace IntegratedConfigurationManager.Patches
{
    public class SettingsScreenClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(SettingsScreen), nameof(SettingsScreen.Close), Type.EmptyTypes);
        }

        [PatchPostfix]
        private static void Postfix()
        {
            PluginsTab.OnScreenClosed();
        }
    }
}
