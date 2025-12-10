using UnityEngine;

/// <summary>
/// WeaponItem - A melee weapon that can be swung by holding interact and moving the mouse.
/// Similar to the attack mechanic in Penumbra: Overture.
/// </summary>
public class WeaponItem : PickableItem
{
    [Header("Weapon Settings")]
    [Tooltip("Damage dealt when hitting an enemy.")]
    public int damage = 50;
    
    [Tooltip("Minimum mouse movement speed required to trigger a swing.")]
    public float swingThreshold = 2f;
    
    [Tooltip("Force multiplier applied to the weapon during swing.")]
    public float swingForce = 10f;
    
    [Tooltip("Cooldown between swings in seconds.")]
    public float swingCooldown = 0.5f;
    
    [Tooltip("How long the weapon remains active after swinging (damage window).")]
    public float swingDuration = 0.3f;
    
    [Tooltip("Layer mask for objects that can be hit by the weapon.")]
    public LayerMask hitLayers;
    
    [Header("Audio")]
    [Tooltip("Sound played when swinging the weapon.")]
    public AudioClip swingSound;
    
    [Tooltip("Sound played when hitting an object.")]
    public AudioClip hitSound;
    
    private bool isSwinging = false;
    private bool canSwing = true;
    private float swingTimer = 0f;
    private float cooldownTimer = 0f;
    private Vector3 lastSwingDirection;
    private AudioSource audioSource;
    
    protected override void Start()
    {
        base.Start();
        
        // Weapons can be stored in inventory
        isStorable = true;
        canBeUsedFromInventory = true;
        
        // Get or add audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.playOnAwake = false;
    }
    
    void Update()
    {
        // Update swing timer
        if (isSwinging)
        {
            swingTimer += Time.deltaTime;
            if (swingTimer >= swingDuration)
            {
                EndSwing();
            }
        }
        
        // Update cooldown timer
        if (!canSwing)
        {
            cooldownTimer += Time.deltaTime;
            if (cooldownTimer >= swingCooldown)
            {
                canSwing = true;
                cooldownTimer = 0f;
            }
        }
    }
    
    /// <summary>
    /// Called by FirstPersonController when player is holding interact and moving mouse.
    /// </summary>
    public void TrySwing(Vector2 mouseDelta)
    {
        if (!canSwing || isSwinging) return;
        
        // Check if mouse movement exceeds threshold
        float mouseSpeed = mouseDelta.magnitude;
        if (mouseSpeed < swingThreshold) return;
        
        StartSwing(mouseDelta);
    }
    
    private void StartSwing(Vector2 mouseDelta)
    {
        isSwinging = true;
        canSwing = false;
        swingTimer = 0f;
        cooldownTimer = 0f;
        
        // Notify FirstPersonController to lock camera
        FirstPersonController fpc = FindFirstObjectByType<FirstPersonController>();
        if (fpc != null)
        {
            fpc.SetCameraLocked(true);
        }
        
        // Calculate swing direction based on mouse movement
        lastSwingDirection = new Vector3(mouseDelta.x, mouseDelta.y, 1f).normalized;
        
        // Apply force to weapon rigidbody for visual feedback
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 worldSwingDir = Camera.main.transform.TransformDirection(lastSwingDirection);
            rb.AddForce(worldSwingDir * swingForce, ForceMode.Impulse);
        }
        
        // Play swing sound
        if (swingSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(swingSound);
        }
    }
    
    private void EndSwing()
    {
        isSwinging = false;
        
        // Unlock camera
        FirstPersonController fpc = FindFirstObjectByType<FirstPersonController>();
        if (fpc != null)
        {
            fpc.SetCameraLocked(false);
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (!isSwinging) return;
        
        // Check if we hit something in the valid layers
        if (((1 << collision.gameObject.layer) & hitLayers) == 0) return;
        
        // Check for enemy
        EnemyAI enemy = collision.gameObject.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            // Deal damage to enemy
            DamageEnemy(enemy);
            
            // Play hit sound
            if (hitSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(hitSound);
            }
            
            // End swing after successful hit
            EndSwing();
            return;
        }
        
        // Hit something else (environmental object)
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound, 0.5f); // Lower volume for non-enemy hits
        }
    }
    
    private void DamageEnemy(EnemyAI enemy)
    {
        // Apply damage
        enemy.TakeDamage(damage);
        Debug.Log($"Hit enemy with {itemName} for {damage} damage!");
        
        // Apply knockback force
        Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
        if (enemyRb != null)
        {
            Vector3 knockbackDir = (enemy.transform.position - transform.position).normalized;
            enemyRb.AddForce(knockbackDir * swingForce * 0.5f, ForceMode.Impulse);
        }
    }
    
    /// <summary>
    /// Check if weapon is currently being swung.
    /// </summary>
    public bool IsSwinging()
    {
        return isSwinging;
    }
    
    /// <summary>
    /// Check if weapon can swing (not on cooldown).
    /// </summary>
    public bool CanSwing()
    {
        return canSwing;
    }
    
    /// <summary>
    /// Called when weapon is used from inventory - equips it.
    /// </summary>
    public override bool OnUse(InventorySystem inventory)
    {
        // Equipment is handled by InventorySystem PerformItemAction
        return false;
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw swing indicator when swinging
        if (isSwinging)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
        else if (canSwing)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
        else
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
