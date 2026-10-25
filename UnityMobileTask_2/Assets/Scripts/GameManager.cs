using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Saved game values")]
    [Min(0f)] public float health = 100f;
    [Min(0)] public int score;
    [Min(1)] public int currentWave = 1;
    public string selectedCharacterId = "pickaxe";

    private ItemViewportController itemViewport;
    private float autosaveTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        itemViewport = FindAnyObjectByType<ItemViewportController>();
    }

    private void OnEnable()
    {
        if (itemViewport == null)
            itemViewport = FindAnyObjectByType<ItemViewportController>();

        if (itemViewport != null)
            itemViewport.SelectionConfirmed += OnSelectionConfirmed;
    }

    private void OnDisable()
    {
        if (itemViewport != null)
            itemViewport.SelectionConfirmed -= OnSelectionConfirmed;
    }

    private void Update()
    {
        if (SaveSystem.AutosaveInterval <= 0f)
            return;

        autosaveTimer += Time.unscaledDeltaTime;
        if (autosaveTimer >= SaveSystem.AutosaveInterval)
        {
            autosaveTimer = 0f;
            SaveSystem.Instance?.SaveAutomatic();
        }
    }

    public SaveGameData CreateSaveData(int slotNumber, bool automatic)
    {
        if (itemViewport == null)
            itemViewport = FindAnyObjectByType<ItemViewportController>();

        SaveGameData data = new SaveGameData
        {
            slotNumber = slotNumber,
            isAutomaticSave = automatic,
            savedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            health = health,
            score = score,
            currentWave = currentWave,
            selectedCharacterId = itemViewport != null && !string.IsNullOrEmpty(itemViewport.CurrentItemId)
                ? itemViewport.CurrentItemId
                : selectedCharacterId
        };

        Transform item = itemViewport != null ? itemViewport.CurrentItemTransform : null;
        if (item != null)
        {
            data.hasItemTransform = true;
            data.itemPosition = item.position;
            data.itemRotation = item.eulerAngles;
            data.itemScale = item.localScale;
        }

        return data;
    }

    public void ApplySaveData(SaveGameData data)
    {
        if (data == null)
            return;

        health = Mathf.Max(0f, data.health);
        score = Mathf.Max(0, data.score);
        currentWave = Mathf.Max(1, data.currentWave);
        selectedCharacterId = string.IsNullOrWhiteSpace(data.selectedCharacterId)
            ? "pickaxe"
            : data.selectedCharacterId;

        if (itemViewport == null)
            itemViewport = FindAnyObjectByType<ItemViewportController>();

        if (itemViewport == null)
            return;

        itemViewport.SelectItemById(selectedCharacterId);
        Transform item = itemViewport.CurrentItemTransform;
        if (item == null || !data.hasItemTransform)
            return;

        item.SetPositionAndRotation(data.itemPosition, Quaternion.Euler(data.itemRotation));
        item.localScale = data.itemScale;
    }

    public void AddScore(int points)
    {
        score = Mathf.Max(0, score + points);
    }

    public void SetWave(int wave)
    {
        currentWave = Mathf.Max(1, wave);
    }

    private void OnSelectionConfirmed(ItemData item)
    {
        if (item != null)
            selectedCharacterId = item.GetId();
    }
}


