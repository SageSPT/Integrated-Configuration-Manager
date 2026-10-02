using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using IntegratedConfigurationManager.Patches;

namespace IntegratedConfigurationManager
{
    [BepInPlugin("com.sage.integratedconfigurationmanager", "Integrated Configuration Manager", "1.0.0")]
    [BepInDependency("com.bepis.bepinex.configurationmanager", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;

            if (!Chainloader.PluginInfos.ContainsKey(ConfigSource.CmGuid))
            {
                Log.LogWarning("Configuration Manager isn't loaded, so there's no PLUGINS tab this session");
                return;
            }

            new SettingsScreenAwakePatch().Enable();
            new SettingsScreenShowPatch().Enable();
            new SettingsScreenClosePatch().Enable();
            new HasSameSettingsPatch().Enable();
            new SaveSettingsPatch().Enable();
            new ApplyOldSettingsPatch().Enable();
            new NumberSliderPercentPatch().Enable();
            new NumberSliderRecursionGuardPatch().Enable();
        }
    }
}
