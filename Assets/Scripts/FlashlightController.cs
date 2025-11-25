using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("Position Settings")]
    [Tooltip("The relative position offset from the camera's center.")]
    public Vector3 heldPosition = new Vector3(0.4f, -0.4f, 0.5f);
    
    private FlashlightItem currentFlashlightItem;
    private Rigidbody currentRb; // Reference for the flashlight's Rigidbody
    private bool isEquipped = false;

    public void Equip(FlashlightItem item)
    {
        currentFlashlightItem = item;
        currentRb = item.GetComponent<Rigidbody>(); // Get Rigidbody reference
        
        if (currentRb != null)
        {
            // FIX: Set velocity to zero BEFORE setting it to kinematic.
            // This stops any residual movement before physics control is handed to the transform.
            currentRb.linearVelocity = Vector3.zero; 
            currentRb.isKinematic = true; 
        }
        
        currentFlashlightItem.gameObject.SetActive(true);
        
        // Attach the item directly to this controller (which should be a rigid child of the Main Camera)
        currentFlashlightItem.transform.SetParent(transform); 
        
        // Set its local position and rotation, ensuring rigid follow
        currentFlashlightItem.transform.localPosition = heldPosition;
        currentFlashlightItem.transform.localRotation = Quaternion.identity;

        isEquipped = true;
    }

    public void Unequip()
    {
        if (currentFlashlightItem != null)
        {
            currentFlashlightItem.ToggleLight(false);
            
            // Re-enable physics simulation when unequipped/put away
            if (currentRb != null)
            {
                currentRb.isKinematic = false;
            }
            
            // Detach and hide
            currentFlashlightItem.transform.SetParent(null);
            currentFlashlightItem.gameObject.SetActive(false);
        }
        currentFlashlightItem = null;
        currentRb = null; // Clear reference
        isEquipped = false;
    }
    
    public FlashlightItem GetEquippedItem()
    {
        return currentFlashlightItem;
    }
}