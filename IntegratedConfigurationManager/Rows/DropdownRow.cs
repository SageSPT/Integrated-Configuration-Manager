using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using ConfigurationManager;
using EFT.UI;
using TMPro;
using UnityEngine;

namespace IntegratedConfigurationManager.Rows
{
    internal class DropdownRow : SettingRow
    {
        private readonly DropDownBox _dropdown;
        private readonly List<object> _values = new List<object>();
        private Action _unsubscribe;

        public DropdownRow(GameObject root, SettingEntryBase setting, ConfigEntryBase entry)
            : base(root, setting, entry)
        {
            root.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = setting.DispName.TrimStart('!');
            _dropdown = root.GetComponentInChildren<DropDownBox>(true);

            if (setting.AcceptableValues != null)
            {
                foreach (object value in setting.AcceptableValues)
                {
                    _values.Add(value);
                }
            }
            else
            {
                foreach (object value in Enum.GetValues(setting.SettingType))
                {
                    _values.Add(value);
                }
            }

            List<string> labels = new List<string>();
            foreach (object value in _values)
            {
                labels.Add(ConfigSource.ValueLabel(value));
            }

            _dropdown.Show(labels);
            _unsubscribe = _dropdown.OnValueChanged.Subscribe(OnPicked);
            Refresh();
            ApplyCommon();

            if (IsReadOnly)
            {
                _dropdown.Interactable = false;
            }
        }

        public override void Refresh()
        {
            object current = DisplayValue();
            int index = -1;
            for (int i = 0; i < _values.Count; i++)
            {
                if (Equals(_values[i], current))
                {
                    index = i;
                    break;
                }
            }

            if (index >= 0)
            {
                _dropdown.UpdateValue(index, false);
            }
            else
            {
                _dropdown.SetLabelText(ConfigSource.ValueLabel(current));
            }
        }

        private void OnPicked(int index)
        {
            if (index < 0 || index >= _values.Count)
            {
                return;
            }
            SetValue(_values[index]);
        }

        public override void Dispose()
        {
            if (_unsubscribe != null)
            {
                _unsubscribe();
                _unsubscribe = null;
            }
            _dropdown.Close();
        }
    }
}
