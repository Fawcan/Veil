using UnityEngine;

public class SavePoint : MonoBehaviour
{
    public void Interact()
    {
        Debug.Log("Interacting with Save Point...");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGame();
            
            InventorySystem inventory = FindFirstObjectByType<InventorySystem>();
            
            if (inventory != null)
            {
                inventory.ShowFeedback("Game Saved");
            }
            else
            {
                Debug.Log("Game Saved (Inventory UI not found)");
            }
        }
        else
        {
            Debug.LogError("GameManager not found in scene! Save failed.");
        }
    }
}