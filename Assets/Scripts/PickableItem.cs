using UnityEngine;

public class PickableItem : MonoBehaviour
{
    private Rigidbody rb;
    private Collider col;
    private bool isPickedUp = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        
        if (rb == null || col == null)
        {
            Debug.LogError("PickableItem needs a Rigidbody and Collider!", this);
        }
    }

    /// <summary>
    /// Sets the item's state to be held by the camera.
    /// </summary>
    public void PickUp(Transform parent)
    {
        if (isPickedUp) return;

        isPickedUp = true;
        
        // Disable physics interaction
        rb.isKinematic = true;
        col.enabled = false;

        // Set the item to move with the parent (the camera)
        transform.SetParent(parent);

        // Position the item in front of the camera
        transform.localPosition = new Vector3(0.5f, -0.5f, 1.5f);
        transform.localRotation = Quaternion.identity; 
    }

    /// <summary>
    /// Restores the item's physics and releases it from the player.
    /// </summary>
    public void Drop(Transform playerCam, float force)
    {
        if (!isPickedUp) return;

        isPickedUp = false;
        
        // 1. Clear the parent first
        transform.SetParent(null); 

        // 2. Restore physics interaction
        col.enabled = true;
        rb.isKinematic = false;
        
        // 3. Apply a small force forward (optional, but helps with placing)
        rb.AddForce(playerCam.forward * force, ForceMode.VelocityChange);

        Debug.Log($"Dropped {gameObject.name}.");
    }
}