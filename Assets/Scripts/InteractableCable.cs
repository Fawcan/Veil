using UnityEngine;

public class InteractableCable : MonoBehaviour
{
    [Header("Cable Setup")]
    // The Rigidbody component on the end the player picks up.
    [Tooltip("The Rigidbody component on the end the player picks up.")]
    public Rigidbody pickupEnd; 
    
    // The Transform/Rigidbody for the fixed end of the cable (e.g., the wall outlet).
    [Tooltip("The fixed position the cable is attached to.")]
    public Transform fixedAttachmentPoint; 
    
    [Header("Drag Limits")]
    [Tooltip("The maximum distance the pickup end can be pulled from the fixed point before the player loses grip.")]
    public float maxDragDistance = 3.0f; 

    private bool isBeingPulled = false;
    private Rigidbody playerHeldBody; // Reference to the object holding the cable end (FPC's heldItemHolder)

    /// <summary>
    /// Called by the FPC when the player grabs the cable end.
    /// </summary>
    /// <param name="holder">The transform the player uses to hold objects.</param>
    public void StartInteract(Transform holder)
    {
        if (pickupEnd == null) return;
        
        isBeingPulled = true;
        playerHeldBody = holder.GetComponent<Rigidbody>();
        
        if (playerHeldBody == null)
        {
            // You should have a Rigidbody on your FPC's heldItemHolder to use joints, 
            // but for simple movement, we just need the transform.
            Debug.LogWarning("FPC heldItemHolder is missing Rigidbody. Cable physics will be simplified.");
        }

        // 1. Move the pickup end to the player's holder transform
        pickupEnd.transform.SetParent(holder);
        pickupEnd.transform.localPosition = Vector3.zero;
        pickupEnd.isKinematic = true; // Stop cable end from reacting to world physics
        
        // Optional: Attach a FixedJoint or configurable joint between the holder and the cable end
        // This is complex and usually done in the FPC, but for now, we rely on parenting.
    }

    /// <summary>
    /// Called by the FPC when the player stops interacting or is pulled too far.
    /// </summary>
    public void StopInteract()
    {
        if (pickupEnd == null) return;
        
        isBeingPulled = false;
        playerHeldBody = null;

        // 1. Re-enable physics simulation
        pickupEnd.transform.SetParent(null);
        pickupEnd.isKinematic = false; 
        
        // 2. Optional: Add a small spring force or gravity pull to the cable end if desired
    }

    void Update()
    {
        if (isBeingPulled && fixedAttachmentPoint != null)
        {
            // Calculate the distance between the cable's fixed end and the currently held end
            float distance = Vector3.Distance(pickupEnd.position, fixedAttachmentPoint.position);

            if (distance > maxDragDistance)
            {
                // Break the interaction if the player pulls the cable too far
                // The FPC needs to be told to stop holding the current item.
                FirstPersonController fpc = FindFirstObjectByType<FirstPersonController>();
                if (fpc != null)
                {
                    fpc.ReleaseCurrentInteraction(); // <-- MUST be implemented in FPC
                    // The StopInteract() method above will be called as part of ReleaseCurrentInteraction()
                }
            }
        }
    }
}