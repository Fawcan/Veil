using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private FirstPersonController player;
    private InventorySystem inventory;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveGame()
    {
        FindReferences(); 

        if (player == null || inventory == null)
        {
            Debug.LogError("Cannot Save: Player or Inventory not found!");
            return;
        }

        SaveData data = new SaveData();
        data.playerPosition = new float[] { player.transform.position.x, player.transform.position.y, player.transform.position.z };
        data.playerRotation = new float[] { player.transform.rotation.x, player.transform.rotation.y, player.transform.rotation.z, player.transform.rotation.w };
        data.cameraPitch = player.GetCameraPitch(); 
        data.inventoryItemNames = inventory.GetItemNames(); 
        data.sceneIndex = SceneManager.GetActiveScene().buildIndex;

        SaveSystem.SaveGame(data);
        Debug.Log("Game Saved Successfully.");
    }

    public void LoadGame()
    {
        SaveData data = SaveSystem.LoadGame();

        if (data != null)
        {
            // 1. Scene Management (if needed)
            if (SceneManager.GetActiveScene().buildIndex != data.sceneIndex)
            {
                // This is the cleanest way to handle scene load and data application
                SceneManager.LoadScene(data.sceneIndex);
                // Note: If you use SceneManager.LoadScene, you MUST use the 
                // SceneManager.sceneLoaded event to call ApplyLoadedData(data).
                // For now, we assume a single scene or persistent player objects.
            }

            ApplyLoadedData(data);

        }
        else
        {
            Debug.Log("No Save File Found. Cannot load game.");
        }
    }

    private void ApplyLoadedData(SaveData data)
    {
        // 2. Critical: Ensure all references are current after potential scene loads
        FindReferences(); 
        
        // 3. Apply Player State
        if (player != null)
        {
            Vector3 pos = new Vector3(data.playerPosition[0], data.playerPosition[1], data.playerPosition[2]);
            Quaternion rot = new Quaternion(data.playerRotation[0], data.playerRotation[1], data.playerRotation[2], data.playerRotation[3]);
            player.LoadState(pos, rot, data.cameraPitch);
        }
        else
        {
            Debug.LogError("Load Failed: Player (FirstPersonController) reference is missing after FindReferences.");
        }

        // 4. Apply Inventory State
        if (inventory != null)
        {
            inventory.LoadItems(data.inventoryItemNames);
            Debug.Log($"Inventory Load Initiated with {data.inventoryItemNames.Count} items.");
        }
        else
        {
            Debug.LogError("Load Failed: InventorySystem reference is missing after FindReferences.");
        }

        Debug.Log("Game Load Attempt Complete.");
        inventory.ShowFeedback("Game Loaded");
    }

    private void FindReferences()
    {
        // Only find if null, for performance
        if (player == null) player = FindFirstObjectByType<FirstPersonController>();
        if (inventory == null) inventory = FindFirstObjectByType<InventorySystem>();
    }
}