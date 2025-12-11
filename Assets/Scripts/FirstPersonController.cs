using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
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
    public float sensitivityScale = 10.0f;
    public bool isYInverted = false; 

    [Header("Crouching")]
    public float crouchHeightMultiplier = 0.5f;
    public float crouchSpeedMultiplier = 0.5f;
    public float crouchLerpSpeed = 10f;
    
    [Header("Stealth")]
    [Tooltip("Time player must be crouched and still before entering stealth mode.")]
    public float stealthDelay = 1.5f;
    
    [Tooltip("FOV reduction when in stealth mode.")]
    public float stealthFOVReduction = 10f;
    
    [Tooltip("Speed of FOV transition.")]
    public float fovTransitionSpeed = 2f;

    [Header("Ground & Obstacle Check")]
    public LayerMask obstacleMask;
    
    [Header("Interaction")]
    public float interactionRange = 2f;
    public float dropForce = 5f;
    [Tooltip("Force applied to an item when explicitly thrown/smashed.")]
    public float smashForce = 20f;
    [Tooltip("Multiplier for movement speed while interacting with an object.")]
    public float interactSpeedMultiplier = 0.75f;
    [Tooltip("How far the player can move from a door before it's auto-released.")]
    public float interactionBreakDistance = 3f;

    [Header("Holding Physics")] 
    public float holdDistance = 1.5f; 
    public float holdForce = 500f; 
    public Transform heldItemHolder;

    [Header("UI & Systems")]
    public PauseMenu pauseMenu; 
    public InventorySystem inventory;
    public FlashlightController flashlightController; 
    
    [Header("Crosshair")]
    public float crosshairSize = 32f;
    public Texture2D defaultCrosshair; 
    public Texture2D hoverCrosshair;   
    public Texture2D useCrosshair; 
    [Tooltip("Crosshair to show when hovering over an interactable ladder.")]
    public Texture2D ladderHoverCrosshair;
    public Texture2D interactCrosshair; 
    public Texture2D useItemCrosshair; 
    public Texture2D inventoryCursor; 

    private float standingHeight;
    private Vector3 standingCenter;
    private float standingCameraY;
    private bool isCrouching = false;
    private bool crouchPressed = false;
    public CharacterController controller; 
    private Vector3 playerVelocity;
    private bool isGrounded;
    private Vector3 lastPosition;
    private float movementThreshold = 0.01f;
    private float xRotation = 0f;
    
    private float stealthTimer = 0f;
    private bool isInStealthMode = false;
    private Camera playerCamera;
    private float normalFOV;
    private float targetFOV;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpPressed;
    private bool isCameraLocked = false;
    
    public PickableItem currentlyHeldItem = null;
    private InteractableDoor heldDoor = null;
    private InteractableValve heldValve = null;
    private InteractableLadder currentLadder = null;
    private bool isHoldingInteractForWeapon = false;

    private enum HoverState { None, Open, Use }
    private HoverState currentHoverState = HoverState.None;
    private bool isHoveringLadder = false;
    void Start()
    {
        currentHealth = maxHealth;

        controller = GetComponent<CharacterController>(); 
        if (cameraTransform == null) cameraTransform = Camera.main.transform;
        if (heldItemHolder == null) heldItemHolder = cameraTransform;
        
        playerCamera = cameraTransform.GetComponent<Camera>();
        if (playerCamera != null)
        {
            normalFOV = playerCamera.fieldOfView;
            targetFOV = normalFOV;
        }
        
        standingHeight = controller.height;
        standingHeight = controller.height;
        standingCenter = controller.center;
        standingCameraY = cameraTransform.localPosition.y;
        
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

        bool isClimbing = currentLadder != null && currentLadder.IsPlayerClimbing();
        if (isClimbing)
        {
            jumpPressed = false;
            crouchPressed = false;

            moveInput = Vector2.zero;

            if (playerVelocity.y < 0f) playerVelocity.y = 0f;
        }

        isGrounded = controller.isGrounded;
        if (isGrounded && playerVelocity.y < 0) { playerVelocity.y = -2f; }
        
        UpdateCrouchState();
        ApplyCrouch();

        float currentSpeed = isCrouching ? moveSpeed * crouchSpeedMultiplier : moveSpeed;
        if (heldDoor != null || heldValve != null) { currentSpeed *= interactSpeedMultiplier; }
        
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(moveDirection * currentSpeed * Time.deltaTime);

        if (!isClimbing)
        {
            if (heldDoor == null && heldValve == null) {
                if (jumpPressed && isGrounded && !isCrouching) {
                    playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    jumpPressed = false; 
                }
            }

            playerVelocity.y += gravity * Time.deltaTime;
            controller.Move(playerVelocity * Time.deltaTime);
        }
        
        // Track position for stealth detection
        lastPosition = transform.position;
        
        // Update stealth state with delay
        UpdateStealthState();
        
        // Smoothly transition FOV
        if (playerCamera != null)
        {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, fovTransitionSpeed * Time.deltaTime);
        }

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
        
        // Handle weapon mouse movement when left click is held
        if (flashlightController != null)
        {
            PickableItem equippedItem = flashlightController.GetEquippedItem();
            if (equippedItem != null)
            {
                WeaponItem weapon = equippedItem.GetComponent<WeaponItem>();
                if (weapon != null)
                {
                    // Set interact button state on weapon
                    weapon.SetInteractHeld(isHoldingInteractForWeapon);
                    
                    // Pass mouse delta to weapon for state handling
                    weapon.OnMouseMove(lookInput);
                }
            }
        }

        if (heldDoor == null && heldValve == null && !isCameraLocked) {
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

    public bool IsStealthy()
    {
        return isInStealthMode;
    }
    
    public void SetCameraLocked(bool locked)
    {
        isCameraLocked = locked;
    }
    
    private void UpdateStealthState()
    {
        // Check if player meets stealth conditions (crouched and still)
        bool meetsStealthConditions = false;
        
        if (isCrouching)
        {
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);
            meetsStealthConditions = distanceMoved < movementThreshold;
        }
        
        // Update stealth timer
        if (meetsStealthConditions)
        {
            stealthTimer += Time.deltaTime;
            
            // Enter stealth mode after delay
            if (stealthTimer >= stealthDelay && !isInStealthMode)
            {
                isInStealthMode = true;
                targetFOV = normalFOV - stealthFOVReduction;
            }
        }
        else
        {
            // Reset timer and exit stealth mode immediately
            stealthTimer = 0f;
            if (isInStealthMode)
            {
                isInStealthMode = false;
                targetFOV = normalFOV;
            }
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

        Vector3 targetPosition = cameraTransform.position + cameraTransform.forward * 0.8f;
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
        isHoveringLadder = false;
        
        if (currentLadder != null && currentLadder.IsPlayerClimbing())
        {
            currentHoverState = HoverState.Use;
            return;
        }

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

            // Check for cable slot when holding a cable
            if (currentlyHeldItem != null)
            {
                InteractableCable heldCable = currentlyHeldItem.GetComponent<InteractableCable>();
                if (heldCable != null)
                {
                    CableSlot slot = hit.collider.GetComponentInParent<CableSlot>();
                    if (slot != null && !slot.isOccupied)
                    {
                        currentHoverState = HoverState.Use;
                        return;
                    }
                }
            }

            InteractableLadder ladder = hit.collider.GetComponentInParent<InteractableLadder>();
            if (ladder != null)
            {
                currentHoverState = HoverState.Open;
                isHoveringLadder = true;
                return;
            }

            // Check for cable hover
            InteractableCable cable = hit.collider.GetComponentInParent<InteractableCable>();
            if (cable != null)
            {
                currentHoverState = HoverState.Open;
                return;
            }

            PickableItem pickableItem = hit.collider.GetComponent<PickableItem>();
            if (pickableItem != null)
            {
                // Don't show interaction prompt if this item is already in inventory
                if (inventory != null && inventory.HasItem(pickableItem.itemId))
                {
                    return;
                }
                currentHoverState = HoverState.Open;
                return;
            }

            if (hit.collider.GetComponentInParent<InteractableDoor>() != null ||
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
        if (GameManager.Instance == null || !GameManager.Instance.isGameOver) 
        {
            GUIStyle healthStyle = new GUIStyle(GUI.skin.label);
            healthStyle.fontSize = 24;
            healthStyle.normal.textColor = currentHealth > 20 ? Color.white : Color.red;
            GUI.Label(new Rect(20, Screen.height - 50, 200, 40), $"Health: {Mathf.RoundToInt(currentHealth)}", healthStyle);

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
        
        if (PauseMenu.GameIsPaused) return; 
        if (inventory != null && inventory.isOpen) return; 

        Cursor.visible = false; 

        Texture2D textureToDraw = defaultCrosshair;
        
        bool isInItemUseMode = inventory != null && inventory.itemToUse != null;

        if (isInItemUseMode)
        {
            if (currentHoverState == HoverState.Use) textureToDraw = useItemCrosshair; 
        }
        else if (currentlyHeldItem != null)
        {
            InteractableCable heldCable = currentlyHeldItem.GetComponent<InteractableCable>();
            if (heldCable != null)
            {
                // Holding a cable - show interact crosshair, or use crosshair when hovering slot
                if (currentHoverState == HoverState.Use)
                {
                    textureToDraw = useCrosshair;
                }
                else
                {
                    textureToDraw = interactCrosshair;
                }
            }
            else
            {
                textureToDraw = interactCrosshair;
            }
        }
        else if (heldDoor != null || heldValve != null)
        {
            textureToDraw = interactCrosshair;
        }
        else if (currentHoverState == HoverState.Open)
        {
            if (isHoveringLadder && ladderHoverCrosshair != null)
            {
                textureToDraw = ladderHoverCrosshair;
            }
            else
            {
                textureToDraw = hoverCrosshair; 
            }
        }
        else if (currentHoverState == HoverState.Use)
        {
            bool isClimbing = currentLadder != null && currentLadder.IsPlayerClimbing();
            if (!isClimbing)
            {
                textureToDraw = useCrosshair;
            }
        }

        if (textureToDraw != null)
        {
            float xMin = (Screen.width / 2) - (crosshairSize / 2);
            float yMin = (Screen.height / 2) - (crosshairSize / 2);
            GUI.DrawTexture(new Rect(xMin, yMin, crosshairSize, crosshairSize), textureToDraw);
        }
    }
    
    public void LoadState(Vector3 position, Quaternion rotation) 
    { 
        controller.enabled = false; 
        transform.position = position; 
        transform.rotation = rotation; 
        
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
                    // Use item's throwForce if it's throwable, otherwise use smashForce
                    float force = currentlyHeldItem.isThrowable ? currentlyHeldItem.throwForce : smashForce;
                    itemRb.AddForce(cameraTransform.forward * force, ForceMode.Impulse);
                    
                    if (currentlyHeldItem.isThrowable)
                    {
                        if(inventory != null) inventory.ShowFeedback($"Threw {currentlyHeldItem.itemName}!");
                    }
                    else
                    {
                        if(inventory != null) inventory.ShowFeedback($"SMASH!");
                    }
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

    public void OnMove(InputAction.CallbackContext context) { moveInput = context.ReadValue<Vector2>(); }
    public void OnLook(InputAction.CallbackContext context) { lookInput = context.ReadValue<Vector2>(); }
    public void OnCrouch(InputAction.CallbackContext context) { crouchPressed = context.ReadValueAsButton(); }
    public void OnJump(InputAction.CallbackContext context) { if (heldDoor != null || heldValve != null) { jumpPressed = false; } else { jumpPressed = context.ReadValueAsButton(); } }
    public void OnThrow(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (currentlyHeldItem != null) 
            { 
                DropItem(true); 
            }
        }
    }
    
    public void OnFire(InputAction.CallbackContext context)
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;
        if (PauseMenu.GameIsPaused) return;
        if (inventory != null && inventory.isOpen) return;
        
        // Check if holding a weapon
        if (flashlightController != null)
        {
            PickableItem equippedItem = flashlightController.GetEquippedItem();
            if (equippedItem != null)
            {
                WeaponItem weapon = equippedItem.GetComponent<WeaponItem>();
                if (weapon != null)
                {
                    if (context.canceled || !context.ReadValueAsButton())
                    {
                        // Mouse released
                        weapon.OnMouseRelease();
                    }
                    // Mouse movement is handled in Update
                }
            }
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
            
            // Stop weapon swinging when interact is released
            isHoldingInteractForWeapon = false;
            
            // Unlock camera when releasing interact if weapon is equipped
            if (flashlightController != null)
            {
                PickableItem equippedItem = flashlightController.GetEquippedItem();
                if (equippedItem != null)
                {
                    WeaponItem weapon = equippedItem.GetComponent<WeaponItem>();
                    if (weapon != null)
                    {
                        SetCameraLocked(false);
                    }
                }
            }
            
            return; 
        }
        
        if (context.performed) 
        {
            if (PauseMenu.GameIsPaused) return;
            if (inventory != null && inventory.isOpen) return;
            
            // Check if holding a weapon through equipment system
            if (flashlightController != null)
            {
                PickableItem equippedItem = flashlightController.GetEquippedItem();
                if (equippedItem != null)
                {
                    WeaponItem weapon = equippedItem.GetComponent<WeaponItem>();
                    if (weapon != null)
                    {
                        isHoldingInteractForWeapon = true;
                        SetCameraLocked(true); // Lock camera when interact is pressed with weapon
                        return; // Don't process other interactions while holding weapon
                    }
                }
            }

            if (currentLadder != null && currentLadder.IsPlayerClimbing())
            {
                currentLadder.TryDismount();
                currentLadder = null;
                return;
            }

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

                // Check for cable slot interaction
                CableSlot cableSlot = hit.collider.GetComponentInParent<CableSlot>();
                if (cableSlot != null)
                {
                    cableSlot.Interact(this);
                    return;
                }

                InteractableLadder ladder = hit.collider.GetComponentInParent<InteractableLadder>();
                if (ladder != null) 
                { 
                    currentLadder = ladder;
                    ladder.StartClimbing(this); 
                    return; 
                }
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