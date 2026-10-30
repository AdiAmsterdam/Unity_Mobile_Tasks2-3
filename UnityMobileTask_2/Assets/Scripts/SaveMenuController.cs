using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class SaveMenuController : MonoBehaviour
{
    private const int ManualSlotCount = 3;
    private readonly List<Texture2D> loadedThumbnails = new List<Texture2D>();

    private Font font;
    private Canvas canvas;
    private GameObject pausePanel;
    private GameObject loadPanel;
    private Transform slotList;
    private ScrollRect slotScroll;
    private Text statusText;
    private GameObject slotPrefab;

    private void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        CreateEventSystemIfNeeded();
        BuildInterface();
    }

    private void BuildInterface()
    {
        GameObject canvasObject = new GameObject(
            "Save Menu Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Button pauseButton = CreateButton(
            canvas.transform,
            "Pause",
            "Pause",
            new Vector2(1f, 1f),
            new Vector2(-95f, -55f),
            new Vector2(150f, 64f));
        pauseButton.onClick.AddListener(OpenPauseMenu);

        pausePanel = CreatePanel(canvas.transform, "Pause Menu", new Vector2(520f, 470f));
        CreateText(pausePanel.transform, "Title", "Paused", new Vector2(0f, -48f), new Vector2(420f, 58f), 36, TextAnchor.MiddleCenter);

        for (int i = 1; i <= ManualSlotCount; i++)
        {
            int slot = i;
            Button saveButton = CreateButton(
                pausePanel.transform,
                "Save Slot " + slot,
                "Save to Slot " + slot,
                new Vector2(0.5f, 1f),
                new Vector2(0f, -125f - (i - 1) * 66f),
                new Vector2(360f, 54f));
            saveButton.onClick.AddListener(() => SaveToSlot(slot));
        }

        Button openLoads = CreateButton(
            pausePanel.transform,
            "Load Saves",
            "Load a Save",
            new Vector2(0.5f, 1f),
            new Vector2(0f, -335f),
            new Vector2(360f, 54f));
        openLoads.onClick.AddListener(OpenLoadMenu);

        Button resume = CreateButton(
            pausePanel.transform,
            "Resume",
            "Resume",
            new Vector2(0.5f, 1f),
            new Vector2(0f, -402f),
            new Vector2(360f, 54f));
        resume.onClick.AddListener(CloseMenus);

        loadPanel = CreatePanel(canvas.transform, "Load Menu", new Vector2(700f, 560f));
        CreateText(loadPanel.transform, "Load Title", "Saved Games", new Vector2(0f, -42f), new Vector2(600f, 54f), 32, TextAnchor.MiddleCenter);

        Button latestAuto = CreateButton(
            loadPanel.transform,
            "Load Latest Auto Save",
            "Load Latest Auto-Save",
            new Vector2(0.5f, 1f),
            new Vector2(-155f, -105f),
            new Vector2(300f, 52f));
        latestAuto.onClick.AddListener(LoadLatestAutomatic);

        Button backup = CreateButton(loadPanel.transform, "Load Auto Backup", "Previous Auto Backup",
            new Vector2(0.5f, 1f), new Vector2(155f, -105f), new Vector2(300f, 52f));
        backup.onClick.AddListener(() =>
        {
            if (SaveSystem.Instance != null && SaveSystem.Instance.LoadAutomaticBackup()) CloseMenus();
            else statusText.text = "No previous valid auto-save backup exists yet.";
        });

        CreateSlotScrollView(loadPanel.transform);

        Button back = CreateButton(
            loadPanel.transform,
            "Back",
            "Back",
            new Vector2(0.5f, 0f),
            new Vector2(0f, 34f),
            new Vector2(300f, 52f));
        back.onClick.AddListener(() =>
        {
            loadPanel.SetActive(false);
            pausePanel.SetActive(true);
        });

        statusText = CreateText(
            canvas.transform,
            "Save Status",
            string.Empty,
            new Vector2(0f, 48f),
            new Vector2(700f, 45f),
            22,
            TextAnchor.MiddleCenter);
        statusText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        statusText.rectTransform.anchorMax = new Vector2(0.5f, 0f);

        pausePanel.SetActive(false);
        loadPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    private void CreateSlotScrollView(Transform parent)
    {
        GameObject scrollObject = new GameObject("Save Slot Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(parent, false);
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.5f, 1f);
        scrollRect.anchorMax = new Vector2(0.5f, 1f);
        scrollRect.pivot = new Vector2(0.5f, 1f);
        scrollRect.anchoredPosition = new Vector2(0f, -175f);
        scrollRect.sizeDelta = new Vector2(620f, 300f);
        scrollObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.12f);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollObject.transform, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        slotScroll = scrollObject.GetComponent<ScrollRect>();
        slotScroll.viewport = viewportRect;
        slotScroll.content = contentRect;
        slotScroll.horizontal = false;
        slotScroll.vertical = true;
        slotList = content.transform;
    }

    private void OpenPauseMenu()
    {
        loadPanel.SetActive(false);
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    private void OpenLoadMenu()
    {
        pausePanel.SetActive(false);
        loadPanel.SetActive(true);
        RefreshSaveSlots();
    }

    private void CloseMenus()
    {
        pausePanel.SetActive(false);
        loadPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    private void SaveToSlot(int slot)
    {
        if (SaveSystem.Instance == null)
            return;

        SaveSystem.Instance.SaveSlot(slot);
        statusText.text = "Saving slot " + slot + "...";
        CloseMenus();
        Invoke(nameof(ClearStatus), 2f);
    }

    private void LoadLatestAutomatic()
    {
        if (SaveSystem.Instance == null || !SaveSystem.Instance.LoadLatestAutomaticSave())
        {
            statusText.text = "There is no automatic save yet.";
            return;
        }

        CloseMenus();
    }

    private void LoadManualSlot(int slot)
    {
        if (SaveSystem.Instance == null || !SaveSystem.Instance.LoadSlot(slot))
        {
            statusText.text = "That save could not be loaded.";
            return;
        }

        CloseMenus();
    }

    private void RefreshSaveSlots()
    {
        foreach (Transform child in slotList)
            Destroy(child.gameObject);

        foreach (Texture2D texture in loadedThumbnails)
            Destroy(texture);
        loadedThumbnails.Clear();

        if (SaveSystem.Instance == null)
            return;

        List<int> slots = SaveSystem.Instance.GetManualSaveSlots();
        for (int i = 0; i < slots.Count; i++)
        {
            int slot = slots[i];
            if (!SaveSystem.Instance.TryGetSave(slot, out SaveGameData data))
                continue;

            GameObject entry = CreateSlotEntry();
            entry.name = "Save Slot " + slot;
            entry.transform.SetParent(slotList, false);

            Text label = entry.transform.Find("Slot Details").GetComponent<Text>();
            label.text = "Slot " + slot + "  |  Wave " + data.currentWave + "  |  Score " + data.score + "\n" + data.savedAt;

            Image thumbnail = entry.transform.Find("Thumbnail").GetComponent<Image>();
            if (SaveSystem.Instance.TryGetScreenshot(slot, out Texture2D image))
            {
                loadedThumbnails.Add(image);
                thumbnail.sprite = Sprite.Create(
                    image,
                    new Rect(0f, 0f, image.width, image.height),
                    new Vector2(0.5f, 0.5f));
            }

            Button button = entry.GetComponent<Button>();
            button.onClick.AddListener(() => LoadManualSlot(slot));
        }

        if (slots.Count == 0)
        {
            Text emptyText = CreateText(
                slotList,
                "No Saves",
                "No manual saves found.",
                Vector2.zero,
                new Vector2(500f, 60f),
                24,
                TextAnchor.MiddleCenter);
            emptyText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            emptyText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        }

        slotScroll.verticalNormalizedPosition = 1f;
    }

    private GameObject CreateSlotEntry()
    {
        if (slotPrefab == null)
            slotPrefab = Resources.Load<GameObject>("SaveSlotPrefab");

        if (slotPrefab != null)
            return Instantiate(slotPrefab);

        GameObject entry = new GameObject("Save Slot", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        RectTransform rect = entry.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(600f, 78f);
        entry.GetComponent<Image>().color = new Color(0.15f, 0.18f, 0.22f, 0.96f);

        Image thumbnail = CreateImage(entry.transform, "Thumbnail", new Vector2(0f, 0.5f), new Vector2(72f, 0f), new Vector2(110f, 68f));
        thumbnail.preserveAspect = true;
        Text details = CreateText(entry.transform, "Slot Details", string.Empty, new Vector2(0f, 0.5f), new Vector2(370f, 0f), new Vector2(470f, 68f), 18, TextAnchor.MiddleLeft);
        details.rectTransform.anchorMin = Vector2.zero;
        details.rectTransform.anchorMax = Vector2.one;
        details.rectTransform.offsetMin = new Vector2(130f, 0f);
        details.rectTransform.offsetMax = new Vector2(-8f, 0f);
        return entry;
    }

    private void CreateEventSystemIfNeeded()
    {
        EventSystem eventSystem = FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(eventSystemObject);
            return;
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            foreach (BaseInputModule module in eventSystem.GetComponents<BaseInputModule>())
                Destroy(module);
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
    }

    private GameObject CreatePanel(Transform parent, string objectName, Vector2 size)
    {
        GameObject panel = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        panel.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.1f, 0.94f);
        return panel;
    }

    private Button CreateButton(Transform parent, string objectName, string caption, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        buttonObject.GetComponent<Image>().color = new Color(0.16f, 0.43f, 0.68f, 1f);

        Text label = CreateText(buttonObject.transform, "Label", caption, Vector2.zero, Vector2.zero, Vector2.zero, 22, TextAnchor.MiddleCenter);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        return buttonObject.GetComponent<Button>();
    }

    private Text CreateText(Transform parent, string objectName, string text, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
    {
        return CreateText(parent, objectName, text, new Vector2(0.5f, 1f), position, size, fontSize, alignment);
    }

    private Text CreateText(Transform parent, string objectName, string text, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text label = textObject.GetComponent<Text>();
        label.font = font;
        label.raycastTarget = false; // Status text must not block selection buttons underneath.
        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = alignment;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return label;
    }

    private Image CreateImage(Transform parent, string objectName, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return imageObject.GetComponent<Image>();
    }

    private void ClearStatus()
    {
        if (statusText != null)
            statusText.text = string.Empty;
    }

    private void OnDestroy()
    {
        foreach (Texture2D texture in loadedThumbnails)
        {
            if (texture != null)
                Destroy(texture);
        }
    }
}


