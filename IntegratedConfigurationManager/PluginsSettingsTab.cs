using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BepInEx.Configuration;
using ConfigurationManager;
using EFT.InputSystem;
using EFT.Settings;
using EFT.UI;
using EFT.UI.Settings;
using IntegratedConfigurationManager.Rows;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IntegratedConfigurationManager
{
    public class PluginsSettingsTab : SettingsTab
    {
        private static string _lastPluginGuid;

        private DropDownBox _picker;
        private Action _unsubscribePicker;
        private RectTransform _content;
        private ScrollRect _scroll;
        private RowTemplates _templates;

        private List<PluginEntry> _plugins = new List<PluginEntry>();
        private PluginEntry _shown;

        private readonly List<SettingRow> _rows = new List<SettingRow>();
        private readonly Dictionary<ConfigEntryBase, SettingRow> _rowsByEntry = new Dictionary<ConfigEntryBase, SettingRow>();
        private readonly List<ConfigFile> _watchedFiles = new List<ConfigFile>();

        private ConfigurationManager.ConfigurationManager _cm;

        internal void Init(DropDownBox picker, RectTransform content, ScrollRect scroll, RowTemplates templates)
        {
            _picker = picker;
            _content = content;
            _scroll = scroll;
            _templates = templates;
            _unsubscribePicker = _picker.OnValueChanged.Subscribe(OnPluginPicked);

            _cm = ConfigSource.GetCm();
            if (_cm != null)
            {
                _cm.DisplayingWindowChanged += OnCmWindowChanged;
            }
        }

        public override void OnTabSelected()
        {
            RefreshPicker();
        }

        private void RefreshPicker()
        {
            try
            {
                _plugins = ConfigSource.Collect();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("couldn't read the plugin list from Configuration Manager: " + ex);
                _plugins = new List<PluginEntry>();
            }

            List<string> names = new List<string>();
            int selected = 0;
            for (int i = 0; i < _plugins.Count; i++)
            {
                names.Add(_plugins[i].DisplayName);
                if (_plugins[i].Info.GUID == _lastPluginGuid)
                {
                    selected = i;
                }
            }

            _picker.Show(names);
            if (_plugins.Count == 0)
            {
                _picker.SetLabelText(string.Empty);
                ClearRows();
                return;
            }
            _picker.UpdateValue(selected, false);
            ShowPlugin(_plugins[selected]);
        }

        private void OnPluginPicked(int index)
        {
            if (index < 0 || index >= _plugins.Count)
            {
                return;
            }
            ShowPlugin(_plugins[index]);
        }

        private void ShowPlugin(PluginEntry plugin)
        {
            _lastPluginGuid = plugin.Info.GUID;
            _shown = plugin;
            ClearRows();

            bool showHeaders = plugin.Categories.Count > 1 || !ConfigSource.HideSingleSections();
            int skipped = 0;

            Transform lastSection = null;
            foreach (CategoryEntry category in plugin.Categories)
            {
                Transform section = Object.Instantiate(_templates.Section, _content, false).transform;
                lastSection = section;

                if (showHeaders && !string.IsNullOrEmpty(category.Name))
                {
                    GameObject header = Object.Instantiate(_templates.Header, section, false);
                    header.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = category.Name;
                }

                foreach (SettingEntryBase setting in category.Settings)
                {
                    SettingRow row;
                    int childrenBefore = section.childCount;
                    try
                    {
                        row = CreateRow(setting, section);
                    }
                    catch (Exception ex)
                    {
                        string details = ex.ToString();
                        if (details.Length > 6000)
                        {
                            details = details.Substring(0, 6000) + "\n...";
                        }
                        Plugin.Log.LogError("couldn't draw " + plugin.Info.GUID + " [" + setting.Category + "] " + setting.DispName + ": " + details);
                        row = null;

                        for (int i = section.childCount - 1; i >= childrenBefore; i--)
                        {
                            GameObject broken = section.GetChild(i).gameObject;
                            broken.SetActive(false);
                            Object.Destroy(broken);
                        }
                    }
                    if (row == null)
                    {
                        skipped++;
                        continue;
                    }
                    _rows.Add(row);
                    if (row.Entry != null)
                    {
                        _rowsByEntry[row.Entry] = row;
                        WatchFile(row.Entry.ConfigFile);
                    }
                }

            }
            if (lastSection != null)
            {
                lastSection.Find("Separator").gameObject.SetActive(false);
            }

            if (skipped > 0)
            {
                Plugin.Log.LogWarning(plugin.DisplayName + ": " + skipped + " settings couldn't be drawn, they're still editable in F12");
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            _scroll.verticalNormalizedPosition = 1f;
        }

        private SettingRow CreateRow(SettingEntryBase setting, Transform section)
        {
            ConfigEntryBase entry = ConfigSource.GetEntry(setting);
            Type type = setting.SettingType;

            if (setting.CustomDrawer != null || setting.CustomHotkeyDrawer != null)
            {
                return ReadOnly(setting, entry, section);
            }

            if (setting.ShowRangeAsPercent.HasValue && setting.AcceptableValueRange.Key != null)
            {
                SliderRow slider = new SliderRow(Object.Instantiate(_templates.Slider, section, false), setting, entry);
                _children.Add(slider.Slider);
                return slider;
            }

            if (setting.AcceptableValues != null)
            {
                return new DropdownRow(Object.Instantiate(_templates.Dropdown, section, false), setting, entry);
            }

            if (type == typeof(bool))
            {
                return new ToggleRow(Object.Instantiate(_templates.Toggle, section, false), setting, entry);
            }
            if (type == typeof(KeyboardShortcut) || type == typeof(KeyCode))
            {
                return new KeybindRow(Object.Instantiate(_templates.Keybind, section, false), setting, entry, this, _templates.KeySetColor, _templates.KeyEmptyColor);
            }
            if (type == typeof(Color) || type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) || type == typeof(Quaternion))
            {
                return ReadOnly(setting, entry, section);
            }

            if (type.IsEnum)
            {
                if (type.IsDefined(typeof(FlagsAttribute), false))
                {
                    return ReadOnly(setting, entry, section);
                }
                return new DropdownRow(Object.Instantiate(_templates.Dropdown, section, false), setting, entry);
            }

            if (setting.ObjToStr != null && setting.StrToObj != null)
            {
                return new TextRow(Object.Instantiate(_templates.Text, section, false), setting, entry);
            }

            return ReadOnly(setting, entry, section);
        }

        private SettingRow ReadOnly(SettingEntryBase setting, ConfigEntryBase entry, Transform section)
        {
            return new ReadOnlyRow(Object.Instantiate(_templates.Text, section, false), setting, entry);
        }

        private void WatchFile(ConfigFile file)
        {
            if (file == null || _watchedFiles.Contains(file))
            {
                return;
            }
            file.SettingChanged += OnSettingChanged;
            file.ConfigReloaded += OnConfigReloaded;
            _watchedFiles.Add(file);
        }

        private void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            SettingRow row;
            if (!_rowsByEntry.TryGetValue(e.ChangedSetting, out row))
            {
                return;
            }
            object pending;
            if (PendingEdits.TryGet(row.Entry, out pending))
            {
                return;
            }
            row.Refresh();
        }

        private void OnConfigReloaded(object sender, EventArgs e)
        {
            foreach (SettingRow row in _rows)
            {
                object pending;
                if (row.Entry == null || !PendingEdits.TryGet(row.Entry, out pending))
                {
                    row.Refresh();
                }
            }
        }

        private void OnCmWindowChanged(object sender, ValueChangedEventArgs<bool> e)
        {
            if (!e.NewValue && isActiveAndEnabled)
            {
                RefreshPicker();
            }
        }

        private void ClearRows()
        {
            foreach (ConfigFile file in _watchedFiles)
            {
                file.SettingChanged -= OnSettingChanged;
                file.ConfigReloaded -= OnConfigReloaded;
            }
            _watchedFiles.Clear();

            foreach (SettingRow row in _rows)
            {
                SliderRow slider = row as SliderRow;
                if (slider != null)
                {
                    _children.Remove(slider.Slider);
                }
                try
                {
                    row.Dispose();
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogDebug("row cleanup: " + ex.Message);
                }
            }
            _rows.Clear();
            _rowsByEntry.Clear();

            if (_content == null)
            {
                return;
            }

            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                GameObject child = _content.GetChild(i).gameObject;
                child.SetActive(false);
                Object.Destroy(child);
            }
        }

        public override ETranslateResult TranslateCommand(ECommand command)
        {
            if (command.IsCommand(ECommand.Escape))
            {
                foreach (SettingRow row in _rows)
                {
                    TextRow text = row as TextRow;
                    if (text != null && text.ConsumeEscape())
                    {
                        return ETranslateResult.BlockAll;
                    }
                    KeybindRow keybind = row as KeybindRow;
                    if (keybind != null && keybind.ConsumeEscape())
                    {
                        return ETranslateResult.BlockAll;
                    }
                }
            }
            return base.TranslateCommand(command);
        }

        public override Task TakeSettingsFrom(SettingsManager settingsManager)
        {
            if (_shown == null)
            {
                return Task.CompletedTask;
            }
            foreach (CategoryEntry category in _shown.Categories)
            {
                foreach (SettingEntryBase setting in category.Settings)
                {
                    if (setting.ReadOnly == true || setting.HideDefaultButton)
                    {
                        continue;
                    }
                    if (setting.DefaultValue == null && !setting.SettingType.IsClass)
                    {
                        continue;
                    }
                    ConfigEntryBase entry = ConfigSource.GetEntry(setting);
                    if (entry != null)
                    {
                        PendingEdits.Set(entry, setting.DefaultValue);
                    }
                }
            }
            return Task.CompletedTask;
        }

        public override void Close()
        {
            if (_picker != null)
            {
                _picker.Close();
            }
            ClearRows();
            base.Close();
        }

        private void OnDisable()
        {
            foreach (SettingRow row in _rows)
            {
                KeybindRow keybind = row as KeybindRow;
                if (keybind != null)
                {
                    keybind.CancelCapture();
                }
            }
        }

        private void OnDestroy()
        {
            ClearRows();
            if (_unsubscribePicker != null)
            {
                _unsubscribePicker();
                _unsubscribePicker = null;
            }
            if (_cm != null)
            {
                _cm.DisplayingWindowChanged -= OnCmWindowChanged;
            }
        }
    }
}
