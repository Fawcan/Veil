using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// CableSlot - Represents an attachment point where InteractableCable can be connected.
/// Handles cable attachment, detachment, and interaction with the player.
/// </summary>
public class CableSlot : MonoBehaviour
{
    [Header("Slot Settings")]
    [Tooltip("Unique identifier for this cable slot.")]
    public string slotID = "slot_01";
    
    [Tooltip("The transform where the cable end snaps to when attached.")]
    public Transform attachmentPoint;
    
    [Tooltip("Maximum distance for click-to-attach interaction.")]
    public float attachmentRange = 2.0f;
    
    [Tooltip("Is this slot currently occupied by a cable?")]
    public bool isOccupied = false;

    [Header("Visual Feedback")]
    [Tooltip("GameObject shown when slot is empty and available.")]
    public GameObject emptyIndicator;
    
    [Tooltip("GameObject shown when slot has a cable attached.")]
    public GameObject occupiedIndicator;

    [Header("Events")]
    [Tooltip("Invoked when a cable is successfully attached.")]
    public UnityEvent onCableAttached;
    
    [Tooltip("Invoked when a cable is detached from this slot.")]
    public UnityEvent onCableDetached;

    private InteractableCable attachedCable;

    void Start()
    {
        // Use self as attachment point if none specified
        if (attachmentPoint == null)
        {
            attachmentPoint = transform;
        }

        // Ensure we have a trigger collider for auto-connect
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<SphereCollider>();
            ((SphereCollider)col).radius = attachmentRange;
            col.isTrigger = true;
        }
        else if (!col.isTrigger)
        {
        }
        
        UpdateVisuals();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Update visuals when values change in editor
        if (Application.isPlaying)
        {
            UpdateVisuals();
        }
    }
#endif

    void OnTriggerEnter(Collider other)
    {
    }

    void OnCollisionEnter(Collision collision)
    {
    }

    /// <summary>
    /// Attach a cable to this slot.
    /// Called by InteractableCable.AttachToSlot()
    /// </summary>
    public void AttachCable(InteractableCable cable)
    {
        
        if (cable == null)
        {
            return;
        }

        if (isOccupied && attachedCable != cable)
        {
            return;
        }

        attachedCable = cable;
        isOccupied = true;

        UpdateVisuals();
        onCableAttached?.Invoke();
    }

    /// <summary>
    /// Detach the currently attached cable.
    /// Called by InteractableCable.DetachFromSlot()
    /// </summary>
    public void DetachCable()
    {
        if (!isOccupied || attachedCable == null)
        {
            return;
        }

        InteractableCable cable = attachedCable;
        attachedCable = null;
        isOccupied = false;

        UpdateVisuals();
        onCableDetached?.Invoke();
    }

    /// <summary>
    /// Get the cable currently attached to this slot.
    /// </summary>
    public InteractableCable GetAttachedCable()
    {
        return attachedCable;
    }

    /// <summary>
    /// Check if a position is within attachment range of this slot.
    /// </summary>
    public bool IsInRange(Vector3 position)
    {
        return Vector3.Distance(attachmentPoint.position, position) <= attachmentRange;
    }

    /// <summary>
    /// Interact with this slot. Called when player clicks on it.
    /// If holding a cable, attach it. If slot has cable, detach it.
    /// </summary>
    public void Interact(FirstPersonController player)
    {
        if (player == null) return;

        // If slot is occupied, allow detachment
        if (isOccupied && attachedCable != null)
        {
            // Pick up the attached cable
            if (player.currentlyHeldItem == null)
            {
                attachedCable.DetachFromSlot();
                
                // Use PickableItem mechanics to pick it up
                player.currentlyHeldItem = attachedCable;
                
                Collider[] cableColliders = attachedCable.GetComponentsInChildren<Collider>();
                foreach (Collider c in cableColliders)
                {
                    Physics.IgnoreCollision(c, player.controller, true);
                }

                Rigidbody cableRb = attachedCable.GetComponent<Rigidbody>();
                if (cableRb != null)
                {
                    cableRb.isKinematic = false;
                    cableRb.useGravity = false;
                    cableRb.linearVelocity = Vector3.zero;
                    cableRb.angularVelocity = Vector3.zero;
                    cableRb.linearDamping = 20f;
                    cableRb.angularDamping = 20f;
                    cableRb.constraints = RigidbodyConstraints.FreezeRotation;
                }

                if (player.inventory != null)
                {
                }
            }
            return;
        }

        // If player is holding a cable, attach it
        if (player.currentlyHeldItem != null)
        {
            InteractableCable heldCable = player.currentlyHeldItem.GetComponent<InteractableCable>();
            if (heldCable != null)
            {
                // Check if cable can reach this slot
                if (!heldCable.CanReachSlot(this))
                {
                    if (player.inventory != null)
                    {
                    }
                    return;
                }

                // Attach the cable
                if (heldCable.AttachToSlot(this))
                {
                    // Release from player's hand
                    player.currentlyHeldItem = null;
                    
                    if (player.inventory != null)
                    {
                    }
                }
            }
            else
            {
                if (player.inventory != null)
                {
                }
            }
        }
        else
        {
            if (player.inventory != null)
            {
            }
        }
    }

    private void UpdateVisuals()
    {
        if (emptyIndicator != null)
        {
            emptyIndicator.SetActive(!isOccupied);
        }

        if (occupiedIndicator != null)
        {
            occupiedIndicator.SetActive(isOccupied);
        }
    }

    /* void OnDrawGizmosSelected()
    {
        Transform point = attachmentPoint != null ? attachmentPoint : transform;
        
        // Draw attachment point with solid sphere
        Gizmos.color = isOccupied ? Color.green : Color.yellow;
        Gizmos.DrawSphere(point.position, 0.15f);
        
        // Draw attachment range
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(point.position, attachmentRange);
        
        // Draw forward direction (cable alignment)
        DrawArrow(point.position, point.forward, 0.4f, Color.blue);
        
        // Draw slot transform arrow
        DrawArrow(transform.position, transform.forward, 0.3f, Color.cyan);
    }

    private void DrawArrow(Vector3 position, Vector3 direction, float length, Color color)
    {
        if (direction.sqrMagnitude < 0.01f) return;
        
        Gizmos.color = color;
        Vector3 endPoint = position + direction * length;
        
        // Draw shaft
        Gizmos.DrawLine(position, endPoint);
        
        // Draw arrow head
        float arrowHeadLength = length * 0.25f;
        float arrowHeadWidth = length * 0.15f;
        
        Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
        if (right.sqrMagnitude < 0.01f)
        {
            right = Vector3.Cross(direction, Vector3.right).normalized;
        }
        Vector3 up = Vector3.Cross(right, direction).normalized;
        
        Vector3 arrowBase = endPoint - direction * arrowHeadLength;
        
        // Create cone with 8 segments
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 2f / 8f;
            Vector3 offset = (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * arrowHeadWidth;
            Gizmos.DrawLine(endPoint, arrowBase + offset);
            
            // Draw base circle
            float nextAngle = (i + 1) * Mathf.PI * 2f / 8f;
            Vector3 nextOffset = (right * Mathf.Cos(nextAngle) + up * Mathf.Sin(nextAngle)) * arrowHeadWidth;
            Gizmos.DrawLine(arrowBase + offset, arrowBase + nextOffset);
        }
    } */
}
