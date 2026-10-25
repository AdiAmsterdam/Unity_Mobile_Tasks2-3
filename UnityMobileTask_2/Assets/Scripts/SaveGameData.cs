using System;
using UnityEngine;

[Serializable]
public class SaveGameData
{
    public int slotNumber;
    public bool isAutomaticSave;
    public string savedAt;
    public float health = 100f;
    public int score;
    public int currentWave = 1;
    public string selectedCharacterId = "pickaxe";
    public bool hasItemTransform;
    public Vector3 itemPosition;
    public Vector3 itemRotation;
    public Vector3 itemScale = Vector3.one;
}


