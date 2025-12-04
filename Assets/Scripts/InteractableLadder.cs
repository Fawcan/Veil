using System.Collections;
using UnityEngine;

public class InteractableLadder : MonoBehaviour
{
    [Header("Ladder Settings")]
    [Tooltip("The climb speed in units per second.")]
    public float climbSpeed = 1f;
    
    [Tooltip("The position where the player dismounts at the top (should be above the ladder).")]
    public Transform topDismountPoint;
    
    [Tooltip("The position where the player dismounts at the bottom (should be below the ladder).")]
    public Transform bottomDismountPoint;
    
    [Header("Climb Boundaries")]
    [Tooltip("The lowest Y position the player can climb to.")]
    public float minClimbHeight = 0f;
    
    [Tooltip("The highest Y position the player can climb to.")]
    public float maxClimbHeight = 5f;
    
    [Tooltip("Extra distance to keep the player from penetrating the ladder collider when snapping onto it.")]
    public float snapDistanceFromCollider = 0.1f;
    
    [Header("Optional: Visual Feedback")]
    [Tooltip("Distance from top to show 'Press E to Exit' message.")]
    public float topExitRange = 0.5f;

    [Header("Snap / Orientation")]
    [Tooltip("If true, smoothly move the player to the snap position instead of teleporting instantly.")]
    public bool smoothSnap = true;

    [Tooltip("Duration (seconds) of the snap smoothing.")]
    public float snapDuration = 0.15f;

    [Tooltip("If true, rotate the player to face the ladder during snap.")]
    public bool orientToLadder = true;

    [Tooltip("Controls how quickly the player orients to face the ladder (0-1 used as Lerp factor).")]
    public float orientationSpeed = 1f;

    [Tooltip("Yaw offset (degrees) applied after facing the ladder so the player can be angled slightly.")]
    public float orientationOffset = 0f;

    [Header("Forward Reference")]
    [Tooltip("Optional transform that defines the ladder's forward face. If null, the ladder's own transform.forward is used.")]
    public Transform forwardReference;
    
    [Tooltip("If true, flip which side is considered the front (use -forward).")]
    public bool flipForward = false;

    private FirstPersonController attachedPlayer;
    private CharacterController playerController;
    private Coroutine snapCoroutine;
    private bool isPlayerOnLadder = false;
    private InventorySystem inventorySystem;
    private UnityEngine.InputSystem.PlayerInput playerInput; // Add reference to PlayerInput

    void Start()
    {
        inventorySystem = FindFirstObjectByType<InventorySystem>();
        
        // Auto-calculate boundaries if dismount points are set
        if (topDismountPoint != null)
        {
            maxClimbHeight = topDismountPoint.position.y;
        }
        
        if (bottomDismountPoint != null)
        {
            minClimbHeight = bottomDismountPoint.position.y;
        }
    }

    void Update()
    {
        if (isPlayerOnLadder && attachedPlayer != null)
        {
            HandleClimbing();
        }
    }

