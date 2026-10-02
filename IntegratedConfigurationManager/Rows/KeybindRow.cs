using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using ConfigurationManager;
using EFT;
using EFT.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IntegratedConfigurationManager.Rows
{
    internal class KeybindRow : SettingRow
    {
        private static KeyCode[] _keysToCheck;

        private readonly MonoBehaviour _runner;
        private readonly Button _button;
        private readonly TMP_Text _text;
        private readonly Image _background;
        private readonly Color _setColor;
        private readonly Color _emptyColor;
        private readonly bool _shortcut;

        private Coroutine _capture;
        private bool _hadCmOverride;
        private int _captureEndedFrame = -10;

        public KeybindRow(GameObject root, SettingEntryBase setting, ConfigEntryBase entry, MonoBehaviour runner, Color setColor, Color emptyColor)
            : base(root, setting, entry)
        {
            root.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = setting.DispName.TrimStart('!');
            _runner = runner;
            _button = root.transform.Find("Key").GetComponent<Button>();
            _background = _button.GetComponent<Image>();
            _text = _button.GetComponentInChildren<TMP_Text>(true);
            _setColor = setColor;
            _emptyColor = emptyColor;
            _shortcut = setting.SettingType == typeof(KeyboardShortcut);

            _button.onClick.AddListener(StartCapture);
            Refresh();
            ApplyCommon();

            if (IsReadOnly)
            {
                _button.interactable = false;
            }
        }

        public bool IsCapturing
        {
            get { return _capture != null; }
        }

        public bool ConsumeEscape()
        {
            return IsCapturing || Time.frameCount - _captureEndedFrame <= 1;
        }

        public override void Refresh()
        {
            if (IsCapturing)
            {
                return;
            }

            object value = DisplayValue();
            List<KeyCode> keys = new List<KeyCode>();
            if (value is KeyboardShortcut)
            {
                KeyboardShortcut shortcut = (KeyboardShortcut)value;
                if (shortcut.MainKey != KeyCode.None)
                {
                    keys.Add(shortcut.MainKey);
                    foreach (KeyCode modifier in shortcut.Modifiers)
                    {
                        keys.Add(modifier);
                    }
                }
            }
            else if (value is KeyCode && (KeyCode)value != KeyCode.None)
            {
                keys.Add((KeyCode)value);
            }

            if (keys.Count == 0)
            {
                _text.text = "Settings/NotSet".Localized();
                _background.color = _emptyColor;
            }
            else
            {
                _text.text = Describe(keys);
                _background.color = _setColor;
            }
        }

        private static string Describe(List<KeyCode> keys)
        {
            List<string> parts = new List<string>();
            foreach (KeyCode key in keys)
            {
                parts.Add("\"" + KeyTools.GetKeyNameAlias(key) + "\"");
            }
            return string.Join(" + ", parts.ToArray());
        }

        private void StartCapture()
        {
            if (IsReadOnly || IsCapturing)
            {
                return;
            }

            ConfigurationManager.ConfigurationManager cm = ConfigSource.GetCm();
            if (cm != null)
            {
                _hadCmOverride = cm.OverrideHotkey;
                cm.OverrideHotkey = true;
            }

            _background.color = _setColor;
            _text.text = "Press any key...".Localized();
            _capture = _runner.StartCoroutine(Capture());
        }

        private IEnumerator Capture()
        {
            if (_keysToCheck == null)
            {
                List<KeyCode> keys = new List<KeyCode>();
                foreach (KeyCode key in UnityInput.Current.SupportedKeyCodes)
                {
                    if (key != KeyCode.Mouse0 && key != KeyCode.None)
                    {
                        keys.Add(key);
                    }
                }
                _keysToCheck = keys.ToArray();
            }

            yield return null;

            while (true)
            {
                if (UnityInput.Current.GetKeyDown(KeyCode.Escape))
                {
                    FinishCapture();
                    yield break;
                }

                List<KeyCode> held = new List<KeyCode>();
                foreach (KeyCode key in _keysToCheck)
                {
                    if (UnityInput.Current.GetKeyUp(key))
                    {
                        List<KeyCode> modifiers = new List<KeyCode>();
                        foreach (KeyCode other in _keysToCheck)
                        {
                            if (other != key && UnityInput.Current.GetKey(other))
                            {
                                modifiers.Add(other);
                            }
                        }
                        if (_shortcut)
                        {
                            SetValue(new KeyboardShortcut(key, modifiers.ToArray()));
                        }
                        else
                        {
                            SetValue(key);
                        }
                        FinishCapture();
                        yield break;
                    }
                    if (UnityInput.Current.GetKey(key))
                    {
                        held.Add(key);
                    }
                }

                if (held.Count > 0)
                {
                    _text.text = Describe(held) + " + ...";
                }
                yield return null;
            }
        }

        private void StopCapture()
        {
            if (_capture != null && _runner != null)
            {
                _runner.StopCoroutine(_capture);
            }
            FinishCapture();
        }

        private void FinishCapture()
        {
            _capture = null;
            _captureEndedFrame = Time.frameCount;

            ConfigurationManager.ConfigurationManager cm = ConfigSource.GetCm();
            if (cm != null)
            {
                cm.OverrideHotkey = _hadCmOverride;
            }
            Refresh();
        }

        public void CancelCapture()
        {
            if (IsCapturing)
            {
                StopCapture();
            }
        }

        public override void Dispose()
        {
            _button.onClick.RemoveListener(StartCapture);
            if (IsCapturing)
            {
                StopCapture();
            }
        }
    }
}
