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
    public bool isYInverted = false; 

    [Header("Crouching")]
    public float crouchHeightMultiplier = 0.5f;
    public float crouchSpeedMultiplier = 0.5f;
    public float crouchLerpSpeed = 10f;

    [Header("Ground & Obstacle Check")]
    public LayerMask obstacleMask;
    
    [Header("Interaction")]
    public float interactionRange = 2f; 
    public float dropForce = 5f;
    [Tooltip("Multiplier for movement speed while interacting with an object.")]
    public float interactSpeedMultiplier = 0.75f; 
    [Tooltip("How far the player can move from a door before it's auto-released.")]
    public float interactionBreakDistance = 3f; 

    [Header("UI & Systems")]
    public PauseMenu pauseMenu; 
    public InventorySystem inventory;

    private float standingHeight;
    private Vector3 standingCenter;
    private float standingCameraY;
    private bool isCrouching = false;
    private bool crouchPressed = false;
    private CharacterController controller; 
    private Vector3 playerVelocity;
    private bool isGrounded;
    private float xRotation = 0f;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpPressed;
    
    private PickableItem currentlyHeldItem = null;
    private InteractableDoor heldDoor = null;

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

    void Update()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && playerVelocity.y < 0) { playerVelocity.y = -2f; }
        
        UpdateCrouchState();
        ApplyCrouch();

        float currentSpeed = isCrouching ? moveSpeed * crouchSpeedMultiplier : moveSpeed;
        if (heldDoor != null) { currentSpeed *= interactSpeedMultiplier; }
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(moveDirection * currentSpeed * Time.deltaTime);

        if (heldDoor == null) {
            if (jumpPressed && isGrounded && !isCrouching) {
                playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpPressed = false; 
            }
        }

        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);

        if (PauseMenu.GameIsPaused) return;
        if (inventory != null && inventory.isOpen) return;

        float mouseX = lookInput.x * lookSensitivity;
        float mouseY = lookInput.y * lookSensitivity;
        if (isYInverted) mouseY = -mouseY;

        if (heldDoor != null) {
            float distance = Vector3.Distance(transform.position, heldDoor.transform.position);
            if (distance > interactionBreakDistance) { heldDoor.StopInteract(); heldDoor = null; }
            else { heldDoor.UpdateInteraction(lookInput.x); }
        }
        
        if (heldDoor == null) {
            transform.Rotate(Vector3.up * mouseX);
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }
    
    private void UpdateCrouchState() { if(crouchPressed && isGrounded){isCrouching=true;}else if(!crouchPressed && isCrouching){Vector3 rO=transform.position+controller.center+(Vector3.up*(controller.height/2)*0.9f);float rD=(standingHeight-controller.height)+0.1f;if(!Physics.Raycast(rO,Vector3.up,rD,obstacleMask)){isCrouching=false;}}}
    private void ApplyCrouch() { float tH=isCrouching?standingHeight*crouchHeightMultiplier:standingHeight;Vector3 tC=isCrouching?standingCenter*crouchHeightMultiplier:standingCenter;float tCY=isCrouching?standingCameraY*crouchHeightMultiplier:standingCameraY;controller.height=Mathf.Lerp(controller.height,tH,crouchLerpSpeed*Time.deltaTime);controller.center=Vector3.Lerp(controller.center,tC,crouchLerpSpeed*Time.deltaTime);Vector3 cP=cameraTransform.localPosition;cP.y=Mathf.Lerp(cP.y,tCY,crouchLerpSpeed*Time.deltaTime);cameraTransform.localPosition=cP;}
    private void DropItem() { if(currentlyHeldItem!=null){currentlyHeldItem.Drop(cameraTransform, dropForce);currentlyHeldItem=null;}}
    public void SetLookSensitivity(float sensitivity) { lookSensitivity = sensitivity; }
    public void SetInvertY(bool inverted) { isYInverted = inverted; }
    public float GetCameraPitch() { return xRotation; }
    public void LoadState(Vector3 position, Quaternion rotation, float pitch) { controller.enabled = false; transform.position = position; transform.rotation = rotation; xRotation = pitch; cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f); controller.enabled = true; }

    public void OnMove(InputAction.CallbackContext context) { moveInput = context.ReadValue<Vector2>(); }
    public void OnLook(InputAction.CallbackContext context) { lookInput = context.ReadValue<Vector2>(); }
    public void OnCrouch(InputAction.CallbackContext context) { crouchPressed = context.ReadValueAsButton(); }
    public void OnJump(InputAction.CallbackContext context) { if (heldDoor != null) { jumpPressed = false; } else { jumpPressed = context.ReadValueAsButton(); } }
    
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.canceled || !context.ReadValueAsButton()) { if (heldDoor != null) { heldDoor.StopInteract(); heldDoor = null; } return; }
        if (context.performed) {
            if (currentlyHeldItem != null) { DropItem(); return; }
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, interactionRange)) {
                PickableItem item = hit.collider.GetComponent<PickableItem>();
                if (item != null) { 
                    if (item.isStorable && inventory != null) { 
                        // --- MODIFIED SECTION ---
                        // Try to add item. If successful (true), hide it. 
                        // If false (full), do nothing (error shown by InventorySystem).
                        bool wasAdded = inventory.AddItem(item);
                        if (wasAdded)
                        {
                            item.gameObject.SetActive(false); 
                        }
                        // --- END MODIFIED SECTION ---
                    } else { 
                        item.PickUp(cameraTransform); 
                        currentlyHeldItem = item; 
                    } 
                    return; 
                }
                InteractableDoor door = hit.collider.GetComponentInParent<InteractableDoor>();
                if (door != null) { heldDoor = door; door.StartInteract(); return; }
                SavePoint savePoint = hit.collider.GetComponent<SavePoint>();
                if (savePoint != null) { savePoint.Interact(); return; }
            }
        }
    }

    public void OnInventory(InputAction.CallbackContext context) { if (context.performed && inventory != null) inventory.ToggleInventory(); }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (inventory != null && inventory.isOpen)
            {
                inventory.ToggleInventory();
                return;
            }
            if (pauseMenu != null)
            {
                pauseMenu.TogglePause();
            }
        }
    }
}