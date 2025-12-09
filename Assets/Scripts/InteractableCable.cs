using UnityEngine;

/// <summary>
/// InteractableCable - Represents a cable that can be picked up and attached to CableSlots.
/// The cable uses PickableItem for pickup mechanics and CharacterJoint for physics simulation.
/// </summary>
public class InteractableCable : PickableItem
{
    [Header("Cable Configuration")]
    [Tooltip("The Rigidbody on the end that can be picked up and moved.")]
    public Rigidbody cableEndRigidbody;
    
    [Tooltip("The fixed attachment point (wall socket, device, etc).")]
    public Transform fixedEnd;
    
    [Tooltip("Maximum distance the cable can stretch from fixed end.")]
    public float maxCableLength = 5.0f;

    [Header("Physics Joint Settings")]
    [Tooltip("CharacterJoint connecting cable segments.")]
    public CharacterJoint cableJoint;
    
    [Tooltip("Spring force for cable tension.")]
    public float springForce = 50f;
    
    [Tooltip("Damper for cable physics.")]
    public float damperForce = 5f;

    [Header("Slot Attachment")]
    [Tooltip("The slot this cable is currently attached to (if any).")]
    private CableSlot attachedSlot;
    
    [Header("Auto-Connect Settings")]
    [Tooltip("If true, cable will auto-connect when colliding with empty slots.")]
    public bool autoConnectOnCollision = true;
    
    [Tooltip("Time in seconds after detaching before cable can auto-connect again.")]
    public float reconnectGracePeriod = 0.5f;
    
    private bool isCableHeld = false;
    private float lastDetachTime = -999f;

