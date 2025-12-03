using UnityEngine;

public class DamageZone : MonoBehaviour
{
    public float damageAmount = 20f;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the box is the player
        FirstPersonController player = other.GetComponent<FirstPersonController>();
        
        if (player != null)
        {
            player.TakeDamage(damageAmount);
            Debug.Log($"Player walked into damage zone! Took {damageAmount} damage.");
        }
    }
}