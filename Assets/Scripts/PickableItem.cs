using UnityEngine;

public class PickableItem : MonoBehaviour
{
    [Header("Item Data")]
    public string itemName = "Item";
    [TextArea(3, 5)] // Allows for multi-line text in Inspector
    public string itemDescription = "Description here...";
    public Sprite icon; 
    public bool isStorable = true; 

    private Rigidbody rb;
    private Collider col;
    private bool isPickedUp = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        
        if (!isStorable && (rb == null || col == null))
        {
            Debug.LogError("Physics PickableItem needs Rigidbody and Collider!", this);
        }
    }

    public void PickUp(Transform parent)
    {
        if (isPickedUp) return;
        isPickedUp = true;
        
        if (rb != null) rb.isKinematic = true;
        if (col != null) col.enabled = false;

        transform.SetParent(parent);
        transform.localPosition = new Vector3(0.5f, -0.5f, 1.5f);
        transform.localRotation = Quaternion.identity; 
    }

    public void Drop(Transform playerCam, float force)
    {
        if (!isPickedUp) return;
        isPickedUp = false;
        
        transform.SetParent(null); 
        if (col != null) col.enabled = true;
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForce(playerCam.forward * force, ForceMode.VelocityChange);
        }
    }
}