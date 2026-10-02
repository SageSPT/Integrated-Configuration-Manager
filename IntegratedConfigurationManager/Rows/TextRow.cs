using System;
using BepInEx.Configuration;
using ConfigurationManager;
using TMPro;
using UnityEngine;

namespace IntegratedConfigurationManager.Rows
{
    internal class TextRow : SettingRow
    {
        private readonly TMP_InputField _input;

        private int _editEndedFrame = -10;

        public TextRow(GameObject root, SettingEntryBase setting, ConfigEntryBase entry)
            : base(root, setting, entry)
        {
            root.transform.Find("Caption").GetComponent<TextMeshProUGUI>().text = setting.DispName.TrimStart('!');
            _input = root.GetComponentInChildren<TMP_InputField>(true);

            _input.readOnly = IsReadOnly;
            _input.characterLimit = 0;
            _input.characterValidation = ValidationFor(setting.SettingType);

            _input.onEndEdit.AddListener(OnEndEdit);
            Refresh();
            ApplyCommon();
        }

        public override void Refresh()
        {
            _input.SetTextWithoutNotify(ToText(DisplayValue()));
        }

        public bool ConsumeEscape()
        {
            if (_input.isFocused)
            {
                Refresh();
                _input.DeactivateInputField();
                return true;
            }
            return Time.frameCount - _editEndedFrame <= 1;
        }

        private void OnEndEdit(string text)
        {
            _editEndedFrame = Time.frameCount;
            if (!IsReadOnly && Setting.StrToObj != null)
            {
                try
                {
                    SetValue(Setting.StrToObj(text));
                }
                catch (Exception)
                {
                }
            }
            Refresh();
        }

        private string ToText(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }
            if (Setting.ObjToStr != null)
            {
                return Setting.ObjToStr(value);
            }
            return value.ToString();
        }

        private static TMP_InputField.CharacterValidation ValidationFor(Type type)
        {
            if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(sbyte))
            {
                return TMP_InputField.CharacterValidation.Integer;
            }
            if (type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) || type == typeof(byte))
            {
                return TMP_InputField.CharacterValidation.Digit;
            }
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            {
                return TMP_InputField.CharacterValidation.Decimal;
            }
            return TMP_InputField.CharacterValidation.None;
        }

        public override void Dispose()
        {
            _input.onEndEdit.RemoveListener(OnEndEdit);
        }
    }
}
