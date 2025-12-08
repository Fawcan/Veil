using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class DestructibleObject : MonoBehaviour, IDestructible
{
    [Tooltip("The kinetic energy (Mass * Velocity^2) required to smash the object.")]
    public float destructionThreshold = 100f;

    [Header("Effects")]
    [Tooltip("The object to show when the window is destroyed (e.g., shattered pieces).")]
    public GameObject destroyedPrefab;

    [Header("Events")]
    [Tooltip("Event triggered when this object is destroyed.")]
    public UnityEvent onDestroyed;
    
    public float DestructionThreshold => destructionThreshold;

    private float accumulatedDamage = 0f;

    void OnCollisionEnter(Collision collision)
    {
        PickableItem item = collision.gameObject.GetComponent<PickableItem>();
        Rigidbody rb = collision.rigidbody;

        if (item != null && rb != null)
        {
            float kineticEnergy = 0.5f * rb.mass * rb.linearVelocity.sqrMagnitude;
            
            // Accumulate damage instead of requiring it all in one hit
            accumulatedDamage += kineticEnergy;
            
            Debug.Log($"Hit detected! Item: {item.itemName}, Energy: {kineticEnergy}, Total Damage: {accumulatedDamage}/{destructionThreshold}");

            if (accumulatedDamage >= destructionThreshold)
            {
                OnSmashHit(collision, accumulatedDamage);
            }
        }
    }

    public void OnSmashHit(Collision collision, float calculatedForce)
    {
        Debug.Log("Window Smashed!");
        
        if (destroyedPrefab != null)
        {
            Instantiate(destroyedPrefab, transform.position, transform.rotation);
        }

        // Trigger completion event
        onDestroyed?.Invoke();

        // 2. Optional: Add particle effects or sound here

        Destroy(gameObject);
    }
}