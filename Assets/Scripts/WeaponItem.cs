using UnityEngine;

/// <summary>
/// WeaponItem - A melee weapon with state-based swing mechanics.
/// Hold left click and move mouse to wind up, then swing back to attack.
/// </summary>
public class WeaponItem : PickableItem
{
    // States: Idle -> Swing (winding up) -> Attack (striking) -> Idle
    public enum WeaponState
    {
        Idle,
        Swing,   // Winding up (moving hammer to side)
        Attack   // Striking (swinging through)
    }
    
    [Header("Weapon Settings")]
    [Tooltip("Damage dealt when hitting an enemy.")]
    public int damage = 50;
    
    [Tooltip("Layer mask for objects that can be hit by the weapon.")]
    public LayerMask hitLayers;
    
    [Header("Swing Mechanics")]
    [Tooltip("Position offset when wound up ready to attack.")]
    public Vector3 windupPositionOffset = new Vector3(-0.4f, 0.1f, 0.5f);
    
    [Tooltip("Position offset at rest (idle).")]
    public Vector3 idlePositionOffset = new Vector3(0f, -0.25f, -0.1f);
    
    [Tooltip("Rotation of weapon when held (idle).")]
    public Vector3 idleRotation = new Vector3(-25f, 180f, 90f);
    
    [Tooltip("Position at screen center during attack (in parent space). Adjusted so top of weapon hits crosshair.")]
    public Vector3 attackCenterPosition = new Vector3(0f, -0.2f, 0.5f);
    
    [Tooltip("Speed of position animation.")]
    public float positionSpeed = 5f;
    
    [Tooltip("Mouse movement threshold to start winding up.")]
    public float mouseThreshold = 0.5f;
    
    [Tooltip("Cooldown after completing an attack.")]
    public float attackCooldown = 0.5f;
    
    [Header("Audio")]
    [Tooltip("Sound played when winding up.")]
    public AudioClip windupSound;
    
    [Tooltip("Sound played when attacking.")]
    public AudioClip attackSound;
    
    [Tooltip("Sound played when hitting an object.")]
    public AudioClip hitSound;
    
    private WeaponState currentState = WeaponState.Idle;
    private AudioSource audioSource;
    private Rigidbody rb;
    
    // Position tracking
    private Vector3 currentPosition;
    private Vector3 targetPosition;
    private int windupDirection = 0; // -1 for left, 1 for right, 0 for none
    private bool isSwingFullyWoundUp = false; // Tracks if swing reached max position
    
    // Cooldown
    private float cooldownTimer = 0f;
    private bool isOnCooldown = false;
    private bool isInteractHeld = false;
    
    // Camera unlock delay
    private float cameraUnlockDelay = 0.3f;
    private float cameraUnlockTimer = 0.5f;
    private bool waitingToUnlockCamera = false;
    
    // Equipped state
    private bool isEquipped = false;
    
    // Reference to parent ItemHolder
    private Transform itemHolder;
    
    protected override void Start()
    {
        base.Start();
        
        // Weapons can be stored in inventory
        isStorable = true;
        canBeUsedFromInventory = true;
        
        // Get rigidbody
        rb = GetComponent<Rigidbody>();
        
        // Get or add audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
        
        // Initialize to Idle state
        currentState = WeaponState.Idle;
        windupDirection = 0;
        
        // Ensure rigidbody is kinematic at start
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }
    
