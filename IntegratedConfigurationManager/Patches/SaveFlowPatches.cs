using System.Reflection;
using EFT.Settings;
using EFT.UI.Settings;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace IntegratedConfigurationManager.Patches
{
    public class HasSameSettingsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(SettingsScreen.SettingsScreenController.SettingsGroups), nameof(SettingsScreen.SettingsScreenController.SettingsGroups.HasSameSettings), new[] { typeof(SettingsManager) });
        }

        [PatchPostfix]
        private static void Postfix(ref bool __result)
        {
            if (PendingEdits.Count > 0)
            {
                __result = false;
            }
        }
    }

    public class SaveSettingsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(SettingsScreen.SettingsScreenController), nameof(SettingsScreen.SettingsScreenController.SaveSettings));
        }

        [PatchPrefix]
        private static void Prefix()
        {
            PendingEdits.ApplyAll();
        }
    }

    public class ApplyOldSettingsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(SettingsScreen.SettingsScreenController), nameof(SettingsScreen.SettingsScreenController.ApplyOldSettings));
        }

        [PatchPostfix]
        private static void Postfix()
        {
            PendingEdits.Clear();
        }
    }
}
