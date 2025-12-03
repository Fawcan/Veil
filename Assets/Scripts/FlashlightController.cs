using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("Position Settings")]
    [Tooltip("The relative position offset from the camera's center.")]
    public Vector3 itemEquipOffset = new Vector3(0.4f, -0.4f, 0.5f);
    
    [Header("Movement Sway/Bob Settings")]
    [Tooltip("The speed of the bobbing motion.")]
    public float bobFrequency = 1.5f;
    [Tooltip("The maximum side-to-side (X) movement.")]
    public float bobAmplitudeX = 0.005f;
    [Tooltip("The maximum up-down (Y) movement.")]
    public float bobAmplitudeY = 0.01f;
    [Tooltip("How quickly the light returns to its base position.")]
    public float smoothing = 10f;

    [Header("Dependencies")]
    // The FPC is needed to read the player's movement input.
    public FirstPersonController fpc; 
    
    // The current item equipped in the light slot, regardless of whether it's a flashlight or glowstick.
    private PickableItem currentEquippedItem; 
    private Rigidbody currentRb; 
    private bool isEquipped = false;
    
    private Vector3 originalPosition; 
    private float bobTime; 

    void Start()
    {
        // Store the base position from the Editor/Scene setup
        originalPosition = transform.localPosition;
        
        if (fpc == null)
        {
            // Fallback to finding it in parent/scene if not set
            fpc = GetComponentInParent<FirstPersonController>(); 
            if (fpc == null) Debug.LogError("FlashlightController requires a reference to FirstPersonController and should be a child of the camera.");
        }
    }

    void Update()
    {
        if (fpc != null)
        {
            ApplyBob();
        }
    }

    void ApplyBob()
    {
        if (!isEquipped)
        {
            // If nothing is equipped, smooth back to original position
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, Time.deltaTime * smoothing);
            bobTime = 0f;
            return;
        }

        // Get the player's movement input strength (0 to 1)
        float speed = fpc.GetMoveInputMagnitude();
        
        Vector3 targetPosition = originalPosition;
        
        if (speed > 0.01f) // Player is moving
        {
            // Advance bob time based on speed
            bobTime += Time.deltaTime * bobFrequency * speed;
            
            // Calculate X (Side-to-Side) Bob: Uses a sine wave
            float bobX = Mathf.Sin(bobTime) * bobAmplitudeX;
            
            // Calculate Y (Up-Down) Bob: Uses a cosine wave for vertical motion, 
            // and uses the absolute value to make the bobbing more "up-down" (like walking steps)
            float bobY = Mathf.Abs(Mathf.Cos(bobTime)) * bobAmplitudeY;
            
            // Apply the bob offset to the target position
            targetPosition.x += bobX;
            targetPosition.y += bobY;
        }
        else // Player is stationary
        {
            // Reset bobTime gradually when movement stops
            bobTime = Mathf.Lerp(bobTime, 0f, Time.deltaTime * smoothing);
        }
        
        // Smoothly move the controller to the calculated target position
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * smoothing);
    }
    
    // ... (EquipLightItem, UnequipCurrentLight, and GetEquippedItem methods are unchanged)
    
    /// <summary>
    /// Equips a generic light item (Flashlight or Glowstick) into the held slot.
    /// </summary>
    public void EquipLightItem(PickableItem item)
    {
        // 1. Ensure any currently held item is unequipped first for clean transition
        UnequipCurrentLight(); 

        currentEquippedItem = item;
        currentRb = item.GetComponent<Rigidbody>(); 
        
        if (currentRb != null)
        {
            currentRb.linearVelocity = Vector3.zero; 
            currentRb.isKinematic = true; 
        }
        
        currentEquippedItem.gameObject.SetActive(true);
        currentEquippedItem.transform.SetParent(transform); 
        
        // This is where we apply the offset relative to the FlashlightController's position
        currentEquippedItem.transform.localPosition = itemEquipOffset;
        currentEquippedItem.transform.localRotation = Quaternion.identity;

        isEquipped = true;
    }

    /// <summary>
    /// Unequips whatever light item is currently held.
    /// </summary>
    public void UnequipCurrentLight()
    {
        if (currentEquippedItem != null)
        {
            // Try to turn off the light before storing/dropping it
            FlashlightItem flItem = currentEquippedItem.GetComponent<FlashlightItem>();
            if (flItem != null) flItem.ToggleLight(false);
            
            GlowstickItem gsItem = currentEquippedItem.GetComponent<GlowstickItem>();
            if (gsItem != null) gsItem.ToggleLight(false);
            
            // Re-enable physics simulation when unequipped/put away
            if (currentRb != null)
            {
                currentRb.isKinematic = false;
            }
            
            // Detach and hide
            currentEquippedItem.transform.SetParent(null);
            currentEquippedItem.gameObject.SetActive(false);
        }
        currentEquippedItem = null;
        currentRb = null; 
        isEquipped = false;
    }
    
    /// <summary>
    /// Returns the currently equipped light item (Flashlight or Glowstick)
    /// </summary>
    public PickableItem GetEquippedItem()
    {
        return currentEquippedItem;
    }
}