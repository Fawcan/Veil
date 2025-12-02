using UnityEngine;

public class InteractableDoor : MonoBehaviour
{
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
    // We only need one target rotation for the fixed swing direction
    private Quaternion openRotation;
    private Transform pivot;
    private bool isInteracting = false;
    private float targetRotationY;
    
    // REMOVED: For dynamic swing calculation (no longer needed)
    // private Transform playerCamera;
    // private float swingSign = 1f; 
    // private Quaternion currentOpenTargetRotation; 

    private InventorySystem inventorySystem; 

    void Start()
    {
        pivot = transform; 
        
        startRotation = pivot.localRotation;
        
        // Calculate the single, fixed open rotation
        openRotation = startRotation * Quaternion.Euler(0, openAngle, 0);
        
        targetRotationY = 0f; 
        
        inventorySystem = FindFirstObjectByType<InventorySystem>();
        
        // REMOVED: Camera finding for positional checks
        // if (Camera.main != null)
        // {
        //     playerCamera = Camera.main.transform;
        // }

        if (requiredKeyId == 0)
        {
             Debug.LogError($"Door on object {gameObject.name} has requiredKeyId set to 0! Please set a unique ID in the Inspector.");
        }
    }

    // --- NEW: Method for Valves/Switches to call ---
    public void UnlockByMechanism()
    {
        isLocked = false;
        if (inventorySystem != null)
        {
            inventorySystem.ShowFeedback("Mechanism active. Hatch unlocked.");
        }
    }
    // -----------------------------------------------

    /// <summary>
    /// Attempts to use an item from the inventory on this door (e.g., a key).
    /// </summary>
    public bool TryUseItem(PickableItem item)
    {
        if (isLocked)
        {
            // --- NEW LOGIC: CHECK ITEM ID ---
            if (item.itemId == requiredKeyId)
            {
                isLocked = false; 
                
                if (inventorySystem != null) 
                    inventorySystem.ShowFeedback($"Used {item.itemName}. Door Unlocked! Drag to open.");
                Debug.Log($"Door unlocked successfully using Key ID {item.itemId}.");
                
                return true; // Item consumed
            }
            // --- END NEW LOGIC ---
            else
            {
                // Feedback uses the human-readable requiredKeyName
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
        
        // --- FIXED SWING LOGIC ---
        // Always targets the pre-calculated positive open rotation.
        // The mouse drag direction is also fixed (dragging right opens the door).
        // --- END FIXED SWING LOGIC ---
    }
    
    public void UpdateInteraction(float xInput)
    {
        if (!isInteracting || isLocked) return; 

        // Always use a positive sign (1f) so dragging right (positive xInput) opens the door.
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
    }
    
    void Update()
    {
        // Penumbra-style doors: no snapping logic here.
    }
}