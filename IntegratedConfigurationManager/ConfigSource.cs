using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using ConfigurationManager;
using UnityEngine;

namespace IntegratedConfigurationManager
{
    internal class PluginEntry
    {
        public BepInPlugin Info;
        public string DisplayName;
        public List<CategoryEntry> Categories = new List<CategoryEntry>();
    }

    internal class CategoryEntry
    {
        public string Name;
        public List<SettingEntryBase> Settings = new List<SettingEntryBase>();
    }

    internal static class ConfigSource
    {
        public const string CmGuid = "com.bepis.bepinex.configurationmanager";

        private static MethodInfo _collectSettings;
        private static PropertyInfo _entryProperty;

        public static ConfigEntryBase GetEntry(SettingEntryBase setting)
        {
            if (_entryProperty == null)
            {
                Type type = typeof(SettingEntryBase).Assembly.GetType("ConfigurationManager.ConfigSettingEntry");
                _entryProperty = type.GetProperty("Entry", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }
            if (!_entryProperty.DeclaringType.IsInstanceOfType(setting))
            {
                return null;
            }
            return (ConfigEntryBase)_entryProperty.GetValue(setting, null);
        }

        public static bool HideSingleSections()
        {
            BaseUnityPlugin cm = Chainloader.PluginInfos[CmGuid].Instance;
            ConfigEntry<bool> entry;
            if (cm.Config.TryGetEntry<bool>("General", "Hide single sections", out entry))
            {
                return entry.Value;
            }
            return false;
        }

        private static MethodInfo _toProperCase;

        public static string ValueLabel(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }
            if (value is Enum)
            {
                MemberInfo[] members = value.GetType().GetMember(value.ToString());
                if (members.Length > 0)
                {
                    object[] attributes = members[0].GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);
                    if (attributes.Length > 0)
                    {
                        return ((System.ComponentModel.DescriptionAttribute)attributes[0]).Description;
                    }
                }
                return ToProperCase(value.ToString());
            }
            return value.ToString();
        }

        private static string ToProperCase(string text)
        {
            if (_toProperCase == null)
            {
                Type utils = typeof(SettingEntryBase).Assembly.GetType("ConfigurationManager.Utilities.Utils");
                _toProperCase = utils.GetMethod("ToProperCase", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }
            return (string)_toProperCase.Invoke(null, new object[] { text });
        }

        public static ConfigurationManager.ConfigurationManager GetCm()
        {
            return Chainloader.PluginInfos[CmGuid].Instance as ConfigurationManager.ConfigurationManager;
        }

        public static List<PluginEntry> Collect()
        {
            List<PluginEntry> result = new List<PluginEntry>();

            BaseUnityPlugin cm = Chainloader.PluginInfos[CmGuid].Instance;
            bool showAdvanced = ReadFilter(cm, "Show advanced", false);
            bool showKeybinds = ReadFilter(cm, "Show keybinds", true);
            bool showSettings = ReadFilter(cm, "Show settings", true);

            if (_collectSettings == null)
            {
                Type searcher = typeof(SettingEntryBase).Assembly.GetType("ConfigurationManager.SettingSearcher");
                _collectSettings = searcher.GetMethod("CollectSettings", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }
            object[] args = new object[] { null, null, false };
            _collectSettings.Invoke(null, args);
            IEnumerable<SettingEntryBase> all = (IEnumerable<SettingEntryBase>)args[0];

            Dictionary<BepInPlugin, PluginEntry> byPlugin = new Dictionary<BepInPlugin, PluginEntry>();
            Dictionary<PluginEntry, Dictionary<string, CategoryEntry>> categoriesByName = new Dictionary<PluginEntry, Dictionary<string, CategoryEntry>>();

            foreach (SettingEntryBase setting in all)
            {
                if (!PassesFilter(setting, showAdvanced, showKeybinds, showSettings))
                {
                    continue;
                }

                PluginEntry plugin;
                if (!byPlugin.TryGetValue(setting.PluginInfo, out plugin))
                {
                    plugin = new PluginEntry();
                    plugin.Info = setting.PluginInfo;
                    plugin.DisplayName = plugin.Info.Name.TrimStart('!') + " " + plugin.Info.Version;
                    byPlugin[setting.PluginInfo] = plugin;
                    categoriesByName[plugin] = new Dictionary<string, CategoryEntry>();
                    result.Add(plugin);
                }

                string categoryName = setting.Category ?? string.Empty;
                CategoryEntry category;
                if (!categoriesByName[plugin].TryGetValue(categoryName, out category))
                {
                    category = new CategoryEntry();
                    category.Name = setting.Category;
                    categoriesByName[plugin][categoryName] = category;
                    plugin.Categories.Add(category);
                }
                category.Settings.Add(setting);
            }

            result.Sort(delegate (PluginEntry a, PluginEntry b) { return string.Compare(a.Info.Name, b.Info.Name); });
            foreach (PluginEntry plugin in result)
            {
                foreach (CategoryEntry category in plugin.Categories)
                {
                    category.Settings.Sort(CompareSettings);
                }
            }

            return result;
        }

        private static int CompareSettings(SettingEntryBase a, SettingEntryBase b)
        {
            int byOrder = b.Order.CompareTo(a.Order);
            if (byOrder != 0)
            {
                return byOrder;
            }
            return string.Compare(a.DispName, b.DispName);
        }

        private static bool PassesFilter(SettingEntryBase setting, bool showAdvanced, bool showKeybinds, bool showSettings)
        {
            bool advanced = setting.IsAdvanced == true;
            bool keybind = IsKeyboardShortcut(setting);

            if (!showAdvanced && advanced)
            {
                return false;
            }
            if (!showKeybinds && keybind)
            {
                return false;
            }
            if (!showSettings && !advanced && !keybind)
            {
                return false;
            }
            return true;
        }

        public static bool IsKeyboardShortcut(SettingEntryBase setting)
        {
            return setting.SettingType == typeof(KeyboardShortcut) || setting.SettingType == typeof(KeyCode);
        }

        private static bool ReadFilter(BaseUnityPlugin cm, string key, bool fallback)
        {
            ConfigEntry<bool> entry;
            if (cm.Config.TryGetEntry<bool>("Filtering", key, out entry))
            {
                return entry.Value;
            }
            return fallback;
        }
    }
}
