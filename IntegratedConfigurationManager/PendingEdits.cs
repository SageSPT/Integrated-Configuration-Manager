using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace IntegratedConfigurationManager
{
    internal static class PendingEdits
    {
        private static readonly Dictionary<ConfigEntryBase, object> Values = new Dictionary<ConfigEntryBase, object>();

        public static int Count
        {
            get { return Values.Count; }
        }

        public static bool TryGet(ConfigEntryBase entry, out object value)
        {
            return Values.TryGetValue(entry, out value);
        }

        public static void Set(ConfigEntryBase entry, object value)
        {
            if (Equals(entry.BoxedValue, value))
            {
                Values.Remove(entry);
            }
            else
            {
                Values[entry] = value;
            }
        }

        public static void Clear()
        {
            Values.Clear();
        }

        public static void ApplyAll()
        {
            if (Values.Count == 0)
            {
                return;
            }

            List<KeyValuePair<ConfigEntryBase, object>> edits = new List<KeyValuePair<ConfigEntryBase, object>>(Values);
            Values.Clear();

            List<ConfigFile> needSave = new List<ConfigFile>();
            foreach (KeyValuePair<ConfigEntryBase, object> edit in edits)
            {
                ConfigEntryBase entry = edit.Key;
                try
                {
                    entry.BoxedValue = edit.Value;
                    if (!entry.ConfigFile.SaveOnConfigSet && !needSave.Contains(entry.ConfigFile))
                    {
                        needSave.Add(entry.ConfigFile);
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError("couldn't save [" + entry.Definition.Section + "] " + entry.Definition.Key + " in " + entry.ConfigFile.ConfigFilePath + ": " + ex.Message);
                }
            }

            foreach (ConfigFile file in needSave)
            {
                try
                {
                    file.Save();
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError("couldn't write " + file.ConfigFilePath + ": " + ex.Message);
                }
            }
        }
    }
}
