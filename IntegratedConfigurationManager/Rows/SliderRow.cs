using System;
using System.Globalization;
using BepInEx.Configuration;
using ConfigurationManager;
using EFT.UI;
using TMPro;
using UnityEngine;

namespace IntegratedConfigurationManager.Rows
{
    internal class SliderRow : SettingRow
    {
        public const string PercentFormat = "0'%'";

        private readonly NumberSlider _slider;
        private readonly double _min;
        private readonly double _max;
        private readonly bool _percent;
        private readonly bool _whole;
        private readonly int _decimals;

        public NumberSlider Slider
        {
            get { return _slider; }
        }

        public SliderRow(GameObject root, SettingEntryBase setting, ConfigEntryBase entry)
            : base(root, setting, entry)
        {
            root.transform.Find("Caption").GetComponent<TextMeshProUGUI>().text = setting.DispName.TrimStart('!');
            _slider = root.GetComponentInChildren<NumberSlider>(true);

            _min = Convert.ToDouble(setting.AcceptableValueRange.Key, CultureInfo.InvariantCulture);
            _max = Convert.ToDouble(setting.AcceptableValueRange.Value, CultureInfo.InvariantCulture);
            _percent = setting.ShowRangeAsPercent == true && _max > _min;
            _whole = IsWholeNumber(setting.SettingType);
            _decimals = _whole ? 0 : CountDecimals(setting);

            float shownMin = _percent ? 0f : (float)_min;
            float shownMax = _percent ? 100f : (float)_max;
            SetBoundsSafely(shownMin, shownMax);
            _slider.Show(shownMin, shownMax, _percent ? PercentFormat : (_whole ? "0" : "F" + _decimals));

            _slider.Bind(OnSlid);
            Refresh();
            ApplyCommon();
        }

        public override void Refresh()
        {
            Refreshing = true;
            try
            {
                double value = Convert.ToDouble(DisplayValue(), CultureInfo.InvariantCulture);
                if (_percent)
                {
                    float percent = Mathf.Round((float)(100.0 * Math.Abs(value - _min) / Math.Abs(_max - _min)));
                    _slider.UpdateValue(percent, false, 0f, 100f);
                }
                else
                {
                    _slider.UpdateValue((float)value, false, (float)_min, (float)_max);
                }
            }
            finally
            {
                Refreshing = false;
            }
        }

        private void OnSlid(float shown)
        {
            if (Refreshing)
            {
                return;
            }

            double value = _percent ? _min + shown / 100.0 * (_max - _min) : shown;
            value = Math.Round(value, _whole ? 0 : Math.Max(_decimals, _percent ? 4 : 0));
            if (value < _min)
            {
                value = _min;
            }
            if (value > _max)
            {
                value = _max;
            }

            try
            {
                SetValue(Convert.ChangeType(value, Setting.SettingType, CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("couldn't turn " + value + " into " + Setting.SettingType.Name + " for " + Setting.DispName + ": " + ex.Message);
                Refresh();
            }
        }

        private void SetBoundsSafely(float min, float max)
        {
            UnityEngine.UI.Slider unitySlider = _slider._slider;
            if (max > unitySlider.maxValue)
            {
                unitySlider.maxValue = max;
                unitySlider.minValue = min;
            }
            else
            {
                unitySlider.minValue = min;
                unitySlider.maxValue = max;
            }
        }

        private static bool IsWholeNumber(Type type)
        {
            return type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte)
                || type == typeof(sbyte) || type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort);
        }

        private static int CountDecimals(SettingEntryBase setting)
        {
            int most = 1;
            most = Math.Max(most, DecimalsOf(setting.AcceptableValueRange.Key));
            most = Math.Max(most, DecimalsOf(setting.AcceptableValueRange.Value));
            most = Math.Max(most, DecimalsOf(setting.DefaultValue));
            return Math.Min(most, 3);
        }

        private static int DecimalsOf(object value)
        {
            if (value == null)
            {
                return 0;
            }
            string text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (text.IndexOf('E') >= 0 || text.IndexOf('e') >= 0)
            {
                return 3;
            }
            int dot = text.IndexOf('.');
            if (dot < 0)
            {
                return 0;
            }
            return text.Length - dot - 1;
        }
    }
}
