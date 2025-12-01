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
    
    // --- Double-Click State ---
    private float lastClickTime = 0f;
    // --------------------------
    
    [HideInInspector] public PickableItem itemToUse = null; 

    void Start()
    {
        if (inventoryUI != null) inventoryUI.SetActive(false);
        if (descriptionDisplay != null) descriptionDisplay.text = "";
        if (feedbackText != null) feedbackText.text = "";
        
        // Ensure buttons are hidden on Start
        if (discardButton != null) discardButton.SetActive(false);
        if (useButton != null) useButton.SetActive(false); 

        // Add listener for the Use button
        if (useButton != null)
        {
            Button useBtn = useButton.GetComponent<Button>();
            if (useBtn != null) useBtn.onClick.AddListener(OnUseClicked);
        }
        
        if (playerCamera == null) playerCamera = Camera.main.transform;
    }

    public void ToggleInventory()
    {
        if (isOpen) CloseInventory(); else OpenInventory();
    }

    void OpenInventory()
    {
        isOpen = true;
        inventoryUI.SetActive(true);
        Cursor.lockState = CursorLockMode.None; // Unlock cursor
        // Cursor.visible is now managed by FirstPersonController.OnGUI
        if (freezeTimeWhenOpen) Time.timeScale = 0f;
        
        if (descriptionDisplay != null) descriptionDisplay.text = "";
        if (discardButton != null) discardButton.SetActive(false);
        if (useButton != null) useButton.SetActive(false);
        markedItem = null;
        itemToUse = null; 

        RefreshUI();
    }

    public void CloseInventory()
    {
        inventoryUI.SetActive(false);
        isOpen = false;
        Cursor.lockState = CursorLockMode.Locked; // Lock cursor
        // Cursor.visible is now managed by FirstPersonController.OnGUI
        
        markedItem = null;
        if (descriptionDisplay != null) descriptionDisplay.text = "";
        if (discardButton != null) discardButton.SetActive(false);
        if (useButton != null) useButton.SetActive(false);

        if (freezeTimeWhenOpen) Time.timeScale = 1f;
    }

    public bool AddItem(PickableItem item)
    {
        if (collectedItems.Count >= maxSlots)
        {
            ShowFeedback("Inventory Full! Max items: " + maxSlots);
            return false; 
        }
        
        // Note: The flashlight logic here handles pickup when the flashlight is already equipped.
        if (item is FlashlightItem && flashlightController != null && flashlightController.GetEquippedItem() != null)
        {
            flashlightController.Unequip();
        }

        collectedItems.Add(item);
        
        // Ensure the physical object is hidden after pickup
        item.gameObject.SetActive(false);

        if (isOpen) RefreshUI(); 
        else ShowFeedback("Picked up " + item.itemName);

        return true; 
    }

    public void DiscardMarkedItem()
    {
        if (markedItem == null) return;

        // Safeguard Logic
        if (!markedItem.canBeDiscarded)
        {
            ShowFeedback($"Cannot discard {markedItem.itemName}. It is a key item.");
            return; 
        }

        if (markedItem is FlashlightItem && flashlightController != null && flashlightController.GetEquippedItem() == markedItem)
        {
            flashlightController.Unequip();
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
                    // Pass the item reference to the click listener
                    btn.onClick.AddListener(() => OnSlotClicked(item, slotScript));
                }
            }
        }
    }

    void OnSlotClicked(PickableItem item, InventorySlot clickedSlot)
    {
        // --- DOUBLE-CLICK DETECTION LOGIC ---
        if (markedItem == item && Time.unscaledTime - lastClickTime < doubleClickTime)
        {
            // Double-click detected. Reset timer immediately.
            lastClickTime = 0f; 

            if (item.canBeUsedFromInventory)
            {
                 PerformItemAction(item); 
                 // If action is performed (e.g., key used, item consumed), the inventory closes,
                 // so we stop execution here.
                 return;
            }

            // If the item is NOT usable from inventory (like the flashlight), we don't return.
            // We fall through to the single-click logic below, which keeps it selected/highlighted.
        }
        // ------------------------------------

        // --- Single click logic (or non-usable double-click fall-through) ---

        // 1. Select the item and update description/buttons
        markedItem = item;
        if (descriptionDisplay != null) descriptionDisplay.text = item.itemDescription;
        
        // Button Visibility Logic
        if (discardButton != null)
        {
            discardButton.SetActive(item.canBeDiscarded);
        }
        
        if (useButton != null)
        {
            useButton.SetActive(item.canBeUsedFromInventory); 
        }

        // 2. Update all slot highlights
        foreach (InventorySlot slot in activeSlots)
        {
            slot.SetSelected(slot == clickedSlot);
        }

        // 3. Set the time for the next potential double-click check (Only if it was NOT a double-click just now)
        if (Time.unscaledTime - lastClickTime >= doubleClickTime)
        {
            lastClickTime = Time.unscaledTime;
        }
    }
    
    public void OnUseClicked()
    {
        if (markedItem != null && markedItem.canBeUsedFromInventory)
        {
            PerformItemAction(markedItem); 
        }
    }

    /// <summary>
    /// Central logic for using an item, handling equip/consume/defer actions.
    /// This is called by the 'Use' button and the double-click event.
    /// </summary>
    public void PerformItemAction(PickableItem item)
    {
        
        if (item is FlashlightItem flashlight && flashlightController != null)
        {
            // Toggle equip state
            if (flashlightController.GetEquippedItem() == flashlight)
            {
                flashlightController.Unequip();
                ShowFeedback($"{item.itemName} put away.");
            }
            else
            {
                // Unequip current item before equipping the new one
                if (flashlightController.GetEquippedItem() != null)
                {
                    flashlightController.Unequip();
                }
                flashlightController.Equip(flashlight);
                ShowFeedback($"{item.itemName} equipped. Press F to toggle light.");
            }
            CloseInventory(); 
            return;
        }

        // 2. Handle Immediate Use/Consumption (Uses the virtual OnUse method from PickableItem)
        if (item.OnUse(this)) 
        {
            // Item consumed itself
            CloseInventory();
            return;
        }

        // 3. Fallback to deferred interaction (key/object that needs raycast)
        itemToUse = item;
        CloseInventory();
        ShowFeedback($"Ready to use: {itemToUse.itemName}. Now look at an object and press [Interact]!");
    }
    
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

    IEnumerator HideFeedbackAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay); 
        if (feedbackText != null) feedbackText.text = "";
    }
    
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

    // --- SAVE/LOAD METHODS ---

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

    public void LoadItems(List<SaveData.SavedInventoryItem> savedItemData)
    {
        Debug.Log($"[INVENTORY LOAD START] Attempting to load {savedItemData.Count} items.");
        
        if (flashlightController != null)
        {
            flashlightController.Unequip();
        }

        foreach (var item in collectedItems)
        {
            if (item != null && item.gameObject != null) 
            {
                Destroy(item.gameObject);
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
                    // Apply saved properties (ID and discardability)
                    pickable.itemId = savedItem.itemId;
                    pickable.canBeDiscarded = savedItem.canBeDiscarded;

                    if (pickable is FlashlightItem flashlight)
                    {
                        flashlight.ToggleLight(false);
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