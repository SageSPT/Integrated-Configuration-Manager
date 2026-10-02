using System.Reflection;
using EFT.UI.Settings;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace IntegratedConfigurationManager.Patches
{
    public class SettingsScreenAwakePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(SettingsScreen), nameof(SettingsScreen.Awake));
        }

        [PatchPostfix]
        private static void Postfix(SettingsScreen __instance)
        {
            PluginsTab.RegisterIfBuilt(__instance);
        }
    }
}
