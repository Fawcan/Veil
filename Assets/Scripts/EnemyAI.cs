using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// EnemyAI - Patrols area, detects player, chases and attacks with melee.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("How far the enemy can see the player.")]
    public float detectionRange = 15f;
    
    [Tooltip("Field of view angle in degrees.")]
    public float fieldOfView = 120f;
    
    [Tooltip("Detection range multiplier when player is crouching and still.")]
    public float stealthDetectionMultiplier = 0.3f;
    
    [Tooltip("FOV multiplier when player is stealthy.")]
    public float stealthFOVMultiplier = 0.5f;
    
    [Header("Light Detection")]
    [Tooltip("Detection range multiplier when player has light source active.")]
    public float lightDetectionMultiplier = 2.5f;
    
    [Tooltip("FOV multiplier when player has light source active.")]
    public float lightFOVMultiplier = 1.5f;
    
    [Tooltip("Distance to keep from flares when investigating.")]
    public float flareStoppingDistance = 2f;
    
    [Tooltip("Layer mask for the player.")]
    public LayerMask playerLayer;
    
    [Tooltip("Layer mask for obstacles that block vision.")]
    public LayerMask obstacleLayer;

    [Header("Patrol")]
    [Tooltip("How far the enemy wanders during patrol.")]
    public float patrolRadius = 10f;
    
    [Tooltip("Time to wait at each patrol point.")]
    public float patrolWaitTime = 2f;
    
    [Tooltip("Speed while patrolling.")]
    public float patrolSpeed = 2f;

    [Header("Chase")]
    [Tooltip("Speed while chasing the player.")]
    public float chaseSpeed = 4f;
    
    [Tooltip("How close to get before attacking.")]
    public float attackRange = 2f;

    [Header("Attack")]
    [Tooltip("Damage dealt per attack.")]
    public int attackDamage = 20;
    
    [Tooltip("Time between attacks.")]
    public float attackCooldown = 1.5f;
    
    [Tooltip("Animation time for attack.")]
    public float attackDuration = 0.5f;
    
    [Header("Health")]
    [Tooltip("Enemy health points.")]
    public int maxHealth = 100;
    
    private int currentHealth;

    [Header("References")]
    [Tooltip("The player transform (will auto-find if not set).")]
    public Transform player;

    private NavMeshAgent agent;
    private EnemyState currentState = EnemyState.Patrol;
    private Vector3 patrolTarget;
    private float patrolWaitTimer = 0f;
    private float attackTimer = 0f;
    private bool isAttacking = false;
    private Vector3 startPosition;
    private bool playerIsStealthy = false;
    private float currentDetectionRange;
    private float currentFOV;
    private FlareItem nearestFlare = null;
    private float flareCheckInterval = 0.5f;
    private float flareCheckTimer = 0f;

    private enum EnemyState
    {
        Patrol,
        Chase,
        Attack
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        startPosition = transform.position;
        currentHealth = maxHealth;
        
        // Auto-find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
        
        agent.speed = patrolSpeed;
        currentDetectionRange = detectionRange;
        currentFOV = fieldOfView;
        SetNewPatrolTarget();
    }

    void Update()
    {
        if (player == null) return;

        attackTimer -= Time.deltaTime;
        
        // Check if player is stealthy and adjust detection
        FirstPersonController playerController = player.GetComponent<FirstPersonController>();
        bool hasActiveLight = false;
        
        if (playerController != null)
        {
            playerIsStealthy = playerController.IsStealthy();
            
            // Check if player has active flashlight, glowstick, or flare
            FlashlightController flashlightController = playerController.flashlightController;
            if (flashlightController != null)
            {
                PickableItem equippedItem = flashlightController.GetEquippedItem();
                if (equippedItem != null)
                {
                    FlashlightItem flashlight = equippedItem.GetComponent<FlashlightItem>();
                    GlowstickItem glowstick = equippedItem.GetComponent<GlowstickItem>();
                    FlareItem flare = equippedItem.GetComponent<FlareItem>();
                    
                    if ((flashlight != null && flashlight.IsOn()) || 
                        (glowstick != null && glowstick.IsOn()) ||
                        (flare != null && flare.IsActive()))
                    {
                        hasActiveLight = true;
                    }
                }
            }
            
            // Calculate detection based on stealth and light
            if (hasActiveLight)
            {
                // Light overrides stealth - player is very visible
                currentDetectionRange = detectionRange * lightDetectionMultiplier;
                currentFOV = fieldOfView * lightFOVMultiplier;
            }
            else if (playerIsStealthy)
            {
                // Stealthy without light - hard to detect
                currentDetectionRange = detectionRange * stealthDetectionMultiplier;
                currentFOV = fieldOfView * stealthFOVMultiplier;
            }
            else
            {
                // Normal detection
                currentDetectionRange = detectionRange;
                currentFOV = fieldOfView;
            }
        }

        switch (currentState)
        {
            case EnemyState.Patrol:
                PatrolBehavior();
                break;
            case EnemyState.Chase:
                ChaseBehavior();
                break;
            case EnemyState.Attack:
                AttackBehavior();
                break;
        }

        // Periodically check for nearby flares
        flareCheckTimer += Time.deltaTime;
        if (flareCheckTimer >= flareCheckInterval)
        {
            flareCheckTimer = 0f;
            CheckForNearbyFlares();
        }

        // Check for player detection in all states except attack
        if (currentState != EnemyState.Attack && CanSeePlayer())
        {
            currentState = EnemyState.Chase;
            agent.speed = chaseSpeed;
        }
        // If no player detected but there's a visible flare, investigate it
        else if (currentState == EnemyState.Patrol && nearestFlare != null && CanSeeFlare(nearestFlare))
        {
            float distanceToFlare = Vector3.Distance(transform.position, nearestFlare.transform.position);
            
            // Only move toward flare if not already close enough
            if (distanceToFlare > flareStoppingDistance)
            {
                agent.SetDestination(nearestFlare.transform.position);
            }
            else
            {
                // Stop and look at the flare
                agent.ResetPath();
                Vector3 directionToFlare = (nearestFlare.transform.position - transform.position).normalized;
                directionToFlare.y = 0;
                if (directionToFlare != Vector3.zero)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(directionToFlare);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 0.5f);
                }
            }
        }
    }

    private void PatrolBehavior()
    {
        // Check if reached patrol point
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            patrolWaitTimer += Time.deltaTime;
            
            if (patrolWaitTimer >= patrolWaitTime)
            {
                patrolWaitTimer = 0f;
                SetNewPatrolTarget();
            }
        }
    }

    private void ChaseBehavior()
    {
        // Update destination to player position
        agent.SetDestination(player.position);

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Check if in attack range
        if (distanceToPlayer <= attackRange)
        {
            currentState = EnemyState.Attack;
            agent.isStopped = true;
            return;
        }

        // Check if lost sight of player
        if (!CanSeePlayer())
        {
            // Return to patrol after losing sight
            currentState = EnemyState.Patrol;
            agent.speed = patrolSpeed;
            SetNewPatrolTarget();
        }
    }

    private void AttackBehavior()
    {
        // Face the player
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // If player moved out of range, resume chase
        if (distanceToPlayer > attackRange)
        {
            currentState = EnemyState.Chase;
            agent.isStopped = false;
            return;
        }

        // Perform attack if cooldown finished
        if (attackTimer <= 0f && !isAttacking)
        {
            StartAttack();
        }
    }

    private void StartAttack()
    {
        isAttacking = true;
        attackTimer = attackCooldown;

        // Deal damage to player
        FirstPersonController playerController = player.GetComponent<FirstPersonController>();
        if (playerController != null)
        {
            playerController.TakeDamage(attackDamage);
        }

        // End attack after duration
        Invoke(nameof(EndAttack), attackDuration);
    }

    private void EndAttack()
    {
        isAttacking = false;
    }

    private bool CanSeePlayer()
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Check if player is within detection range (adjusted for stealth)
        if (distanceToPlayer > currentDetectionRange)
        {
            return false;
        }

        // Check if player is within field of view (adjusted for stealth)
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        if (angleToPlayer > currentFOV / 2f)
        {
            return false;
        }

        // Raycast to check for obstacles
        if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer, distanceToPlayer, obstacleLayer))
        {
            return false;
        }

        return true;
    }
    
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Enemy reacts to being hit - enter chase state if not already
            if (currentState != EnemyState.Chase && currentState != EnemyState.Attack)
            {
                currentState = EnemyState.Chase;
                agent.speed = chaseSpeed;
            }
        }
    }
    
    private void Die()
    {
        Debug.Log($"{gameObject.name} has been killed!");
        
        // Disable AI
        agent.enabled = false;
        
        // Optional: Add death animation, ragdoll, etc.
        
        // Destroy after delay
        Destroy(gameObject, 2f);
    }

    private void SetNewPatrolTarget()
    {
        // Generate random point within patrol radius
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += startPosition;
        randomDirection.y = startPosition.y;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            patrolTarget = hit.position;
            agent.SetDestination(patrolTarget);
        }
    }

    private void CheckForNearbyFlares()
    {
        FlareItem[] allFlares = FindObjectsByType<FlareItem>(FindObjectsSortMode.None);
        nearestFlare = null;
        float closestDistance = detectionRange * lightDetectionMultiplier;

        foreach (FlareItem flare in allFlares)
        {
            if (flare.IsActive())
            {
                float distance = Vector3.Distance(transform.position, flare.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearestFlare = flare;
                }
            }
        }
    }

    private bool CanSeeFlare(FlareItem flare)
    {
        if (flare == null || !flare.IsActive())
            return false;

        Vector3 directionToFlare = (flare.transform.position - transform.position).normalized;
        float distanceToFlare = Vector3.Distance(transform.position, flare.transform.position);

        // Use extended detection range for light sources
        float effectiveRange = detectionRange * lightDetectionMultiplier;
        float effectiveFOV = fieldOfView * lightFOVMultiplier;

        // Check if within detection range
        if (distanceToFlare > effectiveRange)
            return false;

        // Check if within field of view
        float angle = Vector3.Angle(transform.forward, directionToFlare);
        if (angle > effectiveFOV / 2f)
            return false;

        // Check line of sight
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, directionToFlare, out hit, distanceToFlare, obstacleLayer))
        {
            // Something is blocking the view
            return false;
        }

        return true;
    }

    void OnDrawGizmosSelected()
    {
        // Determine color based on player state
        Color detectionColor = Color.yellow;
        Color fovColor = new Color(1f, 1f, 0f, 0.3f);
        
        if (Application.isPlaying)
        {
            // Check if player has active light
            bool hasLight = false;
            if (player != null)
            {
                FirstPersonController playerController = player.GetComponent<FirstPersonController>();
                if (playerController != null && playerController.flashlightController != null)
                {
                    PickableItem equippedItem = playerController.flashlightController.GetEquippedItem();
                    if (equippedItem != null)
                    {
                        FlashlightItem flashlight = equippedItem.GetComponent<FlashlightItem>();
                        GlowstickItem glowstick = equippedItem.GetComponent<GlowstickItem>();
                        hasLight = (flashlight != null && flashlight.IsOn()) || (glowstick != null && glowstick.IsOn());
                    }
                }
            }
            
            // Also check for nearby flares
            bool hasNearbyFlare = nearestFlare != null && CanSeeFlare(nearestFlare);
            
            // Red = player with light or nearby flare (high detection), Green = stealthy (low detection), Yellow = normal
            if (hasLight || hasNearbyFlare)
            {
                detectionColor = Color.red;
                fovColor = new Color(1f, 0f, 0f, 0.5f);
            }
            else if (playerIsStealthy)
            {
                detectionColor = Color.green;
                fovColor = new Color(0f, 1f, 0f, 0.3f);
            }
        }
        
        // Draw detection range (adjusted for stealth/light)
        float drawDetectionRange = Application.isPlaying ? currentDetectionRange : detectionRange;
        Gizmos.color = detectionColor;
        Gizmos.DrawWireSphere(transform.position, drawDetectionRange);

        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw patrol radius
        Vector3 origin = Application.isPlaying ? startPosition : transform.position;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(origin, patrolRadius);

        // Draw field of view (adjusted for stealth/light)
        float drawFOV = Application.isPlaying ? currentFOV : fieldOfView;
        Vector3 leftBoundary = Quaternion.Euler(0, -drawFOV / 2f, 0) * transform.forward * drawDetectionRange;
        Vector3 rightBoundary = Quaternion.Euler(0, drawFOV / 2f, 0) * transform.forward * drawDetectionRange;

        Gizmos.color = fovColor;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
    }
}