    void Update()
    {
        // Handle camera unlock delay
        if (waitingToUnlockCamera)
        {
            cameraUnlockTimer += Time.deltaTime;
            if (cameraUnlockTimer >= cameraUnlockDelay)
            {
                FirstPersonController fpc = FindFirstObjectByType<FirstPersonController>();
                if (fpc != null)
                {
                    fpc.SetCameraLocked(false);
                }
                waitingToUnlockCamera = false;
                cameraUnlockTimer = 0f;
            }
        }
        
        // Handle cooldown
        if (isOnCooldown)
        {
            cooldownTimer += Time.deltaTime;
            if (cooldownTimer >= attackCooldown)
            {
                isOnCooldown = false;
                cooldownTimer = 0f;
            }
            return;
        }
        
        // Only update weapon position/rotation when equipped
        if (!isEquipped) return;
        
        // Check if interact is released during Swing state
        if (currentState == WeaponState.Swing && !isInteractHeld)
        {
            // Interrupt swing and return to idle
            currentState = WeaponState.Idle;
            windupDirection = 0;
            isSwingFullyWoundUp = false;
            
            // Unlock camera
            FirstPersonController fpc = FindFirstObjectByType<FirstPersonController>();
            if (fpc != null)
            {
                fpc.SetCameraLocked(false);
            }
        }
        
        // Update position based on current state
        switch (currentState)
        {
            case WeaponState.Idle:
                // Smoothly return to idle position
                targetPosition = idlePositionOffset;
                currentPosition = Vector3.Lerp(currentPosition, targetPosition, Time.deltaTime * positionSpeed);
                // Keep idle rotation
                transform.localRotation = Quaternion.Euler(idleRotation);
                break;
                
            case WeaponState.Swing:
                // Move toward windup position
                currentPosition = Vector3.MoveTowards(currentPosition, targetPosition, positionSpeed * Time.deltaTime);
                // Keep idle rotation during swing
                transform.localRotation = Quaternion.Euler(idleRotation);
                
                // Check if reached max windup
                if (Vector3.Distance(currentPosition, targetPosition) < 0.01f)
                {
                    // Fully wound up - ready to attack
                    isSwingFullyWoundUp = true;
                }
                break;
                
            case WeaponState.Attack:
                // Move weapon towards center of screen (top of weapon hits crosshair)
                currentPosition = Vector3.MoveTowards(currentPosition, attackCenterPosition, positionSpeed * 2f * Time.deltaTime);
                // Keep idle rotation during attack
                transform.localRotation = Quaternion.Euler(idleRotation);
                
                // Check if swing completed
                if (Vector3.Distance(currentPosition, attackCenterPosition) < 0.01f)
                {
                    EndAttack();
                }
                break;
        }
        
        // Apply position to weapon's local transform
        transform.localPosition = currentPosition;
    }
    
    /// <summary>
    /// Called by FirstPersonController when left click is held and mouse moves.
    /// </summary>
    public void OnMouseMove(Vector2 mouseDelta)
    {
        if (isOnCooldown) return;
        
        float horizontalMovement = mouseDelta.x;
        
        // Check for significant horizontal mouse movement
        if (Mathf.Abs(horizontalMovement) < mouseThreshold) return;
        
        int moveDirection = horizontalMovement > 0 ? 1 : -1; // 1 = right, -1 = left
        
        switch (currentState)
        {
            case WeaponState.Idle:
                // Only allow starting windup by moving right AND holding interact
                if (moveDirection > 0 && isInteractHeld)
                {
                    Debug.Log("Start windup");
                    StartWindup(moveDirection);
                }
                break;
                
            case WeaponState.Swing:
                // Only trigger attack when moving left AND interact is held AND swing is fully wound up
                if (moveDirection < 0 && isInteractHeld && isSwingFullyWoundUp)
                {
                    Debug.Log("Start attack");
                    StartAttack();
                }
                // Otherwise, continue winding up further if not at max
                break;
                
            case WeaponState.Attack:
                // Already attacking, ignore input
                break;
        }
    }
    
    /// <summary>
    /// Called when left click is released.
    /// </summary>
    public void OnMouseRelease()
    {
        if (currentState == WeaponState.Swing)
        {
            // Released before attacking - return to idle
            currentState = WeaponState.Idle;
            windupDirection = 0;
            
            // Unlock camera
            FirstPersonController fpc = FindFirstObjectByType<FirstPersonController>();
            if (fpc != null)
            {
                Debug.Log("Cam unlocked");
                fpc.SetCameraLocked(false);
            }
        }
    }
    
    /// <summary>
    /// Called by FirstPersonController to set interact button state.
    /// </summary>
    public void SetInteractHeld(bool held)
    {
        isInteractHeld = held;
    }
    