    /// <summary>
    /// Called by FirstPersonController when player interacts with the ladder.
    public void StartClimbing(FirstPersonController player)
    {
        if (isPlayerOnLadder) return;

        attachedPlayer = player;
        playerController = player.GetComponent<CharacterController>();
        playerInput = player.GetComponent<UnityEngine.InputSystem.PlayerInput>(); // Get PlayerInput
        isPlayerOnLadder = true;

        // Disable normal player movement while repositioning
        if (playerController != null) playerController.enabled = false;

        // Snap the player to just outside the ladder's collider so they are not inside it.
        Collider ladderCollider = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
        Vector3 targetPos = player.transform.position;
        if (ladderCollider != null)
        {
            // Snap to the collider's world-space center but offset outward so the player isn't inside the mesh.
            Vector3 colliderCenter = ladderCollider.bounds.center;

            // Use the ladder's forward direction (or the provided reference) to determine the front face.
            Vector3 ladderForward = (forwardReference != null) ? forwardReference.forward : transform.forward;
            ladderForward.y = 0f;
            if (ladderForward.sqrMagnitude < 0.0001f) ladderForward = Vector3.forward;
            ladderForward.Normalize();

            // Optionally flip which side is considered the front
            if (flipForward) ladderForward = -ladderForward;

            // We want the player to be positioned in front of the ladder, so offset opposite the forward vector.
            Vector3 dir = -ladderForward;

            float radius = (playerController != null) ? playerController.radius : 0.5f;
            float offset = radius + snapDistanceFromCollider;

            Vector3 snapped = colliderCenter + dir * offset;
            targetPos.x = snapped.x;
            targetPos.z = snapped.z;
        }
        else
        {
            // Fallback: center on ladder X/Z
            Vector3 snapPosition = transform.position;
            targetPos.x = snapPosition.x;
            targetPos.z = snapPosition.z;
        }

        // Apply snap preserving player's Y
        if (snapCoroutine != null)
        {
            StopCoroutine(snapCoroutine);
            snapCoroutine = null;
        }

        if (smoothSnap)
        {
            // Start coroutine to smoothly move & optionally rotate the player, controller will be re-enabled at the end
            snapCoroutine = StartCoroutine(SmoothSnapAndOrient(player, targetPos));
        }
        else
        {
            player.transform.position = targetPos;
            if (orientToLadder)
            {
                // Orient to ladder forward (or forwardReference) so the player faces the intended front side
                Vector3 ladderForward = (forwardReference != null) ? forwardReference.forward : transform.forward;
                ladderForward.y = 0f;
                if (ladderForward.sqrMagnitude > 0.0001f)
                {
                    ladderForward.Normalize();
                    Quaternion targetRot = Quaternion.LookRotation(ladderForward, Vector3.up);
                    targetRot *= Quaternion.Euler(0f, orientationOffset, 0f);
                    player.transform.rotation = targetRot;
                }
            }

            // Re-enable controller
            if (playerController != null) playerController.enabled = true;
        }

        if (inventorySystem != null)
        {
            inventorySystem.ShowFeedback("Climbing ladder. Press E to exit at top.");
        }

        Debug.Log("Player attached to ladder.");
    }

    /// <summary>
    /// Handles the climbing movement while on the ladder.
    /// </summary>
    void HandleClimbing()
    {
        // Get vertical input from the new Input System
        float verticalInput = 0f;
        if (playerInput != null)
        {
            Vector2 moveInput = playerInput.actions["Move"].ReadValue<Vector2>();
            verticalInput = moveInput.y;
        }

        // Calculate new Y position
        float newY = attachedPlayer.transform.position.y + (verticalInput * climbSpeed * Time.deltaTime);

        // Clamp to ladder boundaries
        newY = Mathf.Clamp(newY, minClimbHeight, maxClimbHeight);

        // Apply the movement
        Vector3 newPosition = attachedPlayer.transform.position;
        newPosition.y = newY;

        playerController.enabled = false;
        attachedPlayer.transform.position = newPosition;
        playerController.enabled = true;

        // Check if player is near the top and can dismount
        if (Mathf.Abs(newY - maxClimbHeight) < topExitRange)
        {
            // Player can press E again to exit at the top
        }
    }

