using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    // --- DOOR STATE ---
    [System.Serializable]
    public struct SavedDoorState
    {
        public string doorID;       // Unique identifier for the door instance
        public float openRatio;     // The ratio between 0 (closed) and 1 (fully open)
        public bool isLocked;       // If the door was locked when saved
        public SavedDoorState(string id, float ratio, bool locked)
        {
            doorID = id;
            openRatio = ratio;
            isLocked = locked;
        }
    }
    // ------------------
    
    // --- INVENTORY ITEM STATE ---
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
    // ----------------------------
    
    // --- NEW: WORLD ITEM STATE ---
    [System.Serializable]
    public struct SavedWorldItemState
    {
        // We only need the unique ID of the collected item.
        public string worldItemID; 

        public SavedWorldItemState(string id)
        {
            worldItemID = id;
        }
    }
    // -----------------------------

    // Player Stats
    public float[] playerPosition;
    public float[] playerRotation;
    public float cameraPitch; // xRotation
    public float playerHealth;
    // Inventory 
    public List<SavedInventoryItem> savedInventoryItems;

    // Door States
    public List<SavedDoorState> doorStates;
    
    // World Items Collected <-- NEW FIELD
    public List<SavedWorldItemState> collectedWorldItems;

    // Current Scene (useful if you have multiple levels)
    public int sceneIndex;

    public SaveData()
    {
        // Initialize the lists
        savedInventoryItems = new List<SavedInventoryItem>();
        doorStates = new List<SavedDoorState>(); 
        collectedWorldItems = new List<SavedWorldItemState>(); // <-- Initialize new list
        
        playerPosition = new float[3];
        playerRotation = new float[4]; // Quaternion has 4 components
    }
}