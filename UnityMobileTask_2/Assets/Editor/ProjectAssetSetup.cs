using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ProjectAssetSetup
{
    private const string ResourcesFolder = "Assets/Resources";
    private const string IconFolder = "Assets/Resources/ItemIcons";
    private const string SlotPrefabPath = "Assets/Resources/SaveSlotPrefab.prefab";

    static ProjectAssetSetup()
    {
        EditorApplication.delayCall += PrepareProjectAssets;
    }

    private static void PrepareProjectAssets()
    {
        Directory.CreateDirectory(ResourcesFolder);
        Directory.CreateDirectory(IconFolder);

        CreateIconIfMissing("pickaxe", new Color(0.16f, 0.35f, 0.52f), 0);
        CreateIconIfMissing("potion", new Color(0.43f, 0.22f, 0.55f), 1);
        CreateIconIfMissing("flashlight", new Color(0.66f, 0.38f, 0.12f), 2);
        AssetDatabase.Refresh();

        AssignItem("Pickaxe", "pickaxe", "Pickaxe");
        AssignItem("Potion", "potion", "Potion");
        AssignItem("Flashlight", "flashlight", "Flashlight");
        CreateSlotPrefab();
        AssetDatabase.SaveAssets();
    }

    private static void CreateIconIfMissing(string id, Color background, int iconType)
    {
        string path = IconFolder + "/" + id + ".png";
        if (File.Exists(path))
            return;

        const int size = 128;
        Texture2D image = new Texture2D(size, size, TextureFormat.RGBA32, false);
        image.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f));
                image.SetPixel(x, y, distance < 60f ? background : new Color(0f, 0f, 0f, 0f));
            }
        }

        Color edge = new Color(0.9f, 0.93f, 1f);
        Color detail = iconType == 1 ? new Color(0.3f, 0.9f, 0.72f) : new Color(0.93f, 0.73f, 0.34f);

        if (iconType == 0)
        {
            DrawLine(image, new Vector2Int(38, 35), new Vector2Int(86, 92), new Color(0.42f, 0.25f, 0.12f), 9);
            DrawLine(image, new Vector2Int(29, 83), new Vector2Int(54, 54), edge, 12);
            DrawLine(image, new Vector2Int(54, 54), new Vector2Int(91, 62), edge, 12);
        }
        else if (iconType == 1)
        {
            DrawRect(image, 48, 83, 80, 99, edge);
            DrawLine(image, new Vector2Int(48, 83), new Vector2Int(39, 56), edge, 7);
            DrawLine(image, new Vector2Int(80, 83), new Vector2Int(89, 56), edge, 7);
            DrawLine(image, new Vector2Int(39, 56), new Vector2Int(45, 32), edge, 7);
            DrawLine(image, new Vector2Int(89, 56), new Vector2Int(83, 32), edge, 7);
            DrawLine(image, new Vector2Int(45, 32), new Vector2Int(83, 32), edge, 7);
            DrawRect(image, 50, 79, 77, 48, detail);
            DrawRect(image, 52, 92, 76, 103, new Color(0.96f, 0.82f, 0.49f));
        }
        else
        {
            DrawLine(image, new Vector2Int(34, 39), new Vector2Int(80, 83), edge, 17);
            DrawLine(image, new Vector2Int(76, 79), new Vector2Int(98, 99), detail, 24);
            DrawLine(image, new Vector2Int(30, 35), new Vector2Int(42, 46), new Color(0.25f, 0.3f, 0.38f), 21);
        }

        File.WriteAllBytes(path, image.EncodeToPNG());
        Object.DestroyImmediate(image);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }

    private static void AssignItem(string assetName, string id, string displayName)
    {
        string[] matches = AssetDatabase.FindAssets(assetName + " t:ItemData", new[] { "Assets/SO" });
        if (matches.Length == 0)
            return;

        string assetPath = AssetDatabase.GUIDToAssetPath(matches[0]);
        ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
        Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(IconFolder + "/" + id + ".png");
        if (item == null)
            return;

        item.itemId = id;
        item.itemName = displayName;
        item.icon = icon;
        EditorUtility.SetDirty(item);
    }

    private static void CreateSlotPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(SlotPrefabPath) != null)
            return;

        GameObject root = new GameObject("SaveSlotPrefab", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(600f, 78f);
        root.GetComponent<Image>().color = new Color(0.15f, 0.18f, 0.22f, 0.96f);
        root.GetComponent<LayoutElement>().preferredHeight = 78f;

        GameObject thumbnailObject = new GameObject("Thumbnail", typeof(RectTransform), typeof(Image));
        thumbnailObject.transform.SetParent(root.transform, false);
        RectTransform thumbnailRect = thumbnailObject.GetComponent<RectTransform>();
        thumbnailRect.anchorMin = new Vector2(0f, 0.5f);
        thumbnailRect.anchorMax = new Vector2(0f, 0.5f);
        thumbnailRect.pivot = new Vector2(0f, 0.5f);
        thumbnailRect.anchoredPosition = new Vector2(8f, 0f);
        thumbnailRect.sizeDelta = new Vector2(110f, 68f);
        thumbnailObject.GetComponent<Image>().preserveAspect = true;

        GameObject textObject = new GameObject("Slot Details", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(root.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(130f, 4f);
        textRect.offsetMax = new Vector2(-8f, -4f);
        Text details = textObject.GetComponent<Text>();
        details.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        details.fontSize = 18;
        details.color = Color.white;
        details.alignment = TextAnchor.MiddleLeft;
        details.horizontalOverflow = HorizontalWrapMode.Wrap;
        details.verticalOverflow = VerticalWrapMode.Overflow;

        PrefabUtility.SaveAsPrefabAsset(root, SlotPrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
    }

    private static void DrawRect(Texture2D image, int xMin, int yMin, int xMax, int yMax, Color color)
    {
        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                if (x >= 0 && y >= 0 && x < image.width && y < image.height)
                    image.SetPixel(x, y, color);
            }
        }
    }

    private static void DrawLine(Texture2D image, Vector2Int start, Vector2Int end, Color color, int thickness)
    {
        int steps = Mathf.Max(Mathf.Abs(end.x - start.x), Mathf.Abs(end.y - start.y));
        for (int step = 0; step <= steps; step++)
        {
            float blend = steps == 0 ? 0f : step / (float)steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(start.x, end.x, blend));
            int y = Mathf.RoundToInt(Mathf.Lerp(start.y, end.y, blend));
            int half = thickness / 2;
            DrawRect(image, x - half, y - half, x + half, y + half, color);
        }
    }
}

