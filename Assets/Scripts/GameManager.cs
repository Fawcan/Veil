using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using TMPro;
using System.Linq;

public class GameManager : MonoBehaviour
{
    #pragma warning disable CS0618 

    public static GameManager Instance;

    [Header("Dependencies")]
    public FirstPersonController fpc;
    public InventorySystem inventorySystem;

    [Header("UI References")]
    [Tooltip("Reference to the Panel containing the Game Over buttons.")]
    public GameObject gameOverUI; 
    [Tooltip("Reference to a UI component to show save/load status.")]
    public TextMeshProUGUI statusText; 

    public bool isGameOver = false;

    private string savePath;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            savePath = Application.persistentDataPath + "/gamesave.dat";
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ----------------------
    // Game Over & Menus
    // ----------------------

    public void TriggerGameOver()
    {
        isGameOver = true;

        // 1. Show the cursor (Unlocks the cursor for menu navigation)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false; 

        // 2. Pause the game physics/time
        Time.timeScale = 0f;

        // 3. Show the Menu
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Game Over UI is not assigned in the GameManager!");
        }
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }

    // ----------------------
    // Saving
    // ----------------------

    public void SaveGame()
    {
        // Prevent saving if dead
        if (isGameOver) return; 

        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(savePath);
        SaveData data = new SaveData();

        // 1. Save Player Data
        if (fpc != null)
        {
            Transform playerT = fpc.transform;
            data.playerPosition[0] = playerT.position.x;
            data.playerPosition[1] = playerT.position.y;
            data.playerPosition[2] = playerT.position.z;
            
            // Player rotation only saves the world rotation (yaw), pitch is reset on load
            data.playerRotation[0] = playerT.rotation.x;
            data.playerRotation[1] = playerT.rotation.y;
            data.playerRotation[2] = playerT.rotation.z;
            data.playerRotation[3] = playerT.rotation.w;

            data.playerHealth = fpc.currentHealth;
        }

        // 2. Save Inventory Data
        if (inventorySystem != null)
        {
            data.savedInventoryItems = inventorySystem.GetSavedItemsData();
        }

        // 3. Save Door States
        InteractableDoor[] doors = FindObjectsOfType<InteractableDoor>();
        foreach (InteractableDoor door in doors)
        {
            if (string.IsNullOrEmpty(door.doorID))
            {
                Debug.LogError($"Door '{door.gameObject.name}' is missing a unique 'Door ID'. Skipping save for this object.");
                continue;
            }
            data.doorStates.Add(door.GetSaveState());
        }

        // 4. Save World Items
        if (inventorySystem != null)
        {
            foreach (var item in inventorySystem.GetCollectedItems()) 
            {
                if (!string.IsNullOrEmpty(item.worldItemID))
                {
                    data.collectedWorldItems.Add(new SaveData.SavedWorldItemState(item.worldItemID));
                }
            }
        }

        // 5. Save Scene Data
        data.sceneIndex = SceneManager.GetActiveScene().buildIndex;

        bf.Serialize(file, data);
        file.Close();

        ShowStatus("Game Saved!", 2f);
    }

    // ----------------------
    // Loading & Respawning
    // ----------------------
    
    public void Respawn()
    {
        isGameOver = false;

        // 1. Hide the Game Over Menu
        if (gameOverUI != null) gameOverUI.SetActive(false);

        // 2. Unpause time so the game can run after loading
        Time.timeScale = 1f;

        // 3. Lock the cursor
        if (fpc != null) fpc.LockCursor();

        // 4. Load the last save
        LoadGame();
    }

    public void LoadGame()
    {
        if (File.Exists(savePath))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(savePath, FileMode.Open);
            SaveData data = (SaveData)bf.Deserialize(file);
            file.Close();

            if (data.sceneIndex != SceneManager.GetActiveScene().buildIndex)
            {
                SceneManager.LoadScene(data.sceneIndex);
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
            else
            {
                ApplyLoadData(data);
                if (fpc != null) fpc.LockCursor(); 
            }

            ShowStatus("Game Loaded!", 2f);
        }
        else
        {
            ShowStatus("No save file found!", 2f);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        if (File.Exists(savePath))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(savePath, FileMode.Open);
            SaveData data = (SaveData)bf.Deserialize(file);
            file.Close();
            ApplyLoadData(data);
            
            if (fpc != null) fpc.LockCursor();
        }
    }

    private void ApplyLoadData(SaveData data)
    {
        // 1. Load Player Data
        if (fpc != null)
        {
            Vector3 position = new Vector3(
                data.playerPosition[0], data.playerPosition[1], data.playerPosition[2]
            );
            Quaternion rotation = new Quaternion(
                data.playerRotation[0], data.playerRotation[1], data.playerRotation[2], data.playerRotation[3]
            );

            // Use the updated LoadState which now resets camera pitch
            fpc.LoadState(position, rotation);

            // If the player died, reset health to max upon loading
            if (data.playerHealth <= 0) 
            {
                fpc.currentHealth = fpc.maxHealth; 
            }
            else
            {
                fpc.currentHealth = data.playerHealth;
            }
        }

        // 2. Load Inventory Data
        if (inventorySystem != null)
        {
            inventorySystem.LoadItems(data.savedInventoryItems);
        }
        
        // 3. Load Door States
        InteractableDoor[] doors = FindObjectsOfType<InteractableDoor>();
        foreach (InteractableDoor door in doors)
        {
            SaveData.SavedDoorState savedState = data.doorStates.FirstOrDefault(s => s.doorID == door.doorID);
            if (!string.IsNullOrEmpty(savedState.doorID))
            {
                door.LoadState(savedState);
            }
        }

        // 4. Load World Item States
        PickableItem[] worldItems = FindObjectsOfType<PickableItem>();
        
        foreach (PickableItem worldItem in worldItems)
        {
            if (string.IsNullOrEmpty(worldItem.worldItemID))
            {
                Debug.LogWarning($"PickableItem '{worldItem.gameObject.name}' is missing a 'World Item ID'. Skipping state check.");
                continue;
            }

            bool wasCollected = data.collectedWorldItems.Any(s => s.worldItemID == worldItem.worldItemID);

            if (wasCollected)
            {
                Destroy(worldItem.gameObject);
            }
        }
    }
    
    private void ShowStatus(string message, float duration)
    {
        if (statusText != null)
        {
            statusText.text = message;
            CancelInvoke("HideStatus");
            Invoke("HideStatus", duration);
        }
        else
        {
            Debug.Log(message);
        }
    }

    private void HideStatus()
    {
        if (statusText != null)
        {
            statusText.text = "";
        }
    }
}