using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    // Player Stats
    public float[] playerPosition;
    public float[] playerRotation;
    public float cameraPitch; // xRotation

    // Inventory (We store item names)
    public List<string> inventoryItemNames;

    // Current Scene (useful if you have multiple levels)
    public int sceneIndex;

    public SaveData()
    {
        inventoryItemNames = new List<string>();
        playerPosition = new float[3];
        playerRotation = new float[4]; // Quaternion has 4 components
    }
}