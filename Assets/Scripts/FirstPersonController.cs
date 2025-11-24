using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Player Movement")]
    public float moveSpeed = 5.0f;
    public float jumpHeight = 1.8f;
    public float gravity = -9.81f;

    [Header("Camera Look")]
    public Transform cameraTransform;
    public float lookSensitivity = 0.1f;

    [Header("Crouching")]
    public float crouchHeightMultiplier = 0.5f;
    public float crouchSpeedMultiplier = 0.5f;
    public float crouchLerpSpeed = 10f;

    [Header("Ground & Obstacle Check")]
    public LayerMask obstacleMask;
    
    [Header("Interaction")]
    public float interactionRange = 2f;
    public float dropForce = 5f;
    [Tooltip("How much to slow down movement when interacting with a door (e.g., 0.3 = 30% speed).")]
    public float doorInteractSpeedMultiplier = 0.3f; 
    [Tooltip("How far the player can move from a door before it's auto-released.")]
    public float interactionBreakDistance = 3f; 

    [Header("UI")]
    public PauseMenu pauseMenu; // Drag your Canvas here in Inspector
    
    // --- Private State Variables ---
    private float standingHeight;
    private Vector3 standingCenter;
    private float standingCameraY;
    private bool isCrouching = false;
    private bool crouchPressed = false;
    private CharacterController controller; 
    private Vector3 playerVelocity;
    private bool isGrounded;
    private float xRotation = 0f;

    // Input action variables
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpPressed;
    
    private PickableItem currentlyHeldItem = null;
    private InteractableDoor heldDoor = null;
    // private Collider currentDoorCollider = null; // Removed

    void Start()
    {
        controller = GetComponent<CharacterController>(); 
        if (cameraTransform == null) cameraTransform = Camera.main.transform;
        standingHeight = controller.height;
        standingCenter = controller.center;
        standingCameraY = cameraTransform.localPosition.y;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    public void SetLookSensitivity(float sensitivity)
    {
        // lookSensitivity is the public variable you already have in the controller
        lookSensitivity = sensitivity; 
    }

    void Update()
    {
        // --- Ground Check ---
        isGrounded = controller.isGrounded;
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }

        // --- Crouch Handling ---
        UpdateCrouchState();
        ApplyCrouch();

        // --- Movement ---
        float currentSpeed = isCrouching ? moveSpeed * crouchSpeedMultiplier : moveSpeed;
        if (heldDoor != null)
        {
            currentSpeed *= doorInteractSpeedMultiplier;
        }
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(moveDirection * currentSpeed * Time.deltaTime);


        // --- Jumping ---
        if (heldDoor == null)
        {
            if (jumpPressed && isGrounded && !isCrouching)
            {
                playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpPressed = false; 
            }
        }

        // --- Gravity ---
        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);


        // --- Camera Look (Mouse) ---
        float mouseX = lookInput.x * lookSensitivity;
        float mouseY = lookInput.y * lookSensitivity;

        // Add this check:
        if (PauseMenu.GameIsPaused) return; 

        mouseX = lookInput.x * lookSensitivity;
        
        if (heldDoor != null)
        {
            // Check for distance break
            float distance = Vector3.Distance(transform.position, heldDoor.transform.position);
            if (distance > interactionBreakDistance)
            {
                // Force release (No collision re-enable needed here)
                heldDoor.StopInteract();
                heldDoor = null;
            }
            else
            {
                // Still holding and in range, pass input to door
                heldDoor.UpdateInteraction(lookInput.x);
            }
        }
        
        if (heldDoor == null)
        {
            // Normal Look
            transform.Rotate(Vector3.up * mouseX);
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    // --- Private Helper Methods ---

    private void UpdateCrouchState()
    {
        if (crouchPressed && isGrounded)
        {
            isCrouching = true;
        }
        else if (!crouchPressed && isCrouching)
        {
            // Check if there is space to stand up
            Vector3 rayOrigin = transform.position + controller.center + (Vector3.up * (controller.height / 2) * 0.9f);
            float rayDistance = (standingHeight - controller.height) + 0.1f;

            if (!Physics.Raycast(rayOrigin, Vector3.up, rayDistance, obstacleMask))
            {
                isCrouching = false;
            }
        }
    }

    private void ApplyCrouch()
    {
        float targetHeight = isCrouching ? standingHeight * crouchHeightMultiplier : standingHeight;
        Vector3 targetCenter = isCrouching ? standingCenter * crouchHeightMultiplier : standingCenter;
        float targetCameraY = isCrouching ? standingCameraY * crouchHeightMultiplier : standingCameraY;

        controller.height = Mathf.Lerp(controller.height, targetHeight, crouchLerpSpeed * Time.deltaTime);
        
        // Fixed Vector3.Lergit typo to Vector3.Lerp
        controller.center = Vector3.Lerp(controller.center, targetCenter, crouchLerpSpeed * Time.deltaTime);
        
        Vector3 cameraPosition = cameraTransform.localPosition;
        cameraPosition.y = Mathf.Lerp(cameraPosition.y, targetCameraY, crouchLerpSpeed * Time.deltaTime);
        cameraTransform.localPosition = cameraPosition;
    }

    private void DropItem()
    {
        if (currentlyHeldItem != null)
        {
            currentlyHeldItem.Drop(cameraTransform, dropForce);
            currentlyHeldItem = null;
        }
    }

    // --- Input System Methods ---
    
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }


    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.performed) // Only trigger once on press
        {
            crouchPressed = !crouchPressed; // Toggle
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        // Disable jump input if door is held
        if (heldDoor != null)
        {
            jumpPressed = false;
        }
        else
        {
            jumpPressed = context.ReadValueAsButton();
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        // --- 1. HANDLE BUTTON RELEASE (The "Hold" Logic) ---
        // If the button was released (Canceled) or is no longer pressed
        if (context.canceled || !context.ReadValueAsButton())
        {
            // If we were holding a door, release it now.
            if (heldDoor != null)
            {
                heldDoor.StopInteract();
                heldDoor = null;
            }
            // We do NOT drop items here, allowing them to "stick" to the hand.
            return;
        }

        // --- 2. HANDLE BUTTON PRESS (The "Toggle" Logic) ---
        // We only want to run the pickup/drop logic on the initial press (Performed)
        if (context.performed)
        {
            // A. Drop currently held item (Toggle behavior)
            if (currentlyHeldItem != null)
            {
                DropItem();
                return;
            }

            // B. Raycast to find new object
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactionRange))
            {
                // Check for PickableItem (Toggle behavior)
                PickableItem item = hit.collider.GetComponent<PickableItem>();
                if (item != null)
                {
                    item.PickUp(cameraTransform);
                    currentlyHeldItem = item;
                    return;
                }

                // Check for InteractableDoor (Hold behavior)
                InteractableDoor door = hit.collider.GetComponentInParent<InteractableDoor>();
                if (door != null)
                {
                    heldDoor = door;
                    door.StartInteract();
                    // We don't need to do anything else; 
                    // The 'Canceled' check at the top will handle releasing it later.
                    return;
                }
                
                Debug.Log("Interacting with: " + hit.collider.name);
            }
        }
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        // Only trigger on the initial press
        if (context.performed) 
        {
            if (pauseMenu != null)
            {
                pauseMenu.TogglePause();
            }
            else
            {
                Debug.LogWarning("Pause Menu is not assigned in the Inspector!");
            }
        }
    }
}