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
        base.PickUp(holder);
        isCableHeld = true;
        
        // Re-enable collider for trigger detection with cable slots
        if (cableEndRigidbody != null)
        {
            Collider col = cableEndRigidbody.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = true;
            }
        }
    }
    
    public new void Drop(Transform cameraTransform, float force)
    {
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
        }
        
        // Ensure cable end has a collider for trigger detection
        Collider col = cableEndRigidbody?.GetComponent<Collider>();
        if (col == null && cableEndRigidbody != null)
        {
            col = cableEndRigidbody.gameObject.AddComponent<SphereCollider>();
            ((SphereCollider)col).radius = 0.15f;
            col.isTrigger = false;
        }
        else if (col != null)
        {
        }
        
        if (cableEndRigidbody != null)
        {
        }
        
        if (cableJoint != null)
        {
            ConfigureJoint();
        }
        
        if (fixedEnd == null)
        {
        }
        
        // Verify setup
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
                
                // If we just got picked up and were attached to a slot, detach
                if (isCableHeld && IsAttachedToSlot())
                {
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
        
        if (slot == null || slot.isOccupied)
        {
            return false;
        }

        if (attachedSlot != null)
        {
            DetachFromSlot();
        }

        // Snap cable end to slot attachment point
        if (cableEndRigidbody != null && slot.attachmentPoint != null)
        {
            cableEndRigidbody.transform.position = slot.attachmentPoint.position;
            cableEndRigidbody.transform.rotation = slot.attachmentPoint.rotation;
            cableEndRigidbody.transform.SetParent(slot.attachmentPoint);
            cableEndRigidbody.isKinematic = true;
        }

        attachedSlot = slot;
        slot.AttachCable(this);
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
        
        // Auto-connect when cable end touches a slot while being held
        if (!autoConnectOnCollision)
        {
            return;
        }
        
        if (IsAttachedToSlot())
        {
            return;
        }
        
        // Check grace period after detaching
        float timeSinceDetach = Time.time - lastDetachTime;
        if (timeSinceDetach < reconnectGracePeriod)
        {
            return;
        }

        // Check if we're currently being held by the player
        if (!isCableHeld)
        {
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
            return;
        }
        
        if (slot.isOccupied)
        {
            return;
        }
        
        if (!CanReachSlot(slot))
        {
            return;
        }

        // Find the player holding this cable
        FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
        if (player == null)
        {
            return;
        }
        
        if (player.currentlyHeldItem != this)
        {
            return;
        }
        
        // Attach cable to slot
        if (AttachToSlot(slot))
        {
            // Release from player's hand
            player.currentlyHeldItem = null;
            
            if (player.inventory != null)
            {
            }
        }
        else
        {
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