    IEnumerator SmoothSnapAndOrient(FirstPersonController player, Vector3 targetPos)
    {
        Transform pT = player.transform;
        Quaternion startRot = pT.rotation;
        Vector3 startPos = pT.position;

        Quaternion targetRot = startRot;
        if (orientToLadder)
        {
            // Use ladder forward (or forwardReference) to determine facing direction
            Vector3 ladderForward = (forwardReference != null) ? forwardReference.forward : transform.forward;
            ladderForward.y = 0f;
            if (ladderForward.sqrMagnitude > 0.0001f)
            {
                ladderForward.Normalize();

                if (flipForward) ladderForward = -ladderForward;

                targetRot = Quaternion.LookRotation(ladderForward, Vector3.up);
                // Apply yaw offset so the player can be angled slightly relative to the ladder
                targetRot *= Quaternion.Euler(0f, orientationOffset, 0f);
            }
        }

        float elapsed = 0f;
        while (elapsed < snapDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / snapDuration));
            pT.position = Vector3.Lerp(startPos, targetPos, t);
            pT.rotation = Quaternion.Slerp(startRot, targetRot, t * orientationSpeed);
            yield return null;
        }

        pT.position = targetPos;
        pT.rotation = targetRot;

        if (playerController != null) playerController.enabled = true;
        snapCoroutine = null;
    }

    /// <summary>
    /// Called when the player presses Interact (E) while on the ladder.
    /// If at the top, dismounts. Otherwise, detaches normally.
    /// </summary>
    public void TryDismount()
    {
        if (!isPlayerOnLadder || attachedPlayer == null) return;

        // Check if player is near the top
        float currentY = attachedPlayer.transform.position.y;
        bool isAtTop = Mathf.Abs(currentY - maxClimbHeight) < topExitRange;

        if (isAtTop && topDismountPoint != null)
        {
            // Dismount at the top position
            DismountAtTop();
        }
        else
        {
            // Regular dismount (detach from ladder)
            StopClimbing();
        }
    }

    /// <summary>
    /// Dismounts the player at the top position.
    /// </summary>
    void DismountAtTop()
    {
        if (topDismountPoint == null)
        {
            Debug.LogWarning("Top dismount point not set!");
            StopClimbing();
            return;
        }

        // Stop any snap coroutine to avoid conflicts
        if (snapCoroutine != null)
        {
            StopCoroutine(snapCoroutine);
            snapCoroutine = null;
        }

        playerController.enabled = false;
        attachedPlayer.transform.position = topDismountPoint.position;
        //attachedPlayer.transform.rotation = topDismountPoint.rotation;
        playerController.enabled = true;

        // Reset state
        isPlayerOnLadder = false;
        attachedPlayer = null;
        playerController = null;

        if (inventorySystem != null)
        {
            inventorySystem.ShowFeedback("Exited ladder.");
        }

        Debug.Log("Player dismounted at top.");
    }

    /// <summary>
    /// Detaches the player from the ladder (normal dismount).
    /// </summary>
    public void StopClimbing()
    {
        if (!isPlayerOnLadder) return;

        // Stop any snap coroutine running
        if (snapCoroutine != null)
        {
            StopCoroutine(snapCoroutine);
            snapCoroutine = null;
        }

        isPlayerOnLadder = false;
        attachedPlayer = null;
        playerController = null;

        if (inventorySystem != null)
        {
            inventorySystem.ShowFeedback("Released from ladder.");
        }

        Debug.Log("Player detached from ladder.");
    }

    /// <summary>
    /// Returns whether a player is currently on this ladder.
    /// </summary>
    public bool IsPlayerClimbing()
    {
        return isPlayerOnLadder;
    }

    void OnDrawGizmosSelected()
    {
        Collider ladderCollider = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
        Vector3 center = transform.position;

        if (ladderCollider != null)
        {
            Bounds b = ladderCollider.bounds;
            center = b.center;

            // Draw collider bounds
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(b.center, b.size);

            // Draw a small sphere at the center (snap target X/Z)
            Gizmos.color = Color.yellow;
            float sphereSize = Mathf.Min(Mathf.Max(b.size.x, b.size.z) * 0.03f, 0.25f);
            Gizmos.DrawSphere(b.center, sphereSize);
        }
        else
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        }

        // Draw forward direction from center (use forwardReference if provided)
        Vector3 ladderForward = (forwardReference != null) ? forwardReference.forward : transform.forward;
        ladderForward.y = 0f;
        if (ladderForward.sqrMagnitude > 0.0001f)
        {
            ladderForward.Normalize();
            if (flipForward) ladderForward = -ladderForward;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(center, center + ladderForward * 0.5f);
            Gizmos.DrawIcon(center + ladderForward * 0.55f, "sv_label_0", true);
        }
    }
}
