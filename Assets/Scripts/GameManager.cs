using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using TMPro;

public class GameManager : MonoBehaviour
{
    // --- FIX: This line enables the legacy Input system methods (like Input.GetKeyDown) 
    //           to work even if the Input System package is active.
    #pragma warning disable CS0618 

    public static GameManager Instance;

    [Header("Dependencies")]
    public FirstPersonController fpc;
    public InventorySystem inventorySystem;

    // Assumed dependency for showing game status
    [Tooltip("Reference to a UI component to show save/load status.")]
    public TextMeshProUGUI statusText; 

    // File path for saving
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

    void Update()
    {

    }

    // ----------------------
    // Saving
    // ----------------------

    public void SaveGame()
    {
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
            
            // Assuming player rotation is used for horizontal view, and camera pitch for vertical
            data.playerRotation[0] = playerT.rotation.x;
            data.playerRotation[1] = playerT.rotation.y;
            data.playerRotation[2] = playerT.rotation.z;
            data.playerRotation[3] = playerT.rotation.w;

            // Assuming FirstPersonController stores camera pitch (vertical look)
            // data.cameraPitch = fpc.GetCameraPitch(); 
        }

        // 2. Save Inventory Data
        if (inventorySystem != null)
        {
            // FIX: Using the new function and variable name: GetSavedItemsData()
            data.savedInventoryItems = inventorySystem.GetSavedItemsData();
        }

        // 3. Save Scene Data
        data.sceneIndex = SceneManager.GetActiveScene().buildIndex;

        bf.Serialize(file, data);
        file.Close();

        ShowStatus("Game Saved!", 2f);
    }

    // ----------------------
    // Loading
    // ----------------------

    public void LoadGame()
    {
        if (File.Exists(savePath))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(savePath, FileMode.Open);
            SaveData data = (SaveData)bf.Deserialize(file);
            file.Close();

            // Load the correct scene first
            if (data.sceneIndex != SceneManager.GetActiveScene().buildIndex)
            {
                SceneManager.LoadScene(data.sceneIndex);
                // We postpone the actual loading until the scene is fully loaded
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
            else
            {
                ApplyLoadData(data);
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
        }
    }

    private void ApplyLoadData(SaveData data)
    {
        // 1. Load Player Data
        if (fpc != null)
        {
            fpc.transform.position = new Vector3(
                data.playerPosition[0],
                data.playerPosition[1],
                data.playerPosition[2]
            );
            fpc.transform.rotation = new Quaternion(
                data.playerRotation[0],
                data.playerRotation[1],
                data.playerRotation[2],
                data.playerRotation[3]
            );
            // fpc.SetCameraPitch(data.cameraPitch); // You need to implement this setter in FPC
        }

        // 2. Load Inventory Data
        if (inventorySystem != null)
        {
            // FIX: Using the new variable name: savedInventoryItems
            inventorySystem.LoadItems(data.savedInventoryItems);
        }
    }

    // Helper function to display messages
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