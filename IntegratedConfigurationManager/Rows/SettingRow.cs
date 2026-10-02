using BepInEx.Configuration;
using ConfigurationManager;
using EFT.UI;
using UnityEngine;

namespace IntegratedConfigurationManager.Rows
{
    internal abstract class SettingRow
    {
        public readonly GameObject Root;
        public readonly SettingEntryBase Setting;
        public readonly ConfigEntryBase Entry;

        protected SettingRow(GameObject root, SettingEntryBase setting, ConfigEntryBase entry)
        {
            Root = root;
            Setting = setting;
            Entry = entry;
        }

        protected bool Refreshing;

        protected bool IsReadOnly
        {
            get { return Setting.ReadOnly == true; }
        }

        public virtual void Dispose()
        {
        }

        protected object DisplayValue()
        {
            object pending;
            if (Entry != null && PendingEdits.TryGet(Entry, out pending))
            {
                return pending;
            }
            return Setting.Get();
        }

        protected void SetValue(object value)
        {
            if (Entry == null || IsReadOnly)
            {
                return;
            }
            PendingEdits.Set(Entry, value);
        }

        public abstract void Refresh();

        protected void ApplyCommon(bool lockRow = false, string extraTooltip = null)
        {
            string text = Setting.Description;
            if (!string.IsNullOrEmpty(extraTooltip))
            {
                text = string.IsNullOrEmpty(text) ? extraTooltip : text + "\n\n" + extraTooltip;
            }

            if (!string.IsNullOrEmpty(text))
            {
                HoverTooltipArea tooltip = Root.AddComponent<HoverTooltipArea>();
                tooltip.SetMessageText(text, true);
            }

            if (IsReadOnly || lockRow)
            {
                CanvasGroup group = Root.GetComponent<CanvasGroup>();
                if (group == null)
                {
                    group = Root.AddComponent<CanvasGroup>();
                }
                group.SetUnlockStatus(false, false);
            }
        }
    }
}
