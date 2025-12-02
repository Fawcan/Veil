using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DestructibleObject : MonoBehaviour, IDestructible
{
    [Tooltip("The kinetic energy (Mass * Velocity^2) required to smash the object.")]
    public float destructionThreshold = 100f;

    [Header("Effects")]
    [Tooltip("The object to show when the window is destroyed (e.g., shattered pieces).")]
    public GameObject destroyedPrefab;
    
    public float DestructionThreshold => destructionThreshold;

    void OnCollisionEnter(Collision collision)
    {
        // Check if the colliding object is a PickableItem with a Rigidbody (i.e., thrown)
        PickableItem item = collision.gameObject.GetComponent<PickableItem>();
        Rigidbody rb = collision.rigidbody;

        if (item != null && rb != null)
        {
            // Calculate the kinetic energy of the incoming item (0.5 * Mass * Velocity^2)
            // We use the square of the magnitude of the velocity
            float kineticEnergy = 0.5f * rb.mass * rb.linearVelocity.sqrMagnitude;
            
            Debug.Log($"Hit detected! Item: {item.itemName}, Energy: {kineticEnergy} vs Threshold: {destructionThreshold}");

            if (kineticEnergy >= destructionThreshold)
            {
                OnSmashHit(collision, kineticEnergy);
            }
        }
    }

    public void OnSmashHit(Collision collision, float calculatedForce)
    {
        Debug.Log("Window Smashed!");
        
        // 1. Instantiate destruction effect
        if (destroyedPrefab != null)
        {
            // Spawn the shattered pieces at the hit location
            Instantiate(destroyedPrefab, transform.position, transform.rotation);
        }

        // 2. Optional: Add particle effects or sound here

        // 3. Destroy the original window object
        Destroy(gameObject);
    }
}