    private void StartWindup(int direction)
    {
        currentState = WeaponState.Swing;
        windupDirection = direction;
        isSwingFullyWoundUp = false;
        
        // Lock camera during windup
        FirstPersonController fpc = FindFirstObjectByType<FirstPersonController>();
        if (fpc != null)
        {
            fpc.SetCameraLocked(true);
        }
        
        // Set target position for windup
        // Moving left (direction = -1): go to left-high position
        // Moving right (direction = 1): go to right-high position
        if (direction < 0)
        {
            targetPosition = new Vector3(-0.5f, -0.1f, -0.1f); // Left side, raised
        }
         else
        {
            targetPosition = new Vector3(0.5f, -0.1f, -0.1f); // Right side, raised
        } 
        
        // Play windup sound
        if (windupSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(windupSound);
        }
    }
    
    private void StartAttack()
    {
        currentState = WeaponState.Attack;
        
        // Target is center of screen for precise aiming
        targetPosition = attackCenterPosition;
        
        // Lock camera during attack
        FirstPersonController fpc = FindFirstObjectByType<FirstPersonController>();
        if (fpc != null)
        {
            fpc.SetCameraLocked(true);
        }
        
        // Make rigidbody non-kinematic for physics during attack
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            
            // Apply force for physics-based collision
            Vector3 forceDirection = transform.TransformDirection(new Vector3(-windupDirection, 0, 0));
            rb.AddForce(forceDirection * 15f, ForceMode.Impulse);
        }
        
        // Play attack sound
        if (attackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
    }
    
    private void EndAttack()
    {
        currentState = WeaponState.Idle;
        windupDirection = 0;
        isOnCooldown = true;
        cooldownTimer = 0f;
        
        // Reset velocities before making kinematic
        if (rb != null)
        {
            rb.angularVelocity = Vector3.zero;
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        
        // Start camera unlock delay
        waitingToUnlockCamera = true;
        cameraUnlockTimer = 0f;
    }
    
    public void SetEquipped(bool equipped)
    {
        isEquipped = equipped;
        
        if (equipped)
        {
            // Initialize position to idle offset
            currentPosition = idlePositionOffset;
            targetPosition = idlePositionOffset;
            
            // Set idle rotation
            transform.localRotation = Quaternion.Euler(idleRotation);
            transform.localPosition = currentPosition;
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Only deal damage during attack state
        if (currentState != WeaponState.Attack) return;
        
        // Check if we hit something in the valid layers
        if (((1 << collision.gameObject.layer) & hitLayers) == 0) return;
        
        // Check for enemy
        EnemyAI enemy = collision.gameObject.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            DamageEnemy(enemy);
            
            // Play hit sound
            if (hitSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(hitSound);
            }
            return;
        }
        
        // Hit something else (environmental object)
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound, 0.5f);
        }
    }
    
    private void DamageEnemy(EnemyAI enemy)
    {
        enemy.TakeDamage(damage);
        Debug.Log($"Hit enemy with {itemName} for {damage} damage!");
        
        // Apply knockback
        Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
        if (enemyRb != null)
        {
            Vector3 knockbackDir = (enemy.transform.position - transform.position).normalized;
            enemyRb.AddForce(knockbackDir * 10f, ForceMode.Impulse);
        }
    }
    
    /// <summary>
    /// Called when weapon is used from inventory - equips it.
    /// </summary>
    public override bool OnUse(InventorySystem inventory)
    {
        // Equipment is handled by InventorySystem PerformItemAction
        return false;
    }
    
    public WeaponState GetCurrentState()
    {
        Debug.Log($"Weapon State: {currentState}");
        return currentState;
    }
    
    public bool IsOnCooldown()
    {
        return isOnCooldown;
    }
    
    void OnDrawGizmosSelected()
    {
        // Visual feedback for weapon state
        switch (currentState)
        {
            case WeaponState.Idle:
                Gizmos.color = Color.green;
                break;
            case WeaponState.Swing:
                Gizmos.color = Color.yellow;
                break;
            case WeaponState.Attack:
                Gizmos.color = Color.red;
                break;
        }
        
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}
