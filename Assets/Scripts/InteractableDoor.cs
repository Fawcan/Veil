using UnityEngine;

public class InteractableDoor : MonoBehaviour
{
    [Header("Saving")]
    [Tooltip("MUST be a unique identifier for this door instance in the scene.")]
    public string doorID; // <-- New field for unique saving
    
    [Header("Door State")]
    public bool isLocked = false;
    
    [Header("Key Linkage (ID is used for logic)")]
    [Tooltip("The unique ID that the required key must match.")]
    public int requiredKeyId = 101; // Example ID
    [Tooltip("The item name is only used for UI feedback, not logic.")]
    public string requiredKeyName = "Old Key"; 
    
    [Header("Movement")]
    public float openAngle = 125f; 
    public float interactionSpeed = 0.5f; 

    // Rotation/Physics members
    private Quaternion startRotation;
    private Quaternion openRotation;
    private Transform pivot;
    private bool isInteracting = false;
    
    // targetRotationY: 0 (closed) to 1 (open) ratio
    private float targetRotationY; 
    
    private InventorySystem inventorySystem; 

    void Start()
    {
        pivot = transform; 
        
        startRotation = pivot.localRotation;
        
        // Calculate the single, fixed open rotation
        openRotation = startRotation * Quaternion.Euler(0, openAngle, 0);
        
        // targetRotationY will default to 0 (closed) unless loaded
        targetRotationY = 0f; 
        pivot.localRotation = startRotation; // Ensure the door starts closed initially
        
        inventorySystem = FindFirstObjectByType<InventorySystem>();
        
        if (requiredKeyId == 0)
        {
             Debug.LogError($"Door on object {gameObject.name} has requiredKeyId set to 0! Please set a unique ID in the Inspector.");
        }

        // IMPORTANT: Check for SaveManager and call LoadState if necessary here,
        // once your central SaveManager is implemented. (e.g., SaveManager.Instance.OnGameLoad += LoadStateFromManager;)
    }

    // --- NEW: Method for Valves/Switches to call ---
    public void UnlockByMechanism()
    {
        isLocked = false;
        if (inventorySystem != null)
        {
            inventorySystem.ShowFeedback("Mechanism active. Hatch unlocked.");
        }
        // Save the unlocked state if necessary (e.g., calling SaveManager.SaveGame after this action)
    }
    // -----------------------------------------------

    /// <summary>
    /// Attempts to use an item from the inventory on this door (e.g., a key).
    /// </summary>
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
                
                // You might want to trigger a save here if unlocking is a permanent, non-reversible state change
                
                return true; // Item consumed
            }
            else
            {
                if (inventorySystem != null) 
                    inventorySystem.ShowFeedback($"Wrong Key. This door requires the key named: {requiredKeyName}.");
                return false; // Wrong item, do not consume
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

        // Always use a positive sign (1f) so dragging right (positive xInput) opens the door.
        // We use interactionSpeed as a multiplier to the player's input
        targetRotationY += xInput * interactionSpeed * Time.deltaTime * 1f; 
        
        // Clamp between 0 (closed) and 1 (fully open)
        targetRotationY = Mathf.Clamp(targetRotationY, 0f, 1f); 
        
        // Interpolate between startRotation and the single, fixed openRotation
        Quaternion targetRot = Quaternion.Lerp(startRotation, openRotation, targetRotationY);
        pivot.localRotation = targetRot;
    }
    
    public void StopInteract()
    {
        isInteracting = false;
        // Optionally, you might trigger a save here since the state has changed
    }
    
    void Update()
    {
        // Penumbra-style doors: no snapping logic here.
        // The rotation is applied directly in UpdateInteraction, but we need to ensure 
        // the door moves smoothly if the player stops interacting mid-swing.
        // If you don't use UpdateInteraction (e.g., mouse isn't dragging), 
        // the rotation will just hold its last position, which is the Penumbra style.
    }
    
    // =========================================================================
    //                            SAVE / LOAD METHODS
    // =========================================================================

    /// <summary>
    /// Returns the door's current state for saving.
    /// </summary>
    public SaveData.SavedDoorState GetSaveState()
    {
        // Save the current unclamped rotation ratio (0 to 1) and the lock state
        return new SaveData.SavedDoorState(doorID, targetRotationY, isLocked);
    }

    /// <summary>
    /// Loads the door's state from the saved data structure.
    /// </summary>
    public void LoadState(SaveData.SavedDoorState savedState)
    {
        if (savedState.doorID != doorID) return; // Safety check

        // 1. Load the rotation ratio
        targetRotationY = savedState.openRatio;
        
        // 2. Apply the rotation instantly to match the saved state
        Quaternion loadedRotation = Quaternion.Lerp(startRotation, openRotation, targetRotationY);
        pivot.localRotation = loadedRotation;
        
        // 3. Load the lock state
        isLocked = savedState.isLocked;
        
        Debug.Log($"Loaded state for Door: {doorID}. Open Ratio: {targetRotationY}, Locked: {isLocked}");
    }
}