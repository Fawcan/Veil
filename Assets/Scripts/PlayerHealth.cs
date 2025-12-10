using UnityEngine;

/// <summary>
/// PlayerHealth - Handles player health and damage.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [Tooltip("Maximum health points.")]
    public int maxHealth = 100;
    
    [Tooltip("Current health points.")]
    public int currentHealth;

    [Header("Damage Feedback")]
    [Tooltip("Time player is invulnerable after taking damage.")]
    public float invulnerabilityTime = 0.5f;

    private float invulnerabilityTimer = 0f;
    private InventorySystem inventory;

    void Start()
    {
        currentHealth = maxHealth;
        inventory = FindFirstObjectByType<InventorySystem>();
    }

    void Update()
    {
        if (invulnerabilityTimer > 0f)
        {
            invulnerabilityTimer -= Time.deltaTime;
        }
    }

    public void TakeDamage(int damage)
    {
        if (invulnerabilityTimer > 0f) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);
        invulnerabilityTimer = invulnerabilityTime;

        if (inventory != null)
        {
            inventory.ShowFeedback($"Took {damage} damage! Health: {currentHealth}/{maxHealth}");
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        if (inventory != null)
        {
            inventory.ShowFeedback($"Healed {amount} HP. Health: {currentHealth}/{maxHealth}");
        }
    }

    private void Die()
    {
        if (inventory != null)
        {
            inventory.ShowFeedback("You died!");
        }
        
        Debug.Log("Player died!");
        // Add death logic here (reload scene, show death screen, etc.)
    }

    public bool IsAlive()
    {
        return currentHealth > 0;
    }
}
