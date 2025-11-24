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
        // Singleton Pattern
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

    // Call this to gather data and save
    public void SaveGame()
    {
        FindReferences(); // Ensure we have the latest player/inventory

        SaveData data = new SaveData();

        // 1. Save Player Transform
        data.playerPosition = new float[] { player.transform.position.x, player.transform.position.y, player.transform.position.z };
        data.playerRotation = new float[] { player.transform.rotation.x, player.transform.rotation.y, player.transform.rotation.z, player.transform.rotation.w };
        // Not using cameraPitch for now
        data.cameraPitch = player.GetCameraPitch(); // We need to add this getter to PlayerController

        // 2. Save Inventory
        data.inventoryItemNames = inventory.GetItemNames(); // We need to add this getter to Inventory

        // 3. Save Scene
        data.sceneIndex = SceneManager.GetActiveScene().buildIndex;

        SaveSystem.SaveGame(data);
    }

    // Call this to load data and apply it
    public void LoadGame()
    {
        SaveData data = SaveSystem.LoadGame();

        if (data != null)
        {
            // If the saved scene is different, load it first (Basic implementation)
            if (SceneManager.GetActiveScene().buildIndex != data.sceneIndex)
            {
                SceneManager.LoadScene(data.sceneIndex);
                // Note: You'd need a way to apply data AFTER the scene loads, usually via SceneManager.sceneLoaded event
            }

            FindReferences();
            
            // 1. Apply Player Transform
            Vector3 pos = new Vector3(data.playerPosition[0], data.playerPosition[1], data.playerPosition[2]);
            Quaternion rot = new Quaternion(data.playerRotation[0], data.playerRotation[1], data.playerRotation[2], data.playerRotation[3]);
            
            player.LoadState(pos, rot, data.cameraPitch);

            // 2. Apply Inventory
            inventory.LoadItems(data.inventoryItemNames);

            Debug.Log("Game Loaded!");
        }
    }

    private void FindReferences()
    {
        if (player == null) player = FindFirstObjectByType<FirstPersonController>();
        if (inventory == null) inventory = FindFirstObjectByType<InventorySystem>();
    }
}