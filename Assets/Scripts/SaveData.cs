using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    [System.Serializable]
    public struct SavedDoorState
    {
        public string doorID;
        public float openRatio;
        public bool isLocked;
        public SavedDoorState(string id, float ratio, bool locked)
        {
            doorID = id;
            openRatio = ratio;
            isLocked = locked;
        }
    }

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

    [System.Serializable]
    public struct SavedWorldItemState
    {
        public string worldItemID; 

        public SavedWorldItemState(string id)
        {
            worldItemID = id;
        }
    }

    [System.Serializable]
    public struct SavedValveState
    {
        public string valveID;
        public float currentAngle;
        public bool isComplete;
        public bool isLocked;
        public bool visibleValveActive;
        public bool hiddenValveActive;

        public SavedValveState(string id, float angle, bool complete, bool locked, bool visibleActive, bool hiddenActive)
        {
            valveID = id;
            currentAngle = angle;
            isComplete = complete;
            isLocked = locked;
            visibleValveActive = visibleActive;
            hiddenValveActive = hiddenActive;
        }
    }

    public float[] playerPosition;
    public float[] playerRotation;
    public float cameraPitch;
    public float playerHealth;

    public List<SavedInventoryItem> savedInventoryItems;

    public List<SavedDoorState> doorStates;
    
    public List<SavedWorldItemState> collectedWorldItems;

    public List<SavedValveState> valveStates;

    public int sceneIndex;

    public SaveData()
    {
        savedInventoryItems = new List<SavedInventoryItem>();
        doorStates = new List<SavedDoorState>(); 
        collectedWorldItems = new List<SavedWorldItemState>();
        valveStates = new List<SavedValveState>();
        
        playerPosition = new float[3];
        playerRotation = new float[4];
    }
}