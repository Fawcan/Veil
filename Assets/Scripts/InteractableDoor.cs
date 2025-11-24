using UnityEngine;

public class InteractableDoor : MonoBehaviour
{
    [Header("Door Settings")]
    [Tooltip("The axis the door will rotate around in its local space.")]
    public Vector3 rotationAxis = Vector3.up;
    [Tooltip("The maximum angle (in degrees) the door can open.")]
    public float maxAngle = 120.0f;
    [Tooltip("The minimum angle (in degrees) the door can be.")]
    public float minAngle = 0.0f;
    [Tooltip("How sensitive the door is to mouse movement.")]
    public float sensitivity = 0.1f; // Reduced sensitivity for X-axis

    [Header("Physics (Optional)")]
    [Tooltip("If set, the door will be kinematic when held and non-kinematic when released.")]
    public Rigidbody doorRigidbody;
    
    private bool isBeingHeld = false;
    private float currentAngle = 0.0f;
    private Quaternion initialRotation;
    
    void Start()
    {
        initialRotation = transform.localRotation;
        
        if (doorRigidbody == null)
        {
            doorRigidbody = GetComponent<Rigidbody>();
        }

        currentAngle = minAngle;
        transform.localRotation = initialRotation * Quaternion.Euler(rotationAxis * currentAngle);
    }

    public void StartInteract()
    {
        isBeingHeld = true;
        if (doorRigidbody != null)
        {
            doorRigidbody.isKinematic = true;
        }
    }

    public void StopInteract()
    {
        isBeingHeld = false;
        if (doorRigidbody != null)
        {
            doorRigidbody.isKinematic = false;
        }
    }

    /// <summary>
    /// Called by the PlayerController's Update loop every frame while held.
    /// --- MODIFIED to use mouseInputX ---
    /// </summary>
    /// <param name="mouseInputX">The raw horizontal mouse delta.</param>
    public void UpdateInteraction(float mouseInputX)
    {
        if (!isBeingHeld) return;

        // Moving mouse "right" (positive X) opens the door (positive angle)
        float rotationAmount = mouseInputX * sensitivity; 
        
        // Add to the current angle
        currentAngle += rotationAmount;

        // Clamp the angle between min and max
        currentAngle = Mathf.Clamp(currentAngle, minAngle, maxAngle);

        // Apply the rotation
        transform.localRotation = initialRotation * Quaternion.Euler(rotationAxis * currentAngle);
    }
}