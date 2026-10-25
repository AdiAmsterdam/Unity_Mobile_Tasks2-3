using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Mobile Task/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Item Data")]
    public string itemId;
    public string itemName;
    public GameObject prefab;
    public Sprite icon;

    public string GetId()
    {
        if (!string.IsNullOrWhiteSpace(itemId))
            return itemId;

        if (!string.IsNullOrWhiteSpace(itemName))
            return itemName.Trim().ToLowerInvariant().Replace(" ", "_");

        return name.ToLowerInvariant().Replace(" ", "_");
    }

    public string GetDisplayName()
    {
        return string.IsNullOrWhiteSpace(itemName) ? name : itemName;
    }
}


