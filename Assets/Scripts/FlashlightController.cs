using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("Position Settings")]
    [Tooltip("The relative position offset from the camera's center.")]
    public Vector3 heldPosition = new Vector3(0.4f, -0.4f, 0.5f);
    
    // The current item equipped in the light slot, regardless of whether it's a flashlight or glowstick.
    private PickableItem currentEquippedItem; 
    private Rigidbody currentRb; 
    private bool isEquipped = false;

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
        
        currentEquippedItem.transform.localPosition = heldPosition;
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