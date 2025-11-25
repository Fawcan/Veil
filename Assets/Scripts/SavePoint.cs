using UnityEngine;

public class SavePoint : MonoBehaviour
{
    public void Interact()
    {
        Debug.Log("Interacting with Save Point...");
        
        if (GameManager.Instance != null)
        {
            // 1. Perform the Save
            GameManager.Instance.SaveGame();
            
            // 2. Find the Inventory System to use its UI Feedback
            // (We use FindFirstObjectByType because there should only be one player/inventory)
            InventorySystem inventory = FindFirstObjectByType<InventorySystem>();
            
            if (inventory != null)
            {
                inventory.ShowFeedback("Game Saved");
            }
            else
            {
                // Fallback if inventory isn't found (e.g., pure console log)
                Debug.Log("Game Saved (Inventory UI not found)");
            }
        }
        else
        {
            Debug.LogError("GameManager not found in scene! Save failed.");
        }
    }
}