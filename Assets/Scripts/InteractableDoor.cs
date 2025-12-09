using UnityEngine;

public class InteractableDoor : MonoBehaviour
{
    [Header("Saving")]
    [Tooltip("MUST be a unique identifier for this door instance in the scene.")]
    public string doorID;
    
    [Header("Door State")]
    public bool isLocked = false;
    
    [Header("Key Linkage (ID is used for logic)")]
    [Tooltip("The unique ID that the required key must match.")]
    public int requiredKeyId = 101;
    [Tooltip("The item name is only used for UI feedback, not logic.")]
    public string requiredKeyName = "Old Key"; 
    
    [Header("Movement")]
    public float openAngle = 125f; 
    public float interactionSpeed = 0.5f; 

    private Quaternion startRotation;
    private Quaternion openRotation;
    private Transform pivot;
    private bool isInteracting = false;
    
    private float targetRotationY; 
    
    private InventorySystem inventorySystem; 

    void Start()
    {
        pivot = transform; 
        
        startRotation = pivot.localRotation;
        
        openRotation = startRotation * Quaternion.Euler(0, openAngle, 0);
        
        targetRotationY = 0f; 
        pivot.localRotation = startRotation;
        
        inventorySystem = FindFirstObjectByType<InventorySystem>();
        
        if (requiredKeyId == 0)
        {
             Debug.LogError($"Door on object {gameObject.name} has requiredKeyId set to 0! Please set a unique ID in the Inspector.");
        }
    }

    public void UnlockByMechanism()
    {
        isLocked = false;
        if (inventorySystem != null)
        {
            inventorySystem.ShowFeedback("Mechanism active. Hatch unlocked.");
        }
    }

    public void LockByMechanism()
    {
        isLocked = true;
        if (inventorySystem != null)
        {
            inventorySystem.ShowFeedback("Mechanism inactive. Hatch locked.");
        }
    }

    public bool TryUseItem(PickableItem item)
    {
        if (isLocked)
        {
            if (item.itemId == requiredKeyId)
            {
                isLocked = false; 
                
                if (inventorySystem != null) 
                    inventorySystem.ShowFeedback($"Used {item.itemName}. Door Unlocked! Drag to open.");
                Debug.Log($"Door unlocked successfully using Key ID {item.itemId}.");
                
                
                return true; 
            }
            else
            {
                if (inventorySystem != null) 
                    inventorySystem.ShowFeedback($"Wrong Key. This door requires the key named: {requiredKeyName}.");
                return false; 
            }
        }
        
        if (inventorySystem != null) inventorySystem.ShowFeedback("The door is already unlocked.");
        return false;
    }
    
    public void StartInteract()
    {
        if (isLocked)
        {
            if (inventorySystem != null) inventorySystem.ShowFeedback("It's locked.");
            Debug.Log("Door is locked.");
            return;
        }
        
        isInteracting = true;
    }
    
    public void UpdateInteraction(float xInput)
    {
        if (!isInteracting || isLocked) return; 

        targetRotationY += xInput * interactionSpeed * Time.deltaTime * 1f; 
        
        targetRotationY = Mathf.Clamp(targetRotationY, 0f, 1f); 
        
        Quaternion targetRot = Quaternion.Lerp(startRotation, openRotation, targetRotationY);
        pivot.localRotation = targetRot;
    }
    
    public void StopInteract()
    {
        isInteracting = false;
    }
    
    void Update()
    {
    }
    
    public SaveData.SavedDoorState GetSaveState()
    {
        return new SaveData.SavedDoorState(doorID, targetRotationY, isLocked);
    }

    public void LoadState(SaveData.SavedDoorState savedState)
    {
        if (savedState.doorID != doorID) return;

        targetRotationY = savedState.openRatio;
        
        Quaternion loadedRotation = Quaternion.Lerp(startRotation, openRotation, targetRotationY);
        pivot.localRotation = loadedRotation;
        
        isLocked = savedState.isLocked;
        
        Debug.Log($"Loaded state for Door: {doorID}. Open Ratio: {targetRotationY}, Locked: {isLocked}");
    }
}