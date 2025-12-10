using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections; 
using System.Collections.Generic;

public class InventorySystem : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryUI; 
    public Transform itemsContainer; 
    public GameObject itemSlotPrefab; 
    public TextMeshProUGUI descriptionDisplay; 
    public TextMeshProUGUI feedbackText; 
    public GameObject discardButton; 
    public GameObject useButton; 

    [Header("Settings")]
    public bool freezeTimeWhenOpen = false;
    public int maxSlots = 12; 
    public float throwForce = 5f; 
    public Transform playerCamera; 
    [Tooltip("Time window (in unscaled seconds) to detect a double-click.")]
    public float doubleClickTime = 0.3f; 

    [Header("Dependencies")]
    public FlashlightController flashlightController; 

    public bool isOpen = false; 
    
    private List<PickableItem> collectedItems = new List<PickableItem>();
    private List<InventorySlot> activeSlots = new List<InventorySlot>();
    private PickableItem markedItem = null; 
    
    private float lastClickTime = 0f;
    
    [HideInInspector] public PickableItem itemToUse = null; 

    // Initialize the inventory UI on game start
    void Start()
    {
        if (inventoryUI != null) inventoryUI.SetActive(false);
        if (descriptionDisplay != null) descriptionDisplay.text = "";
        if (feedbackText != null) feedbackText.text = "";
        
        if (discardButton != null) discardButton.SetActive(false);
        if (useButton != null) useButton.SetActive(false); 

        if (useButton != null)
        {
            Button useBtn = useButton.GetComponent<Button>();
            if (useBtn != null) useBtn.onClick.AddListener(OnUseClicked);
        }
        
        if (playerCamera == null) playerCamera = Camera.main.transform;
    }

    // Opens or closes the inventory UI based on current state
    public void ToggleInventory()
    {
        if (isOpen) CloseInventory(); else OpenInventory();
    }

    // Opens the inventory UI and unlocks the cursor
    void OpenInventory()
    {
        isOpen = true;
        inventoryUI.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        if (freezeTimeWhenOpen) Time.timeScale = 0f;
        
        if (descriptionDisplay != null) descriptionDisplay.text = "";
        if (discardButton != null) discardButton.SetActive(false);
        if (useButton != null) useButton.SetActive(false);
        markedItem = null;
        itemToUse = null; 

        RefreshUI();
    }

    // Closes the inventory UI and locks the cursor
    public void CloseInventory()
    {
        inventoryUI.SetActive(false);
        isOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        markedItem = null;
        if (descriptionDisplay != null) descriptionDisplay.text = "";
        if (discardButton != null) discardButton.SetActive(false);
        if (useButton != null) useButton.SetActive(false);

        if (freezeTimeWhenOpen) Time.timeScale = 1f;
    }

    // Adds an item to the inventory and deactivates it in the world
    public bool AddItem(PickableItem item)
    {
        if (collectedItems.Count >= maxSlots)
        {
            ShowFeedback("Inventory Full! Max items: " + maxSlots);
            return false; 
        }
        
        if (flashlightController != null && flashlightController.GetEquippedItem() == item)
        {
            flashlightController.UnequipCurrentLight(); 
        }

        collectedItems.Add(item);
        
        item.gameObject.SetActive(false);

        if (isOpen) RefreshUI(); 
        else ShowFeedback("Picked up " + item.itemName);

        return true; 
    }

    // Throws the currently selected item out of inventory into the world
    public void DiscardMarkedItem()
    {
        if (markedItem == null) return;

        if (!markedItem.canBeDiscarded)
        {
            ShowFeedback($"Cannot discard {markedItem.itemName}. It is a key item.");
            return; 
        }

        if (flashlightController != null && flashlightController.GetEquippedItem() == markedItem)
        {
            flashlightController.UnequipCurrentLight(); 
        }

        collectedItems.Remove(markedItem);
        
        markedItem.gameObject.SetActive(true);
        markedItem.transform.position = playerCamera.position + (playerCamera.forward * 1.5f);
        markedItem.transform.rotation = Quaternion.identity;

        Rigidbody rb = markedItem.GetComponent<Rigidbody>();
        Collider col = markedItem.GetComponent<Collider>();

        if (col != null) col.enabled = true;
        if (rb != null)
        {
            rb.isKinematic = false; 
            rb.linearVelocity = Vector3.zero; 
            rb.AddForce(playerCamera.forward * throwForce, ForceMode.VelocityChange);
        }
        
        markedItem = null;
        if (descriptionDisplay != null) descriptionDisplay.text = "";
        if (discardButton != null) discardButton.SetActive(false);
        if (useButton != null) useButton.SetActive(false); 
        
        RefreshUI();
    }

    // Rebuilds all inventory slot UI elements to match current items
    void RefreshUI()
    {
        foreach (Transform child in itemsContainer) Destroy(child.gameObject);
        activeSlots.Clear();

        foreach (PickableItem item in collectedItems)
        {
            GameObject slotObj = Instantiate(itemSlotPrefab, itemsContainer);
            InventorySlot slotScript = slotObj.GetComponent<InventorySlot>();
            
            if (slotScript != null)
            {
                slotScript.SetItem(item);
                activeSlots.Add(slotScript);

                Button btn = slotScript.slotButton; 
                if (btn == null) btn = slotObj.GetComponent<Button>();
                if (btn != null) 
                {
                    btn.onClick.AddListener(() => OnSlotClicked(item, slotScript));
                }
            }
        }
    }

    // Handles clicking on an inventory slot (selection and double-click to use)
    void OnSlotClicked(PickableItem item, InventorySlot clickedSlot)
    {
        if (markedItem == item && Time.unscaledTime - lastClickTime < doubleClickTime)
        {
            lastClickTime = 0f; 

            if (item.canBeUsedFromInventory)
            {
                 PerformItemAction(item); 
                 return;
            }

        }
        markedItem = item;
        if (descriptionDisplay != null) descriptionDisplay.text = item.itemDescription;
        
        if (discardButton != null)
        {
            discardButton.SetActive(item.canBeDiscarded);
        }
        
        if (useButton != null)
        {
            useButton.SetActive(item.canBeUsedFromInventory); 
        }

        foreach (InventorySlot slot in activeSlots)
        {
            slot.SetSelected(slot == clickedSlot);
        }

        if (Time.unscaledTime - lastClickTime >= doubleClickTime)
        {
            lastClickTime = Time.unscaledTime;
        }
    }
    
    // Called when the Use button is clicked in the inventory UI
    public void OnUseClicked()
    {
        if (markedItem != null && markedItem.canBeUsedFromInventory)
        {
            PerformItemAction(markedItem); 
        }
    }

    // Executes the use action for an item (equip flashlight, use key, etc.)
    public void PerformItemAction(PickableItem item)
    {
        // Handle WeaponItem equipping
        if (item is WeaponItem weapon && flashlightController != null)
        {
            if (flashlightController.GetEquippedItem() == weapon)
            {
                flashlightController.UnequipCurrentLight();
                ShowFeedback($"{item.itemName} put away.");
            }
            else
            {
                flashlightController.UnequipCurrentLight();
                flashlightController.EquipLightItem(weapon.GetComponent<PickableItem>());
                ShowFeedback($"{item.itemName} equipped. Hold Interact and move mouse to swing!");
            }
            CloseInventory();
            return;
        }
        
        if (item is FlashlightItem flashlight && flashlightController != null)
        {
            if (flashlightController.GetEquippedItem() == flashlight)
            {
                flashlightController.UnequipCurrentLight();
                ShowFeedback($"{item.itemName} put away.");
            }
            else
            {
                flashlightController.UnequipCurrentLight(); 
                
                flashlightController.EquipLightItem(flashlight.GetComponent<PickableItem>());
                ShowFeedback($"{item.itemName} equipped. Press F to toggle light.");
            }
            CloseInventory(); 
            return;
        }

        if (item is GlowstickItem glowstick && flashlightController != null)
        {
            if (flashlightController.GetEquippedItem() == glowstick)
            {
                bool isNowOn = !glowstick.IsOn();
                
                if (isNowOn)
                {
                    FlashlightItem flItem = flashlightController.GetEquippedItem()?.GetComponent<FlashlightItem>(); 
                    if (flItem != null && flItem.IsOn())
                    {
                        flItem.ToggleLight(false);
                    }
                }
                
                glowstick.ToggleLight(isNowOn);
                ShowFeedback($"{item.itemName} {(isNowOn ? "activated" : "deactivated")}.");
            }
            else
            {
                flashlightController.UnequipCurrentLight(); 
                
                flashlightController.EquipLightItem(glowstick.GetComponent<PickableItem>());
                glowstick.ToggleLight(true); 
                ShowFeedback($"{item.itemName} equipped and activated. Press G to toggle light.");
            }
            CloseInventory(); 
            return;
        }
        
        // Handle throwable items
        if (item.isThrowable)
        {
            FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
            if (player != null && player.currentlyHeldItem == null)
            {
                // Remove from inventory and equip for throwing
                collectedItems.Remove(item);
                item.gameObject.SetActive(true);
                
                // Position item in front of camera
                if (player.cameraTransform != null)
                {
                    item.transform.position = player.cameraTransform.position + player.cameraTransform.forward * 0.5f;
                    item.transform.rotation = player.cameraTransform.rotation;
                }
                
                player.currentlyHeldItem = item;
                
                // If it's a flare, ignite it
                FlareItem flare = item.GetComponent<FlareItem>();
                if (flare != null && !flare.IsActive())
                {
                    flare.Ignite();
                }
                
                // Setup physics for holding
                Collider[] itemColliders = item.GetComponentsInChildren<Collider>();
                foreach (Collider c in itemColliders)
                {
                    Physics.IgnoreCollision(c, player.controller, true);
                }

                Rigidbody itemRb = item.GetComponent<Rigidbody>();
                if (itemRb != null)
                {
                    itemRb.isKinematic = false;
                    itemRb.useGravity = false;
                    itemRb.linearVelocity = Vector3.zero;
                    itemRb.angularVelocity = Vector3.zero;
                    itemRb.linearDamping = 20f;
                    itemRb.angularDamping = 20f;
                    itemRb.constraints = RigidbodyConstraints.FreezeRotation;
                }
                
                CloseInventory();
                RefreshUI();
                ShowFeedback($"Ready to throw {item.itemName}. Press [Throw] to launch it!");
                return;
            }
        }

        if (item.OnUse(this)) 
        {
            CloseInventory();
            return;
        }

        itemToUse = item;
        CloseInventory();
        ShowFeedback($"Ready to use: {itemToUse.itemName}. Now look at an object and press [Interact]!");
    }
    
    // Removes and destroys an item from inventory after it's been consumed/used
    public void ConsumeItem(PickableItem item)
    {
        if (collectedItems.Contains(item))
        {
            collectedItems.Remove(item);
            if (item.gameObject != null)
            {
                Destroy(item.gameObject);
            }
        }
        itemToUse = null; 
        markedItem = null;
        RefreshUI(); 
    }

    // Displays a temporary feedback message to the player
    public void ShowFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            StopAllCoroutines(); 
            StartCoroutine(HideFeedbackAfterDelay(2f));
        }
        else
        {
            Debug.Log(message); 
        }
    }

    // Coroutine that hides feedback text after a delay
    IEnumerator HideFeedbackAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay); 
        if (feedbackText != null) feedbackText = null;
    }
    
    // Searches inventory for an unequipped flashlight item
    public FlashlightItem FindFlashlight()
    {
        foreach (var item in collectedItems)
        {
            if (item is FlashlightItem flashlight)
            {
                if (flashlightController == null || flashlightController.GetEquippedItem() != flashlight)
                {
                    return flashlight;
                }
            }
        }
        return null;
    }

    // Searches inventory for an unequipped glowstick item
    public GlowstickItem FindGlowstick()
    {
        foreach (var item in collectedItems)
        {
            if (item is GlowstickItem glowstick)
            {
                if (flashlightController == null || flashlightController.GetEquippedItem() != glowstick)
                {
                    return glowstick;
                }
            }
        }
        return null;
    }

    // Returns the list of all items currently in inventory
    public List<PickableItem> GetCollectedItems()
    {
        return collectedItems;
    }

    // Converts current inventory into save data format for game saving
    public List<SaveData.SavedInventoryItem> GetSavedItemsData()
    {
        List<SaveData.SavedInventoryItem> savedItems = new List<SaveData.SavedInventoryItem>();
        foreach (var item in collectedItems)
        {
            savedItems.Add(new SaveData.SavedInventoryItem(
                item.itemName, 
                item.itemId, 
                item.canBeDiscarded
            ));
        }
        return savedItems;
    }

    // Loads inventory items from save data by instantiating prefabs from Resources
    public void LoadItems(List<SaveData.SavedInventoryItem> savedItemData)
    {
        Debug.Log($"[INVENTORY LOAD START] Attempting to load {savedItemData.Count} items.");
        
        if (flashlightController != null)
        {
            flashlightController.UnequipCurrentLight(); 
        }

        // Only destroy items that are currently in the inventory (items we're about to replace)
        // Don't destroy items that are in the world scene
        foreach (var item in collectedItems)
        {
            if (item != null && item.gameObject != null) 
            {
                // Only destroy if this item is actually in our inventory (not in world)
                // Check if item was inactive (inventory items are typically inactive)
                if (!item.gameObject.activeSelf || item.gameObject.name.Contains("INVENTORY_ITEM_HIDDEN"))
                {
                    Destroy(item.gameObject);
                }
            }
        }
        collectedItems.Clear();

        markedItem = null;
        if (descriptionDisplay != null) descriptionDisplay.text = "";
        if (discardButton != null) discardButton.SetActive(false);
        if (useButton != null) useButton.SetActive(false); 

        foreach (SaveData.SavedInventoryItem savedItem in savedItemData)
        {
            string path = $"Items/{savedItem.itemName}";
            GameObject itemPrefab = Resources.Load<GameObject>(path);
            
            if (itemPrefab != null)
            {
                GameObject itemObj = Instantiate(itemPrefab);
                
                itemObj.transform.SetParent(null);
                itemObj.transform.position = Vector3.zero;
                itemObj.transform.rotation = Quaternion.identity;
                itemObj.name = $"INVENTORY_ITEM_HIDDEN_{savedItem.itemName}";
                
                itemObj.SetActive(false); 
                PickableItem pickable = itemObj.GetComponent<PickableItem>();
                
                if (pickable != null)
                {
                    pickable.itemId = savedItem.itemId;
                    pickable.canBeDiscarded = savedItem.canBeDiscarded;

                    if (pickable is FlashlightItem flashlight)
                    {
                        flashlight.ToggleLight(false);
                    }
                    if (pickable is GlowstickItem glowstick)
                    {
                        glowstick.ToggleLight(false);
                    }
                    
                    collectedItems.Add(pickable);
                }
                else
                {
                    Debug.LogError($"Inventory Load Error: Instantiated item '{savedItem.itemName}' is missing PickableItem component! Item destroyed.");
                    Destroy(itemObj); 
                }
            }
            else
            {
                Debug.LogError($"Inventory Load Error: Could not find item prefab at path 'Resources/{path}'. Check the Resources/Items folder structure and ensure the prefab name matches the Item Name exactly.");
            }
        }

        if (isOpen) RefreshUI();
    }
}