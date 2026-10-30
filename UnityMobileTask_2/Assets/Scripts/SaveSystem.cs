using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }
    public static float AutosaveInterval = 120f;

    private const string FolderName = "SaveSlots";
    private const string AutosaveName = "autosave.json";
    private readonly HashSet<string> savesInProgress = new HashSet<string>();
    private int manualSaveVersion;

    public string SaveFolder => Path.Combine(Application.persistentDataPath, FolderName);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Directory.CreateDirectory(SaveFolder);
    }

    public void SaveSlot(int slotNumber)
    {
        if (slotNumber < 1)
            return;

        string key = "slot_" + slotNumber.ToString("00");
        if (savesInProgress.Add(key))
        {
            manualSaveVersion++;
            StartCoroutine(SaveRoutine(slotNumber, false, key));
        }
    }

    public void SaveAutomatic()
    {
        if (savesInProgress.Any(key => key != "autosave")) return;
        const string key = "autosave";
        if (savesInProgress.Add(key))
            StartCoroutine(SaveRoutine(0, true, key));
    }

    private IEnumerator SaveRoutine(int slotNumber, bool automatic, string key)
    {
        int version = manualSaveVersion;
        yield return new WaitForEndOfFrame();
        if (automatic && version != manualSaveVersion)
        {
            savesInProgress.Remove(key);
            yield break;
        }

        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        if (screenshot == null)
        {
            savesInProgress.Remove(key);
            yield break;
        }

        SaveGameData data = GameManager.Instance != null
            ? GameManager.Instance.CreateSaveData(slotNumber, automatic)
            : new SaveGameData { slotNumber = slotNumber, isAutomaticSave = automatic };
        byte[] imageBytes = screenshot.EncodeToPNG();
        Destroy(screenshot);

        try
        {
            Directory.CreateDirectory(SaveFolder);
            File.WriteAllBytes(GetImagePath(slotNumber, automatic), imageBytes);
            SaveJsonWithBackup(GetJsonPath(slotNumber, automatic), JsonUtility.ToJson(data, true));
        }
        catch (Exception exception)
        {
            Debug.LogError("Could not save the game: " + exception.Message, this);
        }

        savesInProgress.Remove(key);
    }

    public bool LoadSlot(int slotNumber)
    {
        if (slotNumber < 1 || !TryReadSave(GetJsonPath(slotNumber, false), out SaveGameData data))
            return false;

        Apply(data);
        return true;
    }

    public bool LoadLatestAutomaticSave()
    {
        if (!TryReadSave(GetJsonPath(0, true), out SaveGameData data))
            return false;

        Apply(data);
        return true;
    }

    public bool TryGetSave(int slotNumber, out SaveGameData data)
    {
        return TryReadSave(GetJsonPath(slotNumber, false), out data);
    }

    public bool LoadAutomaticBackup()
    {
        if (!TryReadJson(GetJsonPath(0, true) + ".bak", out SaveGameData data)) return false;
        Apply(data);
        return true;
    }

    public bool TryGetScreenshot(int slotNumber, out Texture2D texture)
    {
        return TryLoadImage(GetImagePath(slotNumber, false), out texture);
    }

    public List<int> GetManualSaveSlots()
    {
        Directory.CreateDirectory(SaveFolder);
        List<int> slots = new List<int>();

        foreach (string path in Directory.GetFiles(SaveFolder, "slot_*.json*"))
        {
            string fileName = Path.GetFileName(path);
            if (!fileName.StartsWith("slot_", StringComparison.OrdinalIgnoreCase))
                continue;

            string numberText = fileName.Substring("slot_".Length);
            int extensionPosition = numberText.IndexOf('.');
            if (extensionPosition >= 0)
                numberText = numberText.Substring(0, extensionPosition);

            if (!int.TryParse(numberText, out int slot) || slot < 1)
                continue;

            if (TryReadSave(GetJsonPath(slot, false), out _))
                slots.Add(slot);
        }

        return slots.Distinct().OrderBy(slot => slot).ToList();
    }

    public bool HasLatestAutomaticSave =>
        TryReadSave(GetJsonPath(0, true), out _);

    private void Apply(SaveGameData data)
    {
        GameManager.Instance?.ApplySaveData(data);
        Time.timeScale = 1f;
    }

    private string GetJsonPath(int slotNumber, bool automatic)
    {
        string fileName = automatic ? AutosaveName : "slot_" + slotNumber.ToString("00") + ".json";
        return Path.Combine(SaveFolder, fileName);
    }

    private string GetImagePath(int slotNumber, bool automatic)
    {
        string fileName = automatic ? "autosave.png" : "slot_" + slotNumber.ToString("00") + ".png";
        return Path.Combine(SaveFolder, fileName);
    }

    private void SaveJsonWithBackup(string path, string json)
    {
        string temporaryPath = path + ".tmp";
        string backupPath = path + ".bak";
        File.WriteAllText(temporaryPath, json);

        if (File.Exists(path) && TryReadJson(path, out _))
            File.Copy(path, backupPath, true);

        if (File.Exists(path))
            File.Delete(path);

        File.Move(temporaryPath, path);
    }

    private bool TryReadSave(string path, out SaveGameData data)
    {
        if (TryReadJson(path, out data))
            return true;

        return TryReadJson(path + ".bak", out data);
    }

    private bool TryReadJson(string path, out SaveGameData data)
    {
        data = null;
        if (!File.Exists(path))
            return false;

        try
        {
            string json = File.ReadAllText(path);
            data = JsonUtility.FromJson<SaveGameData>(json);
            return data != null && !string.IsNullOrWhiteSpace(data.savedAt);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Could not read save file '" + path + "': " + exception.Message);
            return false;
        }
    }

    private bool TryLoadImage(string path, out Texture2D texture)
    {
        texture = null;
        if (!File.Exists(path))
            return false;

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (image.LoadImage(bytes))
            {
                texture = image;
                return true;
            }

            Destroy(image);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Could not load save thumbnail: " + exception.Message);
        }

        return false;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SaveCurrentStateImmediately();
        }
    }

    private void OnApplicationQuit()
    {
        SaveCurrentStateImmediately();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveCurrentStateImmediately();
    }

    private void SaveCurrentStateImmediately()
    {
        if (savesInProgress.Any(key => key != "autosave")) return;
        SaveGameData data = GameManager.Instance != null
            ? GameManager.Instance.CreateSaveData(0, true)
            : new SaveGameData { slotNumber = 0, isAutomaticSave = true, savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };

        data.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        try
        {
            Directory.CreateDirectory(SaveFolder);
            SaveJsonWithBackup(GetJsonPath(0, true), JsonUtility.ToJson(data, true));
        }
        catch (Exception exception)
        {
            Debug.LogError("Could not write the automatic save: " + exception.Message, this);
        }
    }
}



