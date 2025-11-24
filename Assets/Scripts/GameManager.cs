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

        // 1. Save Player
        data.playerPosition = new float[] { player.transform.position.x, player.transform.position.y, player.transform.position.z };
        data.playerRotation = new float[] { player.transform.rotation.x, player.transform.rotation.y, player.transform.rotation.z, player.transform.rotation.w };
        data.cameraPitch = player.GetCameraPitch(); 

        // 2. Save Inventory
        data.inventoryItemNames = inventory.GetItemNames(); 

        // 3. Save Scene
        data.sceneIndex = SceneManager.GetActiveScene().buildIndex;

        SaveSystem.SaveGame(data);
        Debug.Log("Game Saved Successfully.");
    }

    public void LoadGame()
    {
        SaveData data = SaveSystem.LoadGame();

        if (data != null)
        {
            // Reload scene if necessary
            if (SceneManager.GetActiveScene().buildIndex != data.sceneIndex)
            {
                SceneManager.LoadScene(data.sceneIndex);
                // Note: In a complex game, you'd use OnSceneLoaded event to wait for the load.
                // For simple setups, Unity might handle the objects if they persist, 
                // but FindReferences below handles the re-link.
            }

            // Force find references again in case objects were destroyed/recreated
            FindReferences(); 
            
            if (player != null)
            {
                Vector3 pos = new Vector3(data.playerPosition[0], data.playerPosition[1], data.playerPosition[2]);
                Quaternion rot = new Quaternion(data.playerRotation[0], data.playerRotation[1], data.playerRotation[2], data.playerRotation[3]);
                player.LoadState(pos, rot, data.cameraPitch);
            }

            if (inventory != null)
            {
                inventory.LoadItems(data.inventoryItemNames);
            }

            Debug.Log("Game Loaded Successfully.");
        }
        else
        {
            Debug.Log("No Save File Found.");
        }
    }

    private void FindReferences()
    {
        // Use the modern Find function
        if (player == null) player = FindFirstObjectByType<FirstPersonController>();
        if (inventory == null) inventory = FindFirstObjectByType<InventorySystem>();
    }
}