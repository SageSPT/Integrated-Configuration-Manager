using BepInEx.Configuration;
using ConfigurationManager;
using EFT.UI;
using TMPro;
using UnityEngine;

namespace IntegratedConfigurationManager.Rows
{
    internal class ToggleRow : SettingRow
    {
        private readonly UpdatableToggle _toggle;

        public ToggleRow(GameObject root, SettingEntryBase setting, ConfigEntryBase entry)
            : base(root, setting, entry)
        {
            _toggle = root.GetComponent<UpdatableToggle>();
            TextMeshProUGUI label = root.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            label.text = setting.DispName.TrimStart('!');

            Refresh();
            _toggle.Bind(OnToggled);
            ApplyCommon();
        }

        public override void Refresh()
        {
            object value = DisplayValue();
            _toggle.UpdateValue(value is bool && (bool)value, false);
        }

        private void OnToggled(bool value)
        {
            SetValue(value);
            if (IsReadOnly)
            {
                Refresh();
            }
        }
    }
}
