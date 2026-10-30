using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Selection is a distinct screen over the interactive 3D preview. Buttons complement
// the assignment's swipe/drag, pinch, hold-and-rotate and double-tap gestures.
public class ItemSelectionScreen : MonoBehaviour
{
    private ItemViewportController viewport;
    private GameObject selection;
    private GameObject changeButton;
    private GameObject joysticks;
    private Text title;
    private Text selectedLabel;
    private Font font;
    private readonly Dictionary<string, Image> cards = new Dictionary<string, Image>();

    private void Start()
    {
        viewport = FindAnyObjectByType<ItemViewportController>();
        if (viewport == null) return;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var canvasObject = new GameObject("Item Selection Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        selection = new GameObject("Selection Screen", typeof(RectTransform));
        selection.transform.SetParent(canvas.transform, false);
        var selectionRect = selection.GetComponent<RectTransform>();
        selectionRect.anchorMin = Vector2.zero;
        selectionRect.anchorMax = Vector2.one;
        selectionRect.offsetMin = selectionRect.offsetMax = Vector2.zero;
        title = Label(selection.transform, "CHOOSE YOUR ITEM", new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(700, 65), 42);
        Label(selection.transform, "Swipe empty space to browse. Drag the model to move it.\nPinch with both fingers to zoom. Hold one finger and move the other to rotate.",
            new Vector2(0.5f, 0), new Vector2(0, 340), new Vector2(1100, 80), 24);
        selectedLabel = Label(selection.transform, "", new Vector2(0.5f, 0), new Vector2(0, 285), new Vector2(700, 60), 32);
        var items = viewport.Items;
        if (items != null)
        {
            for (int i = 0; i < items.Count; i++)
            {
                ItemData item = items[i];
                if (item == null) continue;
                var button = Button(selection.transform, item.GetDisplayName(), new Vector2(0.5f, 0),
                    new Vector2((i - (items.Count - 1) * 0.5f) * 220, 180), new Vector2(200, 140), () => viewport.SelectItemById(item.GetId()));
                cards[item.GetId()] = button.GetComponent<Image>();
                var label = button.GetComponentInChildren<Text>();
                label.rectTransform.anchoredPosition = new Vector2(0, -42);
                label.rectTransform.sizeDelta = new Vector2(190, 40);
                var icon = new GameObject("Item Icon", typeof(RectTransform), typeof(Image));
                icon.transform.SetParent(button.transform, false);
                var r = icon.GetComponent<RectTransform>();
                r.sizeDelta = new Vector2(80, 80);
                r.anchoredPosition = new Vector2(0, 20);
                icon.GetComponent<Image>().sprite = item.icon;
                icon.GetComponent<Image>().preserveAspect = true;
                icon.GetComponent<Image>().raycastTarget = false;
            }
        }
        Button(selection.transform, "< Previous", new Vector2(0.5f, 0), new Vector2(-320, 65), new Vector2(200, 65), viewport.PreviousItem);
        Button(selection.transform, "SELECT ITEM", new Vector2(0.5f, 0), new Vector2(0, 65), new Vector2(350, 65), viewport.ConfirmCurrentItem);
        Button(selection.transform, "Next >", new Vector2(0.5f, 0), new Vector2(320, 65), new Vector2(200, 65), viewport.NextItem);
        changeButton = Button(canvas.transform, "Change Item", new Vector2(0.5f, 1), new Vector2(0, -55), new Vector2(280, 65), ShowSelection).gameObject;
        joysticks = GameObject.Find("Mobile Joysticks");
        viewport.CurrentItemChanged += Refresh;
        viewport.SelectionConfirmed += Confirmed;
        ShowSelection();
    }

    private void ShowSelection()
    {
        selection.SetActive(true);
        changeButton.SetActive(false);
        if (joysticks != null) joysticks.SetActive(false);
        Refresh(viewport.CurrentItemData, viewport.CurrentItemTransform);
    }

    private void Refresh(ItemData item, Transform model)
    {
        if (item == null) return;
        selectedLabel.text = item.GetDisplayName() + " - preview";
        foreach (var card in cards)
            card.Value.color = card.Key == item.GetId() ? new Color(0.15f, 0.55f, 0.75f, 0.95f) : new Color(0.08f, 0.12f, 0.18f, 0.95f);
    }

    private void Confirmed(ItemData item)
    {
        selection.SetActive(false);
        changeButton.SetActive(true);
        if (joysticks != null) joysticks.SetActive(true);
    }

    private Text Label(Transform parent, string caption, Vector2 anchor, Vector2 position, Vector2 size, int fontSize)
    {
        var obj = new GameObject(caption, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        var text = obj.GetComponent<Text>();
        text.font = font; text.fontSize = fontSize; text.text = caption;
        text.alignment = TextAnchor.MiddleCenter; text.color = Color.white; text.raycastTarget = false;
        var rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return text;
    }

    private Button Button(Transform parent, string caption, Vector2 anchor, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        var obj = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position; rect.sizeDelta = size;
        obj.GetComponent<Image>().color = new Color(0.08f, 0.25f, 0.36f, 0.96f);
        Label(obj.transform, caption, new Vector2(0.5f, 0.5f), Vector2.zero, size, 26);
        var button = obj.GetComponent<Button>();
        button.onClick.AddListener(action);
        return button;
    }

    private void OnDestroy()
    {
        if (viewport == null) return;
        viewport.CurrentItemChanged -= Refresh;
        viewport.SelectionConfirmed -= Confirmed;
    }
}
