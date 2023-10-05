using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MajesticButton.Utils;

public class Functions
{
    private static void InstantiateButton(string url, string text, int index, GameObject originalButton, Vector3 originalPos, int spacing, GameObject sourceButton, ButtonSfx sourceButtonSfx, ButtonTextColor sourceButtonTextColor, Image sourceImage)
    {
        var newButton = GameObject.Instantiate(originalButton, MajesticButtonPlugin.buttonParent.transform);

        UpdateButtonImage(originalButton, sourceImage);
        AddLayoutElement(originalButton);
        IncreaseButtonHeight(originalButton);
        AddContentSizeFitter(originalButton);

        AddClickEvent(newButton, url);
        AddLayoutElement(newButton);
        UpdateButtonText(newButton, sourceButton, text);
        AddComponentButtonSfx(newButton, sourceButtonSfx);
        AddComponentButtonTextColor(newButton, sourceButtonTextColor);
        UpdateButtonImage(newButton, sourceImage);
        IncreaseButtonHeight(newButton);
        AddContentSizeFitter(newButton);


        MajesticButtonPlugin.clonedButtons.Add(newButton);
    }


    private static void AddClickEvent(GameObject button, string url)
    {
        button.GetComponent<Button>().onClick = new Button.ButtonClickedEvent(); // Have to fully replace the event, otherwise it keeps the persistent one from the cloned button. onClick.RemoveAllListeners() won't do it.
        button.GetComponent<Button>().onClick.AddListener(() => { Application.OpenURL(url); });
    }

    private static void AddLayoutElement(GameObject button)
    {
        if (button.GetComponent<LayoutElement>() == null)
        {
            var layoutElement = button.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = button.GetComponent<RectTransform>().sizeDelta.x;
            layoutElement.preferredHeight = button.GetComponent<RectTransform>().sizeDelta.y;
        }
    }

    private static void UpdateButtonText(GameObject button, GameObject sourceButton, string text)
    {
        var textComponent = button.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = text;
            textComponent.outlineColor = sourceButton.GetComponentInChildren<TextMeshProUGUI>().outlineColor;
            textComponent.outlineWidth = 0.175f; // 0.175f is the default value for the outline width.
        }
    }

    private static void AddComponentButtonSfx(GameObject button, ButtonSfx source)
    {
        if (button.GetComponent<ButtonSfx>() == null)
        {
            var newComponent = button.AddComponent<ButtonSfx>();
            if (source != null && newComponent != null)
            {
                newComponent.m_sfxPrefab = source.m_sfxPrefab;
                newComponent.m_selectable = source.m_selectable;
                newComponent.m_selectSfxPrefab = source.m_selectSfxPrefab;
            }
        }
    }

    private static void AddComponentButtonTextColor(GameObject button, ButtonTextColor source)
    {
        if (button.GetComponent<ButtonTextColor>() == null)
        {
            var newComponent = button.AddComponent<ButtonTextColor>();
            if (source != null && newComponent != null)
            {
                newComponent.m_button = newComponent.GetComponent<Button>();
                newComponent.m_text = newComponent.GetComponentInChildren<TextMeshProUGUI>();
                newComponent.m_defaultColor = new Color(1f, 161 / 255f, 60 / 255f, 1f);

                newComponent.m_text.color = new Color(1f, 161 / 255f, 60 / 255f, 1f);

                newComponent.m_textMesh = source.m_textMesh;
                newComponent.m_disabledColor = source.m_disabledColor;
                newComponent.m_defaultMeshColor = source.m_defaultMeshColor;
            }
        }
    }

    private static void UpdateButtonImage(GameObject button, Image source)
    {
        var imageComponent = button.GetComponent<Image>();
        if (source != null && imageComponent != null)
        {
            imageComponent.sprite = source.sprite;
        }
    }

    private static void IncreaseButtonHeight(GameObject button)
    {
        var rectTransform = button.GetComponent<RectTransform>();
        var size = rectTransform.sizeDelta;
        size.y *= 1.5f; // Increase the height by 50%
        rectTransform.sizeDelta = size;
    }

    private static void AddContentSizeFitter(GameObject button)
    {
        if (button.GetComponent<ContentSizeFitter>() == null)
        {
            var contentSizeFitter = button.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    internal static void UpdateButtons()
    {
        var urls = MajesticButtonPlugin.ButtonUrls.Value.Split(',');
        var texts = MajesticButtonPlugin.ButtonTexts.Value.Split(',');
        var originalPos = MajesticButtonPlugin.originalButton.transform.localPosition;
        var spacing = 150;

        var sourceButton = GameObject.Find("GuiRoot/GUI/StartGui/CharacterSelection/SelectCharacter/BottomWindow/Start");
        var sourceImage = sourceButton?.GetComponent<Image>();
        var sourceButtonSfx = sourceButton?.GetComponent<ButtonSfx>();
        var sourceButtonTextColor = sourceButton?.GetComponent<ButtonTextColor>();

        if (urls.Length != texts.Length)
        {
            MajesticButtonPlugin.MajesticButtonLogger.LogError("URLs and texts arrays have different lengths.");
            return;
        }

        for (int i = 0; i < urls.Length; ++i)
        {
            if (i >= urls.Length || string.IsNullOrEmpty(urls[i]))
            {
                MajesticButtonPlugin.MajesticButtonLogger.LogWarning($"No URL found for button index {i}, skipping button creation.");
                continue;
            }

            InstantiateButton(urls[i], texts[i], i, MajesticButtonPlugin.originalButton, originalPos, spacing, sourceButton, sourceButtonSfx, sourceButtonTextColor, sourceImage);
        }
    }
}