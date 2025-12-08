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

    public void TriggerGameOver()
    {
        isGameOver = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false; 

        Time.timeScale = 0f;

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

    public void SaveGame()
    {
        if (isGameOver) return; 

        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(savePath);
        SaveData data = new SaveData();

        if (fpc != null)
        {
            Transform playerT = fpc.transform;
            data.playerPosition[0] = playerT.position.x;
            data.playerPosition[1] = playerT.position.y;
            data.playerPosition[2] = playerT.position.z;
            
            data.playerRotation[0] = playerT.rotation.x;
            data.playerRotation[1] = playerT.rotation.y;
            data.playerRotation[2] = playerT.rotation.z;
            data.playerRotation[3] = playerT.rotation.w;

            data.playerHealth = fpc.currentHealth;
        }

        if (inventorySystem != null)
        {
            data.savedInventoryItems = inventorySystem.GetSavedItemsData();
        }

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

        InteractableValve[] valves = FindObjectsOfType<InteractableValve>();
        foreach (InteractableValve valve in valves)
        {
            if (string.IsNullOrEmpty(valve.valveID))
            {
                Debug.LogError($"Valve '{valve.gameObject.name}' is missing a unique 'Valve ID'. Skipping save for this object.");
                continue;
            }
            data.valveStates.Add(valve.GetSaveState());
        }

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

        data.sceneIndex = SceneManager.GetActiveScene().buildIndex;

        bf.Serialize(file, data);
        file.Close();

        ShowStatus("Game Saved!", 2f);
    }
    
    public void Respawn()
    {
        isGameOver = false;

        if (gameOverUI != null) gameOverUI.SetActive(false);

        Time.timeScale = 1f;

        if (fpc != null) fpc.LockCursor();

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
        if (fpc != null)
        {
            Vector3 position = new Vector3(
                data.playerPosition[0], data.playerPosition[1], data.playerPosition[2]
            );
            Quaternion rotation = new Quaternion(
                data.playerRotation[0], data.playerRotation[1], data.playerRotation[2], data.playerRotation[3]
            );

            fpc.LoadState(position, rotation);

            if (data.playerHealth <= 0) 
            {
                fpc.currentHealth = fpc.maxHealth; 
            }
            else
            {
                fpc.currentHealth = data.playerHealth;
            }
        }

        if (inventorySystem != null)
        {
            inventorySystem.LoadItems(data.savedInventoryItems);
        }
        
        InteractableDoor[] doors = FindObjectsOfType<InteractableDoor>();
        foreach (InteractableDoor door in doors)
        {
            SaveData.SavedDoorState savedState = data.doorStates.FirstOrDefault(s => s.doorID == door.doorID);
            if (!string.IsNullOrEmpty(savedState.doorID))
            {
                door.LoadState(savedState);
            }
        }

        InteractableValve[] valves = FindObjectsOfType<InteractableValve>();
        foreach (InteractableValve valve in valves)
        {
            SaveData.SavedValveState savedState = data.valveStates.FirstOrDefault(s => s.valveID == valve.valveID);
            if (!string.IsNullOrEmpty(savedState.valveID))
            {
                valve.LoadState(savedState);
            }
        }

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