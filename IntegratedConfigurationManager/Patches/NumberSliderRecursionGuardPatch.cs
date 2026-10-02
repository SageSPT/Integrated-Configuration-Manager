using System;
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace IntegratedConfigurationManager.Patches
{
    public class NumberSliderRecursionGuardPatch : ModulePatch
    {
        private const int MaxDepth = 8;

        private static int _depth;
        private static bool _logged;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(NumberSlider), nameof(NumberSlider.SetCurrentValue));
        }

        [PatchPrefix]
        private static bool Prefix(NumberSlider __instance, float value)
        {
            _depth++;
            if (_depth <= MaxDepth)
            {
                return true;
            }

            if (!_logged)
            {
                _logged = true;
                Plugin.Log.LogError("NumberSlider.SetCurrentValue looped on '" + __instance.name + "' (value " + value + "), stopped it. call chain:\n" + Environment.StackTrace);
            }
            return false;
        }

        [PatchFinalizer]
        private static Exception Finalizer(Exception __exception)
        {
            _depth--;
            return __exception;
        }
    }
}
