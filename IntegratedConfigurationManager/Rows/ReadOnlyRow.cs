using System;
using System.Globalization;
using BepInEx.Configuration;
using ConfigurationManager;
using TMPro;
using UnityEngine;

namespace IntegratedConfigurationManager.Rows
{
    internal class ReadOnlyRow : SettingRow
    {
        private readonly TMP_InputField _input;

        public ReadOnlyRow(GameObject root, SettingEntryBase setting, ConfigEntryBase entry)
            : base(root, setting, entry)
        {
            root.transform.Find("Caption").GetComponent<TextMeshProUGUI>().text = setting.DispName.TrimStart('!');
            _input = root.GetComponentInChildren<TMP_InputField>(true);
            _input.readOnly = true;
            _input.characterLimit = 0;
            _input.characterValidation = TMP_InputField.CharacterValidation.None;

            Refresh();
            ApplyCommon(true, EditInCmHint());
        }

        public override void Refresh()
        {
            _input.SetTextWithoutNotify(Describe(DisplayValue()));
        }

        private static string EditInCmHint()
        {
            string key = "F12";
            try
            {
                ConfigurationManager.ConfigurationManager cm = ConfigSource.GetCm();
                ConfigEntry<KeyboardShortcut> hotkey;
                if (cm != null && cm.Config.TryGetEntry<KeyboardShortcut>("General", "Show config manager", out hotkey))
                {
                    key = hotkey.Value.ToString();
                }
            }
            catch (Exception)
            {
            }
            return "Change this one in Configuration Manager (" + key + ").";
        }

        private string Describe(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }
            if (value is Color)
            {
                return "#" + ColorUtility.ToHtmlStringRGBA((Color)value);
            }
            if (value is Vector2)
            {
                Vector2 v = (Vector2)value;
                return Join(v.x, v.y);
            }
            if (value is Vector3)
            {
                Vector3 v = (Vector3)value;
                return Join(v.x, v.y, v.z);
            }
            if (value is Vector4)
            {
                Vector4 v = (Vector4)value;
                return Join(v.x, v.y, v.z, v.w);
            }
            if (value is Quaternion)
            {
                Quaternion q = (Quaternion)value;
                return Join(q.x, q.y, q.z, q.w);
            }
            if (value is Enum)
            {
                return value.ToString();
            }
            if (Setting.ObjToStr != null)
            {
                return Setting.ObjToStr(value);
            }
            return value.ToString();
        }

        private static string Join(params float[] parts)
        {
            string[] texts = new string[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                texts[i] = parts[i].ToString("0.###", CultureInfo.InvariantCulture);
            }
            return string.Join(", ", texts);
        }
    }
}
