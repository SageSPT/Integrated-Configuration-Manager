using System;
using System.Collections.Generic;
using System.Reflection;
using EFT.UI;
using EFT.UI.Settings;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IntegratedConfigurationManager
{
    internal static class PluginsTab
    {
        public const SettingsScreen.ESettingsGroup Group = (SettingsScreen.ESettingsGroup)5;

        private const string Caption = "PLUGINS";
        private const string PickerLabel = "Plugin";

        private static readonly FieldInfo TabsField = AccessTools.Field(typeof(SettingsScreen), "_tabs");

        private static SettingsScreen _builtFor;
        private static PluginsSettingsTab _tab;
        private static UIAnimatedToggleSpawner _button;

        public static PluginsSettingsTab Tab
        {
            get { return _tab; }
        }

        public static void RegisterIfBuilt(SettingsScreen screen)
        {
            if (_builtFor == screen && _tab != null && _button != null)
            {
                Register();
            }
        }

        public static void Build(SettingsScreen screen)
        {
            if (_builtFor == screen && _tab != null && _button != null)
            {
                Register();
                return;
            }

            try
            {
                BuildTab(screen);
                BuildButton(screen);
                _builtFor = screen;
                Register();
                Plugin.Log.LogInfo("plugins tab added to settings screen");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("couldn't build the plugins tab, settings will work without it: " + ex);
                if (_tab != null)
                {
                    Object.Destroy(_tab.gameObject);
                }
                if (_button != null)
                {
                    Object.Destroy(_button.gameObject);
                }
                _tab = null;
                _button = null;
                _builtFor = null;
            }
        }

        private static void BuildTab(SettingsScreen screen)
        {
            GameObject source = screen._gameSettingsScreen.gameObject;
            Transform tabParent = source.transform.parent;

            GameObject holder = new GameObject("IntegratedConfigurationManager Holder");
            holder.SetActive(false);
            holder.transform.SetParent(tabParent, false);

            GameObject root = Object.Instantiate(source, holder.transform, false);
            root.name = "Plugins Settings";

            Object.DestroyImmediate(root.GetComponent<GameSettingsTab>());

            Transform topPanel = root.transform.Find("Image/Nickname Panel");
            Transform content = root.transform.Find("Image/Scroll View/Viewport/Other Settings");
            if (topPanel == null || content == null)
            {
                Object.Destroy(holder);
                throw new Exception("Game tab layout changed, didn't find Image/Nickname Panel or the scroll content");
            }

            DestroyChildren(topPanel);
            DestroyChildren(content);

            ShrinkTopPanel(root.transform, topPanel);
            DropDownBox picker = BuildPickerRow(source.transform, topPanel);

            GameObject templateHolder = new GameObject("Templates", typeof(RectTransform));
            templateHolder.SetActive(false);
            templateHolder.transform.SetParent(root.transform, false);
            TMP_Text tabLabel = screen._gameButton.SpawnableToggle._headerLabel;
            RowTemplates templates = RowTemplates.Create(source.transform, screen._controlsSettingsTabScreen, tabLabel, templateHolder.transform);

            ScrollRect scroll = root.transform.Find("Image/Scroll View").GetComponent<ScrollRect>();

            _tab = root.AddComponent<PluginsSettingsTab>();
            _tab.Init(picker, (RectTransform)content, scroll, templates);

            root.SetActive(false);
            root.transform.SetParent(tabParent, false);
            root.transform.SetSiblingIndex(screen._controlsSettingsTabScreen.transform.GetSiblingIndex() + 1);

            Object.Destroy(holder);
        }

        private static void ShrinkTopPanel(Transform root, Transform topPanel)
        {
            RectTransform panel = (RectTransform)topPanel;
            RectTransform scrollView = (RectTransform)root.Find("Image/Scroll View");

            float newHeight = 16f + 38f + 11f;
            float freed = panel.sizeDelta.y - newHeight;

            panel.sizeDelta = new Vector2(panel.sizeDelta.x, newHeight);

            scrollView.anchoredPosition = new Vector2(scrollView.anchoredPosition.x, scrollView.anchoredPosition.y + freed);
            scrollView.sizeDelta = new Vector2(scrollView.sizeDelta.x, scrollView.sizeDelta.y + freed);
        }

        private static DropDownBox BuildPickerRow(Transform gameTab, Transform topPanel)
        {
            Transform template = gameTab.Find("Image/Scroll View/Viewport/Other Settings/Common/InterfaceLanguage");
            if (template == null)
            {
                throw new Exception("didn't find the InterfaceLanguage row on the Game tab");
            }

            GameObject row = Object.Instantiate(template.gameObject, topPanel, false);
            row.name = "PluginPicker";

            LocalizedText[] localized = row.GetComponentsInChildren<LocalizedText>(true);
            foreach (LocalizedText text in localized)
            {
                Object.DestroyImmediate(text);
            }

            HoverTrigger[] triggers = row.GetComponentsInChildren<HoverTrigger>(true);
            foreach (HoverTrigger trigger in triggers)
            {
                Object.DestroyImmediate(trigger);
            }

            TextMeshProUGUI label = row.transform.Find("Text").GetComponent<TextMeshProUGUI>();
            label.text = PickerLabel;

            RectTransform rect = (RectTransform)row.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(327f, -16f);

            DropDownBox picker = row.GetComponentInChildren<DropDownBox>(true);
            if (picker == null)
            {
                throw new Exception("InterfaceLanguage row has no DropDownBox anymore");
            }
            return picker;
        }

        private static void BuildButton(SettingsScreen screen)
        {
            UIAnimatedToggleSpawner source = screen._controlsButton;
            Transform buttonRow = source.transform.parent;

            GameObject holder = new GameObject("IntegratedConfigurationManager Holder");
            holder.SetActive(false);
            holder.transform.SetParent(buttonRow, false);

            GameObject clone = Object.Instantiate(source.gameObject, holder.transform, false);
            clone.name = "PluginsToggleSpawner";
            UIAnimatedToggleSpawner button = clone.GetComponent<UIAnimatedToggleSpawner>();

            if (button._preservedChildren != null && button._preservedChildren.Count > 0)
            {
                Plugin.Log.LogWarning("controls tab button has preserved children, leaving them alone");
            }
            else
            {
                DestroyChildren(clone.transform);
            }

            button._headerCaption = Caption;

            clone.transform.SetParent(buttonRow, false);
            clone.transform.SetAsLastSibling();
            Object.Destroy(holder);
            _button = button;

            AnimatedToggle toggle = button.SpawnedObject;
            toggle.onValueChanged.AddListener(delegate (bool isOn)
            {
                if (isOn)
                {
                    screen.ShowScreen(Group);
                }
            });
        }

        private static void Register()
        {
            Dictionary<SettingsScreen.ESettingsGroup, SettingsScreen.SettingsGroupObjects> tabs =
                (Dictionary<SettingsScreen.ESettingsGroup, SettingsScreen.SettingsGroupObjects>)TabsField.GetValue(null);
            tabs[Group] = new SettingsScreen.SettingsGroupObjects(_tab, _button);

            _builtFor.Add(_tab);
        }

        public static void OnScreenClosed()
        {
            if (_tab != null && _tab.gameObject.activeSelf)
            {
                _tab.Close();
            }
        }

        private static void DestroyChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }
    }
}
