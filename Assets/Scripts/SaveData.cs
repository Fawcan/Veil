using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    // --- NEW: Nested serializable class to store full item data ---
    [System.Serializable]
    public class SavedInventoryItem
    {
        public string itemName;
        public int itemId;
        public bool canBeDiscarded;

        public SavedInventoryItem(string name, int id, bool discardable)
        {
            itemName = name;
            itemId = id;
            canBeDiscarded = discardable;
        }
    }
    // -------------------------------------------------------------

    // Player Stats
    public float[] playerPosition;
    public float[] playerRotation;
    public float cameraPitch; // xRotation

    // Inventory (We now store structured item data)
    public List<SavedInventoryItem> savedInventoryItems;

    // Current Scene (useful if you have multiple levels)
    public int sceneIndex;

    public SaveData()
    {
        // Initialize the new list
        savedInventoryItems = new List<SavedInventoryItem>();
        playerPosition = new float[3];
        playerRotation = new float[4]; // Quaternion has 4 components
    }
}