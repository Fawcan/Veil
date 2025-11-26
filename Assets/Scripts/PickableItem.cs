using UnityEngine;

// Ensure this script is on all pickable items, including your keys.
public class PickableItem : MonoBehaviour
{
    [Header("Item Identification")]
    [Tooltip("Unique ID used for linking keys to doors/containers.")]
    public int itemId = 0; 
    public string itemName = "Default Item"; 
    
    [TextArea(3, 5)]
    public string itemDescription = "A generic item.";
    
    [Header("Inventory UI")]
    public Sprite icon; 
    
    [Header("Behavior")]
    [Tooltip("If true, item is added to inventory. If false, it's held and droppable.")]
    public bool isStorable = true; 
    
    // --- NEW CHECKBOX ---
    [Tooltip("If checked, the item can be removed (discarded) from the inventory.")]
    public bool canBeDiscarded = true; 
    // --------------------
    
    private bool isHeld = false;
    private Rigidbody rb;
    private Collider col;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        
        if (itemId == 0 && isStorable)
        {
            Debug.LogWarning($"Item '{itemName}' is storable but has ID 0. Make sure you set a unique ID!");
        }
    }

    public void PickUp(Transform holder)
    {
        if (isHeld) return;
        isHeld = true;

        transform.SetParent(holder);
        transform.localPosition = new Vector3(0, 0, 0.5f);
        transform.localRotation = Quaternion.identity;

        if (rb != null)
        {
            rb.isKinematic = true;
        }
        if (col != null)
        {
            col.enabled = false;
        }
    }

    public void Drop(Transform cameraTransform, float force)
    {
        isHeld = false;
        transform.SetParent(null);

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(cameraTransform.forward * force, ForceMode.VelocityChange);
        }
        if (col != null)
        {
            col.enabled = true;
        }
    }

    public bool IsHeld()
    {
        return isHeld;
    }
}