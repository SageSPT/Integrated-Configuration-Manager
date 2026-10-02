using System.Reflection;
using EFT.UI;
using HarmonyLib;
using IntegratedConfigurationManager.Rows;
using SPT.Reflection.Patching;

namespace IntegratedConfigurationManager.Patches
{
    public class NumberSliderPercentPatch : ModulePatch
    {
        private static readonly AccessTools.FieldRef<NumberSlider, string> Format = AccessTools.FieldRefAccess<NumberSlider, string>("_format");
        private static readonly AccessTools.FieldRef<NumberSlider, string> CachedText = AccessTools.FieldRefAccess<NumberSlider, string>("_cachedOriginalText");

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(NumberSlider), nameof(NumberSlider.SetStringValue));
        }

        [PatchPrefix]
        private static void Prefix(NumberSlider __instance, ref string value)
        {
            if (Format(__instance) != SliderRow.PercentFormat)
            {
                return;
            }
            if (value != null)
            {
                value = value.Replace("%", string.Empty).Trim();
            }
            string cached = CachedText(__instance);
            if (cached != null)
            {
                CachedText(__instance) = cached.Replace("%", string.Empty).Trim();
            }
        }
    }
}
