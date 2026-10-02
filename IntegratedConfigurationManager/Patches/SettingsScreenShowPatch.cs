using System;
using System.Reflection;
using EFT.UI.Settings;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace IntegratedConfigurationManager.Patches
{
    public class SettingsScreenShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(SettingsScreen), nameof(SettingsScreen.Show), new Type[] { typeof(SettingsScreen.SettingsScreenController) });
        }

        [PatchPrefix]
        private static void Prefix(SettingsScreen __instance)
        {
            PendingEdits.Clear();
            PluginsTab.RegisterIfBuilt(__instance);
        }

        [PatchPostfix]
        private static void Postfix(SettingsScreen __instance)
        {
            PluginsTab.Build(__instance);
        }
    }
}
