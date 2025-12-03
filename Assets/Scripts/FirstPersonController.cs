using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    // Saved User Preferences:
    // doorInteractSpeedMultiplier = 0.75f, interactionRange = 2f, and interactionBreakDistance = 3f.
    
    [Header("Health System")]
    public float maxHealth = 100f;
    public float currentHealth;
    
    [Header("Player Movement")]
    public float moveSpeed = 5.0f;
    public float jumpHeight = 1.8f;
    public float gravity = -9.81f;

    [Header("Camera Look")]
    public Transform cameraTransform;
    [Tooltip("User-facing sensitivity scale (1-20). Internally scaled down for smooth movement.")]
    public float sensitivityScale = 10.0f; // Default value for 1-20 scale
    public bool isYInverted = false; 

    [Header("Crouching")]
    public float crouchHeightMultiplier = 0.5f;
    public float crouchSpeedMultiplier = 0.5f;
    public float crouchLerpSpeed = 10f;

    [Header("Ground & Obstacle Check")]
    public LayerMask obstacleMask;
    
    [Header("Interaction")]
    public float interactionRange = 2f; // User Preferred Value: 2f
    public float dropForce = 5f;
    [Tooltip("Force applied to an item when explicitly thrown/smashed.")]
    public float smashForce = 20f; // Throwing force magnitude
    [Tooltip("Multiplier for movement speed while interacting with an object.")]
    public float interactSpeedMultiplier = 0.75f; // User Preferred Value: 0.75f
    [Tooltip("How far the player can move from a door before it's auto-released.")]
    public float interactionBreakDistance = 3f; // User Preferred Value: 3f
    
    [Header("Holding Physics")] 
    public float holdDistance = 1.5f; 
    public float holdForce = 500f; 

    [Header("UI & Systems")]
    public PauseMenu pauseMenu; 
    public InventorySystem inventory;
    public FlashlightController flashlightController; 
    
    [Header("Crosshair")]
    public float crosshairSize = 32f;
    public Texture2D defaultCrosshair; 
    public Texture2D hoverCrosshair;   
    public Texture2D useCrosshair; 
    public Texture2D interactCrosshair; 
    public Texture2D useItemCrosshair; 
    public Texture2D inventoryCursor; 

    private float standingHeight;
    private Vector3 standingCenter;
    private float standingCameraY;
    private bool isCrouching = false;
    private bool crouchPressed = false;
    private CharacterController controller; 
    private Vector3 playerVelocity;
    private bool isGrounded;
    private float xRotation = 0f; // Camera Pitch

    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpPressed;
    
    private PickableItem currentlyHeldItem = null;
    private InteractableDoor heldDoor = null;
    private InteractableValve heldValve = null;
    
    private enum HoverState { None, Open, Use }
    private HoverState currentHoverState = HoverState.None;

    void Start()
    {
        // Initialize Health
        currentHealth = maxHealth;

        controller = GetComponent<CharacterController>(); 
        if (cameraTransform == null) cameraTransform = Camera.main.transform;
        standingHeight = controller.height;
        standingCenter = controller.center;
        standingCameraY = cameraTransform.localPosition.y;
        
        // Lock the cursor on game start
        LockCursor(); 
        
        if (controller != null) controller.enabled = true;
    }
    
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false; 
    }

    void Update()
    {
        if (controller != null && !controller.enabled && !PauseMenu.GameIsPaused)
        {
            controller.enabled = true;
            Debug.LogWarning("CharacterController was found disabled outside of a load state and was forcibly re-enabled.");
        }
        
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) 
        {
            return;
        }

        isGrounded = controller.isGrounded;
        if (isGrounded && playerVelocity.y < 0) { playerVelocity.y = -2f; }
        
        UpdateCrouchState();
        ApplyCrouch();

        float currentSpeed = isCrouching ? moveSpeed * crouchSpeedMultiplier : moveSpeed;
        if (heldDoor != null || heldValve != null) { currentSpeed *= interactSpeedMultiplier; }
        
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(moveDirection * currentSpeed * Time.deltaTime);

        if (heldDoor == null && heldValve == null) {
            if (jumpPressed && isGrounded && !isCrouching) {
                playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpPressed = false; 
            }
        }

        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);

        if (PauseMenu.GameIsPaused) return;
        if (inventory != null && inventory.isOpen) return;
        

        UpdateHeldItemPhysics();

        float effectiveSensitivity = sensitivityScale / 100f; 
        float mouseX = lookInput.x * effectiveSensitivity;
        float mouseY = lookInput.y * effectiveSensitivity;
        if (isYInverted) mouseY = -mouseY;

        if (heldDoor != null) {
            float distance = Vector3.Distance(transform.position, heldDoor.transform.position);
            if (distance > interactionBreakDistance) { heldDoor.StopInteract(); heldDoor = null; }
            else { heldDoor.UpdateInteraction(lookInput.x); }
        }
        
        if (heldValve != null) {
            float distance = Vector3.Distance(transform.position, heldValve.transform.position);
            if (distance > interactionBreakDistance) 
            { 
                heldValve.StopInteract(); 
                heldValve = null; 
            }
            else 
            { 
                heldValve.UpdateInteraction(lookInput.x); 
            }
        }

        if (heldDoor == null && heldValve == null) {
            transform.Rotate(Vector3.up * mouseX);
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }

        CheckInteractable();
    }
    
    public void TakeDamage(float amount)
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

        currentHealth -= amount;
        if (inventory != null) inventory.ShowFeedback($"Health: {Mathf.RoundToInt(currentHealth)}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Player Died. Showing Menu...");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
    }

    void UpdateHeldItemPhysics()
    {
        if (currentlyHeldItem == null) return;

        Rigidbody itemRb = currentlyHeldItem.GetComponent<Rigidbody>();
        if (itemRb == null) return;

        Vector3 targetPosition = cameraTransform.position + cameraTransform.forward * holdDistance;
        Vector3 displacement = targetPosition - currentlyHeldItem.transform.position;
        
        if (displacement.magnitude > interactionBreakDistance) 
        {
            DropItem(false); 
            if(inventory != null) inventory.ShowFeedback("Held item dropped due to excessive distance.");
            return;
        }

        itemRb.AddForce(displacement * holdForce, ForceMode.Acceleration);
        
        Quaternion targetRotation = cameraTransform.rotation;
        currentlyHeldItem.transform.rotation = Quaternion.Slerp(currentlyHeldItem.transform.rotation, targetRotation, Time.deltaTime * 10f);
    }

    void CheckInteractable()
    {
        currentHoverState = HoverState.None;
        
        if (currentlyHeldItem != null || heldDoor != null || heldValve != null) return;
        
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionRange))
        {
            if (inventory != null && inventory.itemToUse != null)
            {
                if (hit.collider.GetComponentInParent<InteractableDoor>() != null || 
                    hit.collider.GetComponentInParent<FlashlightItem>() != null) 
                {
                    currentHoverState = HoverState.Use;
                    return; 
                }
                return;
            }
            
            if (hit.collider.GetComponent<PickableItem>() != null ||
                hit.collider.GetComponentInParent<InteractableDoor>() != null ||
                hit.collider.GetComponentInParent<InteractableValve>() != null)
            {
                currentHoverState = HoverState.Open;
                return;
            }

            if (hit.collider.GetComponent<SavePoint>() != null)
            {
                currentHoverState = HoverState.Use;
                return;
            }
        }
    }

    void OnGUI()
    {
        // 1. --- DRAW IN-GAME HUD (HEALTH & BATTERY) ---
        if (GameManager.Instance == null || !GameManager.Instance.isGameOver) 
        {
            // Display Health 
            GUIStyle healthStyle = new GUIStyle(GUI.skin.label);
            healthStyle.fontSize = 24;
            healthStyle.normal.textColor = currentHealth > 20 ? Color.white : Color.red;
            GUI.Label(new Rect(20, Screen.height - 50, 200, 40), $"Health: {Mathf.RoundToInt(currentHealth)}", healthStyle);

            // Battery HUD Display
            PickableItem equippedLightItem = flashlightController != null ? flashlightController.GetEquippedItem() : null;
            if (equippedLightItem != null)
            {
                FlashlightItem equippedLight = equippedLightItem.GetComponent<FlashlightItem>();
                if (equippedLight != null && equippedLight.IsOn())
                {
                    float batteryPct = equippedLight.GetBatteryPercentage();
                    string batteryText = $"Battery: {Mathf.RoundToInt(batteryPct * 100)}%";
                    
                    GUIStyle style = new GUIStyle(GUI.skin.label);
                    style.alignment = TextAnchor.UpperRight;
                    style.fontSize = 24;
                    style.normal.textColor = batteryPct > 0.15f ? Color.white : Color.red; 

                    GUI.Label(new Rect(Screen.width - 200, 10, 180, 40), batteryText, style);
                }
                else if (equippedLightItem.GetComponent<GlowstickItem>() != null)
                {
                    GlowstickItem equippedGlowstick = equippedLightItem.GetComponent<GlowstickItem>();
                    if (equippedGlowstick.IsOn())
                    {
                        GUIStyle style = new GUIStyle(GUI.skin.label);
                        style.alignment = TextAnchor.UpperRight;
                        style.fontSize = 24;
                        style.normal.textColor = Color.green; 

                        GUI.Label(new Rect(Screen.width - 200, 10, 180, 40), "Glowstick ON", style);
                    }
                }
            }
        }
        
        // 2. --- CUSTOM CURSOR DRAWING (Runs when cursor lock is NONE, for all menus) ---

        bool isCursorUnlocked = Cursor.lockState != CursorLockMode.Locked;

        if (isCursorUnlocked)
        {
            if (inventoryCursor != null)
            {
                Cursor.visible = false; 
                Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                float cursorSize = crosshairSize;
                Rect position = new Rect(mousePos.x - (cursorSize / 2), Screen.height - mousePos.y - (cursorSize / 2), cursorSize, cursorSize);
                GUI.DrawTexture(position, inventoryCursor);
            }
            else
            {
                Cursor.visible = true;
            }
            return; 
        } 
        
        // 3. --- CROSSHAIR DRAWING (Runs only if cursor is locked and game is running) ---

        if (PauseMenu.GameIsPaused) return; 
        if (inventory != null && inventory.isOpen) return; 

        Cursor.visible = false; 

        Texture2D textureToDraw = defaultCrosshair;
        
        bool isInItemUseMode = inventory != null && inventory.itemToUse != null;

        if (isInItemUseMode)
        {
            if (currentHoverState == HoverState.Use) textureToDraw = useItemCrosshair; 
        }
        else if (heldDoor != null || currentlyHeldItem != null || heldValve != null)
        {
            textureToDraw = interactCrosshair;
        }
        else if (currentHoverState == HoverState.Open)
        {
            textureToDraw = hoverCrosshair; 
        }
        else if (currentHoverState == HoverState.Use)
        {
            textureToDraw = useCrosshair; 
        }

        if (textureToDraw != null)
        {
            float xMin = (Screen.width / 2) - (crosshairSize / 2);
            float yMin = (Screen.height / 2) - (crosshairSize / 2);
            GUI.DrawTexture(new Rect(xMin, yMin, crosshairSize, crosshairSize), textureToDraw);
        }
    }
    
    // --- Camera Load State (Updated to remove pitch parameter and reset xRotation) ---
    /// <summary>
    /// Loads the player's position and rotation (yaw) and resets the camera pitch to 0.
    /// </summary>
    public void LoadState(Vector3 position, Quaternion rotation) 
    { 
        controller.enabled = false; 
        transform.position = position; 
        transform.rotation = rotation; 
        
        // Reset camera pitch to 0 degrees (straight forward)
        xRotation = 0f; 
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f); 
        
        controller.enabled = true; 
    }
    
    private void UpdateCrouchState() { if(crouchPressed && isGrounded){isCrouching=true;}else if(!crouchPressed && isCrouching){Vector3 rO=transform.position+controller.center+(Vector3.up*(controller.height/2)*0.9f);float rD=(standingHeight-controller.height)+0.1f;if(!Physics.Raycast(rO,Vector3.up,rD,obstacleMask)){isCrouching=false;}}}
    private void ApplyCrouch() { float tH=isCrouching?standingHeight*crouchHeightMultiplier:standingHeight;Vector3 tC=isCrouching?standingCenter*crouchHeightMultiplier:standingCenter;float tCY=isCrouching?standingCameraY*crouchHeightMultiplier:standingCameraY;controller.height=Mathf.Lerp(controller.height,tH,crouchLerpSpeed*Time.deltaTime);controller.center=Vector3.Lerp(controller.center,tC,crouchLerpSpeed*Time.deltaTime);Vector3 cP=cameraTransform.localPosition;cP.y=Mathf.Lerp(cP.y,tCY,crouchLerpSpeed*Time.deltaTime);cameraTransform.localPosition=cP;}
    
    private void DropItem(bool isSmash) 
    { 
        if(currentlyHeldItem!=null)
        {
            FlashlightItem flItem = currentlyHeldItem.GetComponent<FlashlightItem>();
            if (flItem != null && flItem.IsOn()) { flItem.ToggleLight(false); }

            GlowstickItem gsItem = currentlyHeldItem.GetComponent<GlowstickItem>();
            if (gsItem != null && gsItem.IsOn()) { gsItem.ToggleLight(false); }
            
            Collider[] itemColliders = currentlyHeldItem.GetComponentsInChildren<Collider>();
            foreach (Collider c in itemColliders) { Physics.IgnoreCollision(c, controller, false); }

            Rigidbody itemRb = currentlyHeldItem.GetComponent<Rigidbody>();
            if (itemRb != null)
            {
                itemRb.isKinematic = false;
                itemRb.useGravity = true;
                itemRb.linearVelocity = Vector3.zero;
                
                itemRb.linearDamping = 0f;
                itemRb.angularDamping = 0.05f; 
                itemRb.constraints = RigidbodyConstraints.None;
                
                if (isSmash)
                {
                    itemRb.AddForce(cameraTransform.forward * smashForce, ForceMode.Impulse);
                    if(inventory != null) inventory.ShowFeedback($"SMASH!");
                }
            }
            
            if (!isSmash)
            {
                currentlyHeldItem.Drop(cameraTransform, dropForce);
            }
            
            currentlyHeldItem = null;
        }
    }
    
    public void SetLookSensitivity(float sensitivity) { sensitivityScale = Mathf.Clamp(sensitivity, 1f, 20f); }
    public void SetInvertY(bool inverted) { isYInverted = inverted; }
    public float GetCameraPitch() { return xRotation; }

    // --- Input Action handlers ---
    public void OnMove(InputAction.CallbackContext context) { moveInput = context.ReadValue<Vector2>(); }
    public void OnLook(InputAction.CallbackContext context) { lookInput = context.ReadValue<Vector2>(); }
    public void OnCrouch(InputAction.CallbackContext context) { crouchPressed = context.ReadValueAsButton(); }
    public void OnJump(InputAction.CallbackContext context) { if (heldDoor != null || heldValve != null) { jumpPressed = false; } else { jumpPressed = context.ReadValueAsButton(); } }
    public void OnThrow(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (currentlyHeldItem != null) { DropItem(true); }
        }
    }
    public void OnInventory(InputAction.CallbackContext context)
    { 
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

        if (context.performed && inventory != null)
        {
            if (PauseMenu.GameIsPaused) return; 
            inventory.ToggleInventory(); 
        } 
    }
    public void OnPause(InputAction.CallbackContext context)
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

        if (context.performed)
        {
            if (inventory != null && inventory.isOpen) { inventory.ToggleInventory(); return; }
            if (pauseMenu != null) { pauseMenu.TogglePause(); }
        }
    }
    
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

        if (context.canceled || !context.ReadValueAsButton()) { 
            if (heldDoor != null) { heldDoor.StopInteract(); heldDoor = null; } 
            if (heldValve != null) { heldValve.StopInteract(); heldValve = null; }
            return; 
        }
        
        if (context.performed) 
        {
            if (PauseMenu.GameIsPaused) return;
            if (inventory != null && inventory.isOpen) return;

            PickableItem stagedItem = inventory != null ? inventory.itemToUse : null;
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            RaycastHit hit;

            if (stagedItem != null)
            {
                bool interactionSuccessful = false;
                FlashlightItem equippedLight = flashlightController?.GetEquippedItem()?.GetComponent<FlashlightItem>();

                BatteryItem battery = stagedItem.GetComponent<BatteryItem>();
                if (battery != null)
                {
                    if (equippedLight != null && equippedLight.currentBatteryLife < equippedLight.maxBatteryLife)
                    {
                        equippedLight.Recharge(battery.GetRechargeAmount());
                        interactionSuccessful = true;
                    } 
                    else if (equippedLight != null && equippedLight.currentBatteryLife >= equippedLight.maxBatteryLife)
                    {
                        if(inventory != null) inventory.ShowFeedback("Flashlight battery is already full.");
                        return;
                    }
                    else 
                    {
                        if(inventory != null) inventory.ShowFeedback("No flashlight is currently equipped.");
                        return;
                    }
                }
                
                if (!interactionSuccessful && Physics.Raycast(ray, out hit, interactionRange))
                {
                    InteractableDoor door = hit.collider.GetComponentInParent<InteractableDoor>();
                    if (door != null && door.TryUseItem(stagedItem)) 
                    {
                        if(inventory != null) inventory.ShowFeedback($"Used {stagedItem.itemName}. Door Unlocked!");
                        interactionSuccessful = true;
                    }
                }
                
                if (interactionSuccessful) 
                {
                    if (inventory != null) inventory.ConsumeItem(stagedItem);
                }
                else 
                { 
                    if (battery == null)
                    {
                        if(inventory != null) inventory.ShowFeedback($"Cannot use {stagedItem.itemName} here."); 
                        if (inventory != null) inventory.itemToUse = null; 
                    }
                }
                return;
            }

            if (currentlyHeldItem != null) { DropItem(false); return; }
            
            if (Physics.Raycast(ray, out hit, interactionRange)) {
                
                PickableItem item = hit.collider.GetComponent<PickableItem>();
                if (item != null) { 
                    if (item.isStorable && inventory != null) { 
                        bool wasAdded = inventory.AddItem(item);
                        if (wasAdded) { item.gameObject.SetActive(false); }
                    } else { 
                        currentlyHeldItem = item; 
                        Collider[] itemColliders = currentlyHeldItem.GetComponentsInChildren<Collider>();
                        foreach (Collider c in itemColliders) { Physics.IgnoreCollision(c, controller, true); }

                        Rigidbody itemRb = currentlyHeldItem.GetComponent<Rigidbody>();
                        if (itemRb != null) {
                            itemRb.isKinematic = false;
                            itemRb.useGravity = false;
                            itemRb.linearVelocity = Vector3.zero;
                            itemRb.angularVelocity = Vector3.zero;
                            itemRb.linearDamping = 20f;
                            itemRb.angularDamping = 20f;
                            itemRb.constraints = RigidbodyConstraints.FreezeRotation;
                        } else {
                            item.PickUp(cameraTransform); 
                            Debug.LogWarning($"PickableItem {item.itemName} lacks a Rigidbody.");
                        }
                        if(inventory != null) inventory.ShowFeedback($"Holding {item.itemName}.");
                    } 
                    return; 
                }
                InteractableDoor door = hit.collider.GetComponentInParent<InteractableDoor>();
                if (door != null) { heldDoor = door; door.StartInteract(); return; }
                
                InteractableValve valve = hit.collider.GetComponentInParent<InteractableValve>();
                if (valve != null) { heldValve = valve; valve.StartInteract(); return; }

                SavePoint savePoint = hit.collider.GetComponent<SavePoint>();
                if (savePoint != null) { savePoint.Interact(); return; }
            }
        }
    }
    
    public void OnFlashlight(InputAction.CallbackContext context)
    {
        if (PauseMenu.GameIsPaused || (inventory != null && inventory.isOpen))
        {
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;
        
        if (context.performed)
        {
            if (inventory == null || flashlightController == null) return;
            
            PickableItem equippedItem = flashlightController.GetEquippedItem();
            FlashlightItem equippedLight = equippedItem?.GetComponent<FlashlightItem>();
            GlowstickItem equippedGlowstick = equippedItem?.GetComponent<GlowstickItem>();
            
            if (equippedLight != null) 
            {
                bool newFlashlightState = !equippedLight.IsOn();
                
                if (newFlashlightState)
                {
                    if (equippedGlowstick != null && equippedGlowstick.IsOn())
                    {
                        equippedGlowstick.ToggleLight(false);
                    }
                    else if (equippedGlowstick != null && !equippedLight.IsOn())
                    {
                        flashlightController.UnequipCurrentLight();
                        equippedItem = null; 
                    }
                    
                    if (equippedItem?.GetComponent<GlowstickItem>() != null)
                    {
                        flashlightController.UnequipCurrentLight();
                        equippedItem = null;
                    }
                }
                
                equippedLight = flashlightController.GetEquippedItem()?.GetComponent<FlashlightItem>();
                if (equippedLight != null)
                {
                    equippedLight.ToggleLight(newFlashlightState);
                }
                else
                {
                    FlashlightItem inventoryLight = inventory.FindFlashlight();
                    if (inventoryLight != null) {
                        flashlightController.EquipLightItem(inventoryLight.GetComponent<PickableItem>());
                        inventoryLight.ToggleLight(true);
                        inventory.ShowFeedback("Flashlight ON");
                    } else { inventory.ShowFeedback("No Flashlight in Inventory"); }
                }
            } 
            else if (equippedItem != null)
            {
                flashlightController.UnequipCurrentLight();
                FlashlightItem inventoryLight = inventory.FindFlashlight();
                if (inventoryLight != null) {
                    flashlightController.EquipLightItem(inventoryLight.GetComponent<PickableItem>());
                    inventoryLight.ToggleLight(true);
                    inventory.ShowFeedback("Flashlight ON");
                } else { inventory.ShowFeedback("No Flashlight in Inventory"); }
            }
            else 
            {
                 FlashlightItem inventoryLight = inventory.FindFlashlight();
                 if (inventoryLight != null) {
                    flashlightController.EquipLightItem(inventoryLight.GetComponent<PickableItem>());
                    inventoryLight.ToggleLight(true);
                    inventory.ShowFeedback("Flashlight ON");
                 } else { inventory.ShowFeedback("No Flashlight in Inventory"); }
            }

            if (currentlyHeldItem != null && flashlightController.GetEquippedItem() == currentlyHeldItem) { currentlyHeldItem = null; }
        }
    }

    public void OnGlowstick(InputAction.CallbackContext context)
    {
        if (PauseMenu.GameIsPaused || (inventory != null && inventory.isOpen))
        {
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;
        
        if (context.performed)
        {
            if (inventory == null || flashlightController == null) return;
            
            PickableItem equippedItem = flashlightController.GetEquippedItem();
            FlashlightItem equippedLight = equippedItem?.GetComponent<FlashlightItem>();
            GlowstickItem equippedGlowstick = equippedItem?.GetComponent<GlowstickItem>();
            
            if (equippedGlowstick != null)
            {
                bool newGlowstickState = !equippedGlowstick.IsOn();
                
                if (newGlowstickState)
                {
                    if (equippedLight != null && equippedLight.IsOn())
                    {
                        equippedLight.ToggleLight(false);
                    }
                }
                
                equippedGlowstick.ToggleLight(newGlowstickState);
                inventory.ShowFeedback(newGlowstickState ? "Glowstick ON" : "Glowstick OFF");
            }
            else if (equippedItem != null)
            {
                flashlightController.UnequipCurrentLight();
                GlowstickItem inventoryGlowstick = inventory.FindGlowstick();
                if (inventoryGlowstick != null) {
                    flashlightController.EquipLightItem(inventoryGlowstick.GetComponent<PickableItem>());
                    if (equippedLight != null && equippedLight.IsOn()) equippedLight.ToggleLight(false);
                    
                    inventoryGlowstick.ToggleLight(true);
                    inventory.ShowFeedback("Glowstick ON");
                } else { inventory.ShowFeedback("No Glowstick in Inventory"); }
            }
            else 
            {
                 GlowstickItem inventoryGlowstick = inventory.FindGlowstick();
                 if (inventoryGlowstick != null) {
                    flashlightController.EquipLightItem(inventoryGlowstick.GetComponent<PickableItem>());
                    inventoryGlowstick.ToggleLight(true);
                    inventory.ShowFeedback("Glowstick ON");
                 } else { inventory.ShowFeedback("No Glowstick in Inventory"); }
            }

            if (currentlyHeldItem != null && flashlightController.GetEquippedItem() == currentlyHeldItem) { currentlyHeldItem = null; }
        }
    }
    
    public float GetMoveInputMagnitude()
    {
        return moveInput.magnitude;
    }
}