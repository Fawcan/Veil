using UnityEngine;

public class SavePoint : MonoBehaviour
{
    public void Interact()
    {
        Debug.Log("Interacting with Save Point...");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGame();
            Debug.Log("Game Saved at Save Point!");

            // Optional: Add visual/audio feedback here
            // e.g., PlaySound(saveSound);
        }
        else
        {
            Debug.LogError("GameManager not found in scene!");
        }
    }
}