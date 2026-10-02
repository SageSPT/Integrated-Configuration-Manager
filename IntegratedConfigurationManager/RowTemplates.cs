using System;
using EFT.UI;
using EFT.UI.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IntegratedConfigurationManager
{
    internal class RowTemplates
    {
        private const string Rows = "Image/Scroll View/Viewport/Other Settings/";

        private const float SectionPadding = 327f;
        private const float SliderSectionPadding = 2f;

        public GameObject Section;
        public GameObject Header;
        public GameObject Toggle;
        public GameObject Dropdown;
        public GameObject Slider;
        public GameObject Text;
        public GameObject Keybind;
        public Color KeySetColor;
        public Color KeyEmptyColor;

        private const float HeaderFontSize = 22f;

        public static RowTemplates Create(Transform gameTab, ControlSettingsTab controlsTab, TMP_Text tabLabel, Transform holder)
        {
            RowTemplates templates = new RowTemplates();

            templates.Section = Clone(gameTab, Rows + "Common", holder, "Section");
            for (int i = templates.Section.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = templates.Section.transform.GetChild(i);
                if (child.name != "Separator")
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            templates.Toggle = Clone(gameTab, Rows + "TogglesOneColumn/Clear RAM", holder, "ToggleRow");

            templates.Header = Clone(gameTab, Rows + "Common/InterfaceLanguage", holder, "CategoryHeader");
            Transform headerDropdown = templates.Header.transform.Find("InterfaceLanguageDropdown");
            if (headerDropdown != null)
            {
                Object.DestroyImmediate(headerDropdown.gameObject);
            }
            StyleHeader(templates.Header, templates.Section, templates.Toggle, tabLabel);

            templates.Dropdown = Clone(gameTab, Rows + "Common/InterfaceLanguage", holder, "DropdownRow");
            StripHoverTriggers(templates.Dropdown);

            templates.Slider = Clone(gameTab, Rows + "Scrolls/FOV", holder, "SliderRow");
            FitSliderRow(templates.Slider, templates.Dropdown);

            templates.Text = Clone(gameTab, Rows + "Scrolls/FOV", holder, "TextRow");
            FitSliderRow(templates.Text, templates.Dropdown);
            MakeTextRow(templates.Text, templates.Dropdown);

            MakeKeybindRow(templates, gameTab, controlsTab, holder);

            return templates;
        }

        private static void StyleHeader(GameObject header, GameObject section, GameObject toggleRow, TMP_Text tabLabel)
        {
            TextMeshProUGUI text = header.transform.Find("Text").GetComponent<TextMeshProUGUI>();
            TMP_Text toggleLabel = toggleRow.transform.Find("Label").GetComponent<TMP_Text>();

            text.font = tabLabel.font;
            text.fontSharedMaterial = tabLabel.fontSharedMaterial;
            text.fontStyle = FontStyles.Normal;
            text.fontSize = HeaderFontSize;
            text.color = toggleLabel.color;
            text.alignment = TextAlignmentOptions.Top;
            text.enableWordWrapping = false;

            RectTransform sectionRect = (RectTransform)section.transform;
            float sectionWidth = sectionRect.sizeDelta.x;
            float padding = section.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().padding.left;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(sectionWidth / 2f - padding, 0f);
            rect.sizeDelta = new Vector2(sectionWidth, rect.sizeDelta.y);
        }

        private static void MakeKeybindRow(RowTemplates templates, Transform gameTab, ControlSettingsTab controlsTab, Transform holder)
        {
            CommandKeyPair pair = controlsTab._commandKeyPairTemplate;
            if (pair == null || pair._keyButton == null)
            {
                throw new Exception("Controls tab has no command key pair template");
            }

            templates.Keybind = Clone(gameTab, Rows + "Common/InterfaceLanguage", holder, "KeybindRow");
            RectTransform dropdown = (RectTransform)templates.Keybind.GetComponentInChildren<DropDownBox>(true).transform;

            GameObject cell = Object.Instantiate(pair._keyButton.gameObject, templates.Keybind.transform, false);
            cell.name = "Key";
            RectTransform cellRect = (RectTransform)cell.transform;
            cellRect.anchorMin = dropdown.anchorMin;
            cellRect.anchorMax = dropdown.anchorMax;
            cellRect.pivot = dropdown.pivot;
            cellRect.anchoredPosition = dropdown.anchoredPosition;
            cellRect.sizeDelta = dropdown.sizeDelta;

            Object.DestroyImmediate(dropdown.gameObject);

            templates.KeySetColor = pair._defaultBackgroundColor;
            templates.KeyEmptyColor = pair._resetBackgroundColor;
        }

        private static void FitSliderRow(GameObject row, GameObject dropdownRow)
        {
            float shift = SliderSectionPadding - SectionPadding;
            RectTransform rowRect = (RectTransform)row.transform;
            rowRect.sizeDelta = new Vector2(((RectTransform)dropdownRow.transform).sizeDelta.x, rowRect.sizeDelta.y);
            foreach (Transform child in row.transform)
            {
                RectTransform childRect = (RectTransform)child;
                childRect.anchoredPosition = new Vector2(childRect.anchoredPosition.x + shift, childRect.anchoredPosition.y);
            }
        }

        private static void MakeTextRow(GameObject row, GameObject dropdownRow)
        {
            Transform host = row.transform.Find("FOV");
            Object.DestroyImmediate(host.GetComponent<NumberSlider>());
            Object.DestroyImmediate(host.GetComponent<UnityEngine.UI.Slider>());
            for (int i = host.childCount - 1; i >= 0; i--)
            {
                Transform child = host.GetChild(i);
                if (child.name != "Value Input")
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            RectTransform rowRect = (RectTransform)dropdownRow.transform;
            RectTransform dropdown = (RectTransform)dropdownRow.GetComponentInChildren<DropDownBox>(true).transform;
            float dropdownLeft = rowRect.sizeDelta.x * dropdown.anchorMin.x + dropdown.anchoredPosition.x - dropdown.pivot.x * dropdown.sizeDelta.x;

            RectTransform hostRect = (RectTransform)host;
            RectTransform input = (RectTransform)host.Find("Value Input");
            float inputHeight = input.sizeDelta.y;
            input.anchorMin = new Vector2(0f, 0.5f);
            input.anchorMax = new Vector2(0f, 0.5f);
            input.pivot = new Vector2(0f, 0.5f);
            input.anchoredPosition = new Vector2(dropdownLeft - hostRect.anchoredPosition.x, 0f);
            input.sizeDelta = new Vector2(dropdown.sizeDelta.x, inputHeight);

            TMP_InputField field = input.GetComponent<TMP_InputField>();
            if (field.textComponent != null)
            {
                field.textComponent.enableAutoSizing = false;
                field.textComponent.fontSize = field.textComponent.fontSizeMax;
            }
        }

        public static void StripHoverTriggers(GameObject row)
        {
            HoverTrigger[] triggers = row.GetComponentsInChildren<HoverTrigger>(true);
            foreach (HoverTrigger trigger in triggers)
            {
                Object.DestroyImmediate(trigger);
            }
        }

        private static GameObject Clone(Transform gameTab, string path, Transform holder, string name)
        {
            Transform source = gameTab.Find(path);
            if (source == null)
            {
                throw new Exception("didn't find " + path + " on the Game tab");
            }

            GameObject copy = Object.Instantiate(source.gameObject, holder, false);
            copy.name = name;
            copy.SetActive(true);

            LocalizedText[] localized = copy.GetComponentsInChildren<LocalizedText>(true);
            foreach (LocalizedText text in localized)
            {
                Object.DestroyImmediate(text);
            }
            return copy;
        }
    }
}
