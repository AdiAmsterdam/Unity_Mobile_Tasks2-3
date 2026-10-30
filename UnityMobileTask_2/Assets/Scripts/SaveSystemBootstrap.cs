using UnityEngine;

public static class SaveSystemBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateSaveSystems()
    {
        if (SaveSystem.Instance != null)
            return;

        GameObject systems = new GameObject("Save System");
        Object.DontDestroyOnLoad(systems);
        systems.AddComponent<GameManager>();
        systems.AddComponent<SaveSystem>();
        systems.AddComponent<SaveMenuController>();
        systems.AddComponent<ItemSelectionScreen>();
    }
}