    /// <summary>
    /// Override PickUp to keep collider enabled for trigger detection while held.
    /// </summary>
    public new void PickUp(Transform holder)
    {
        Debug.Log($"Cable: PickUp called");
        base.PickUp(holder);
        isCableHeld = true;
        Debug.Log($"Cable: isCableHeld set to true");
        
        // Re-enable collider for trigger detection with cable slots
        if (cableEndRigidbody != null)
        {
            Collider col = cableEndRigidbody.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = true;
                Debug.Log($"Cable: Collider re-enabled for trigger detection while held");
            }
        }
    }
    
    public new void Drop(Transform cameraTransform, float force)
    {
        Debug.Log($"Cable: Drop called");
        isCableHeld = false;
        base.Drop(cameraTransform, force);
    }

    protected override void Start()
    {
        base.Start();
        
        // Ensure cable is not storable in inventory
        isStorable = false;
        
        if (cableEndRigidbody == null)
        {
            cableEndRigidbody = GetComponent<Rigidbody>();
            Debug.Log($"Cable: cableEndRigidbody auto-assigned: {cableEndRigidbody != null}");
        }
        
        // Ensure cable end has a collider for trigger detection
        Collider col = cableEndRigidbody?.GetComponent<Collider>();
        if (col == null && cableEndRigidbody != null)
        {
            col = cableEndRigidbody.gameObject.AddComponent<SphereCollider>();
            ((SphereCollider)col).radius = 0.15f;
            col.isTrigger = false;
            Debug.LogWarning($"Cable '{itemName}' end had no collider. Added SphereCollider for collision detection.");
        }
        else if (col != null)
        {
            Debug.Log($"Cable: Collider found - Type: {col.GetType().Name}, IsTrigger: {col.isTrigger}, Enabled: {col.enabled}");
        }
        
        if (cableEndRigidbody != null)
        {
            Debug.Log($"Cable: Rigidbody - IsKinematic: {cableEndRigidbody.isKinematic}, Mass: {cableEndRigidbody.mass}, UseGravity: {cableEndRigidbody.useGravity}");
        }
        
        if (cableJoint != null)
        {
            ConfigureJoint();
        }
        
        if (fixedEnd == null)
        {
            Debug.LogWarning($"Cable '{itemName}' has no fixed end assigned!");
        }
        
        // Verify setup
        Debug.Log($"Cable '{itemName}' initialized. GameObject: {gameObject.name}, Layer: {LayerMask.LayerToName(gameObject.layer)}");
    }

    void OnCollisionEnter(Collision collision)
    {
        
    }

    void Update()
    {
        // Track if we're being held by checking if we're the currentlyHeldItem
        FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
        if (player != null)
        {
            bool shouldBeHeld = (player.currentlyHeldItem == this);
            if (shouldBeHeld != isCableHeld)
            {
                isCableHeld = shouldBeHeld;
                Debug.Log($"Cable: isCableHeld updated to {isCableHeld}");
                
                // If we just got picked up and were attached to a slot, detach
                if (isCableHeld && IsAttachedToSlot())
                {
                    Debug.Log($"Cable: Picked up while attached, detaching from slot");
                    DetachFromSlot();
                }
            }
        }
        
        // Check cable length and prevent overstretching
        if (fixedEnd != null && cableEndRigidbody != null && !IsAttachedToSlot())
        {
            float distance = Vector3.Distance(cableEndRigidbody.position, fixedEnd.position);
            
            if (distance > maxCableLength)
            {
                // Pull cable back toward fixed end
                Vector3 direction = (fixedEnd.position - cableEndRigidbody.position).normalized;
                Vector3 constrainedPosition = fixedEnd.position - direction * maxCableLength;
                cableEndRigidbody.position = Vector3.Lerp(cableEndRigidbody.position, constrainedPosition, Time.deltaTime * 10f);
                
                // Reduce velocity when at max length
                cableEndRigidbody.linearVelocity *= 0.5f;
            }
        }
    }

    private void ConfigureJoint()
    {
        if (cableJoint == null) return;
        
        // Configure the CharacterJoint spring
        SoftJointLimit limit = new SoftJointLimit();
        limit.limit = maxCableLength;
        limit.bounciness = 0f;
        limit.contactDistance = 0.1f;
        
        cableJoint.swing1Limit = limit;
        cableJoint.swing2Limit = limit;
        cableJoint.highTwistLimit = limit;
        cableJoint.lowTwistLimit = limit;
        
        // Configure spring and damper
        SoftJointLimitSpring spring = new SoftJointLimitSpring();
        spring.spring = springForce;
        spring.damper = damperForce;
        
        cableJoint.twistLimitSpring = spring;
        cableJoint.swingLimitSpring = spring;
    }

    /// <summary>
    /// Attach this cable to a cable slot.
    /// </summary>
    public bool AttachToSlot(CableSlot slot)
    {
        Debug.Log($"Cable: AttachToSlot called for slot '{slot.slotID}', isOccupied={slot.isOccupied}");
        
        if (slot == null || slot.isOccupied)
        {
            Debug.Log($"Cable: AttachToSlot failed - slot null or occupied");
            return false;
        }

        if (attachedSlot != null)
        {
            Debug.Log($"Cable: Detaching from previous slot first");
            DetachFromSlot();
        }

        // Snap cable end to slot attachment point
        if (cableEndRigidbody != null && slot.attachmentPoint != null)
        {
            cableEndRigidbody.transform.position = slot.attachmentPoint.position;
            cableEndRigidbody.transform.rotation = slot.attachmentPoint.rotation;
            cableEndRigidbody.transform.SetParent(slot.attachmentPoint);
            cableEndRigidbody.isKinematic = true;
            Debug.Log($"Cable: Snapped to attachment point");
        }

        attachedSlot = slot;
        slot.AttachCable(this);

        Debug.Log($"Cable '{itemName}' attached to slot '{slot.slotID}', slot now occupied={slot.isOccupied}");
        return true;
    }

    /// <summary>
    /// Detach this cable from its current slot.
    /// </summary>
    public void DetachFromSlot()
    {
        if (attachedSlot == null) return;

        if (cableEndRigidbody != null)
        {
            cableEndRigidbody.transform.SetParent(null);
            cableEndRigidbody.isKinematic = false;
        }

        CableSlot slot = attachedSlot;
        attachedSlot = null;
        slot.DetachCable();
        
        // Set grace period timer
        lastDetachTime = Time.time;

        Debug.Log($"Cable '{itemName}' detached from slot '{slot.slotID}', grace period active for {reconnectGracePeriod}s");
    }

    /// <summary>
    /// Check if cable is currently attached to a slot.
    /// </summary>
    public bool IsAttachedToSlot()
    {
        return attachedSlot != null;
    }

    /// <summary>
    /// Get the currently attached slot.
    /// </summary>
    public CableSlot GetAttachedSlot()
    {
        return attachedSlot;
    }

    /// <summary>
    /// Check if cable can reach a specific slot.
    /// </summary>
    public bool CanReachSlot(CableSlot slot)
    {
        if (slot == null || fixedEnd == null) return false;
        
        float distanceToSlot = Vector3.Distance(fixedEnd.position, slot.attachmentPoint.position);
        return distanceToSlot <= maxCableLength;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"CABLE TRIGGER ENTER: {other.name}, isCableHeld={isCableHeld}");
        
        // Auto-connect when cable end touches a slot while being held
        if (!autoConnectOnCollision)
        {
            Debug.Log($"Cable: Auto-connect disabled");
            return;
        }
        
        if (IsAttachedToSlot())
        {
            Debug.Log($"Cable: Already attached to slot");
            return;
        }
        
        // Check grace period after detaching
        float timeSinceDetach = Time.time - lastDetachTime;
        if (timeSinceDetach < reconnectGracePeriod)
        {
            Debug.Log($"Cable: Grace period active ({reconnectGracePeriod - timeSinceDetach:F2}s remaining)");
            return;
        }

        // Check if we're currently being held by the player
        if (!isCableHeld)
        {
            Debug.Log($"Cable: Not being held by player, isCableHeld={isCableHeld}");
            return;
        }

        // Try to find CableSlot on the collider or its parents
        CableSlot slot = other.GetComponent<CableSlot>();
        if (slot == null)
        {
            slot = other.GetComponentInParent<CableSlot>();
        }
        if (slot == null)
        {
            slot = other.GetComponentInChildren<CableSlot>();
        }
        
        if (slot == null)
        {
            Debug.Log($"Cable: Collided with {other.name} (GameObject: {other.gameObject.name}) but no CableSlot found on it, parent, or children");
            return;
        }
        
        Debug.Log($"Cable: Found slot '{slot.slotID}', occupied={slot.isOccupied}");
        
        if (slot.isOccupied)
        {
            Debug.Log($"Cable: Slot is occupied");
            return;
        }
        
        if (!CanReachSlot(slot))
        {
            Debug.Log($"Cable: Cannot reach slot (too far from fixed end)");
            return;
        }

        // Find the player holding this cable
        FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
        if (player == null)
        {
            Debug.Log($"Cable: No player found");
            return;
        }
        
        if (player.currentlyHeldItem != this)
        {
            Debug.Log($"Cable: Player not holding this cable (holding: {player.currentlyHeldItem?.itemName ?? "nothing"})");
            return;
        }

        Debug.Log($"Cable: Attempting to attach to slot '{slot.slotID}'");
        
        // Attach cable to slot
        if (AttachToSlot(slot))
        {
            // Release from player's hand
            player.currentlyHeldItem = null;
            
            if (player.inventory != null)
            {
                player.inventory.ShowFeedback($"Cable connected!");
            }
            
            Debug.Log($"Cable: Successfully attached!");
        }
        else
        {
            Debug.Log($"Cable: AttachToSlot returned false");
        }
    }

/*     void OnDrawGizmosSelected()
    {
        // Draw cable length indicator
        if (fixedEnd != null && cableEndRigidbody != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(fixedEnd.position, cableEndRigidbody.position);
            
            // Draw max length sphere
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawWireSphere(fixedEnd.position, maxCableLength);
            
            // Draw fixed end
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(fixedEnd.position, 0.1f);
            
            // Draw pickup end
            Gizmos.color = IsAttachedToSlot() ? Color.green : Color.cyan;
            Gizmos.DrawWireSphere(cableEndRigidbody.position, 0.1f);
        }
    } */
}