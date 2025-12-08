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
    private UnityEngine.InputSystem.PlayerInput playerInput;

    void Start()
    {
        inventorySystem = FindFirstObjectByType<InventorySystem>();
        
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

    public void StartClimbing(FirstPersonController player)
    {
        if (isPlayerOnLadder) return;

        attachedPlayer = player;
        playerController = player.GetComponent<CharacterController>();
        playerInput = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        isPlayerOnLadder = true;

        if (playerController != null) playerController.enabled = false;

        Collider ladderCollider = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
        Vector3 targetPos = player.transform.position;
        if (ladderCollider != null)
        {
            Vector3 colliderCenter = ladderCollider.bounds.center;

            Vector3 ladderForward = (forwardReference != null) ? forwardReference.forward : transform.forward;
            ladderForward.y = 0f;
            if (ladderForward.sqrMagnitude < 0.0001f) ladderForward = Vector3.forward;
            ladderForward.Normalize();

            if (flipForward) ladderForward = -ladderForward;

            Vector3 dir = -ladderForward;

            float radius = (playerController != null) ? playerController.radius : 0.5f;
            float offset = radius + snapDistanceFromCollider;

            Vector3 snapped = colliderCenter + dir * offset;
            targetPos.x = snapped.x;
            targetPos.z = snapped.z;
        }
        else
        {
            Vector3 snapPosition = transform.position;
            targetPos.x = snapPosition.x;
            targetPos.z = snapPosition.z;
        }

        if (snapCoroutine != null)
        {
            StopCoroutine(snapCoroutine);
            snapCoroutine = null;
        }

        if (smoothSnap)
        {
            snapCoroutine = StartCoroutine(SmoothSnapAndOrient(player, targetPos));
        }
        else
        {
            player.transform.position = targetPos;
            if (orientToLadder)
            {
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

            if (playerController != null) playerController.enabled = true;
        }

        if (inventorySystem != null)
        {
            inventorySystem.ShowFeedback("Climbing ladder. Press E to exit at top.");
        }

        Debug.Log("Player attached to ladder.");
    }

    void HandleClimbing()
    {
        float verticalInput = 0f;
        if (playerInput != null)
        {
            Vector2 moveInput = playerInput.actions["Move"].ReadValue<Vector2>();
            verticalInput = moveInput.y;
        }

        float newY = attachedPlayer.transform.position.y + (verticalInput * climbSpeed * Time.deltaTime);

        newY = Mathf.Clamp(newY, minClimbHeight, maxClimbHeight);

        Vector3 newPosition = attachedPlayer.transform.position;
        newPosition.y = newY;

        playerController.enabled = false;
        attachedPlayer.transform.position = newPosition;
        playerController.enabled = true;

        if (Mathf.Abs(newY - maxClimbHeight) < topExitRange)
        {
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
            Vector3 ladderForward = (forwardReference != null) ? forwardReference.forward : transform.forward;
            ladderForward.y = 0f;
            if (ladderForward.sqrMagnitude > 0.0001f)
            {
                ladderForward.Normalize();

                if (flipForward) ladderForward = -ladderForward;

                targetRot = Quaternion.LookRotation(ladderForward, Vector3.up);
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

    public void TryDismount()
    {
        if (!isPlayerOnLadder || attachedPlayer == null) return;

        float currentY = attachedPlayer.transform.position.y;
        bool isAtTop = Mathf.Abs(currentY - maxClimbHeight) < topExitRange;

        if (isAtTop && topDismountPoint != null)
        {
            DismountAtTop();
        }
        else
        {
            StopClimbing();
        }
    }

    void DismountAtTop()
    {
        if (topDismountPoint == null)
        {
            Debug.LogWarning("Top dismount point not set!");
            StopClimbing();
            return;
        }

        if (snapCoroutine != null)
        {
            StopCoroutine(snapCoroutine);
            snapCoroutine = null;
        }

        playerController.enabled = false;
        attachedPlayer.transform.position = topDismountPoint.position;
        playerController.enabled = true;

        isPlayerOnLadder = false;
        attachedPlayer = null;
        playerController = null;

        if (inventorySystem != null)
        {
            inventorySystem.ShowFeedback("Exited ladder.");
        }

        Debug.Log("Player dismounted at top.");
    }

    public void StopClimbing()
    {
        if (!isPlayerOnLadder) return;

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

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(b.center, b.size);

            Gizmos.color = Color.yellow;
            float sphereSize = Mathf.Min(Mathf.Max(b.size.x, b.size.z) * 0.03f, 0.25f);
            Gizmos.DrawSphere(b.center, sphereSize);
        }
        else
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        }

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
