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

    [Header("Settings")]
    public bool freezeTimeWhenOpen = false;
    public int maxSlots = 12; 
    public float throwForce = 5f; 
    public Transform playerCamera; 

    [Header("Dependencies")]
    public FlashlightController flashlightController; 

    public bool isOpen = false; 
    
    private List<PickableItem> collectedItems = new List<PickableItem>();
    private List<InventorySlot> activeSlots = new List<InventorySlot>();
    private PickableItem markedItem = null; 

    void Start()
    {
        if (inventoryUI != null) inventoryUI.SetActive(false);
        if (descriptionDisplay != null) descriptionDisplay.text = "";
        if (feedbackText != null) feedbackText.text = "";
        if (discardButton != null) discardButton.SetActive(false);
        if (playerCamera == null) playerCamera = Camera.main.transform;
    }

    public void ToggleInventory()
    {
        isOpen = !isOpen;
        if (isOpen) OpenInventory(); else CloseInventory();
    }

    void OpenInventory()
    {
        inventoryUI.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (freezeTimeWhenOpen) Time.timeScale = 0f;
        
        if (descriptionDisplay != null) descriptionDisplay.text = "";
        if (discardButton != null) discardButton.SetActive(false);
        markedItem = null;

        RefreshUI();
    }

    public void CloseInventory()
    {
        inventoryUI.SetActive(false);
        isOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        markedItem = null;
        if (descriptionDisplay != null) descriptionDisplay.text = "";
        if (discardButton != null) discardButton.SetActive(false);

        if (freezeTimeWhenOpen) Time.timeScale = 1f;
    }

    public bool AddItem(PickableItem item)
    {
        if (collectedItems.Count >= maxSlots)
        {
            ShowFeedback("Inventory Full! Max items: " + maxSlots);
            return false; 
        }
        
        // If we pick up a flashlight while one is equipped, unequip the old one first.
        if (item is FlashlightItem && flashlightController != null && flashlightController.GetEquippedItem() != null)
        {
            flashlightController.Unequip();
        }

        collectedItems.Add(item);
        
        if (isOpen) RefreshUI(); 
        else ShowFeedback("Picked up " + item.itemName);

        return true; 
    }

    public void DiscardMarkedItem()
    {
        if (markedItem == null) return;

        // If the item being discarded is equipped, unequip it first
        if (markedItem is FlashlightItem && flashlightController != null && flashlightController.GetEquippedItem() == markedItem)
        {
            flashlightController.Unequip();
        }

        collectedItems.Remove(markedItem);
        
        // Spawn item back into the world
        markedItem.gameObject.SetActive(true);
        markedItem.transform.position = playerCamera.position + (playerCamera.forward * 1.5f);
        markedItem.transform.rotation = Quaternion.identity;

        Rigidbody rb = markedItem.GetComponent<Rigidbody>();
        Collider col = markedItem.GetComponent<Collider>();

        if (col != null) col.enabled = true;
        if (rb != null)
        {
            // CRITICAL: Ensure physics is re-enabled if it was kinematic (e.g., equipped flashlight)
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero; 
            rb.AddForce(playerCamera.forward * throwForce, ForceMode.VelocityChange);
        }
        
        markedItem = null;
        if (descriptionDisplay != null) descriptionDisplay.text = "";
        if (discardButton != null) discardButton.SetActive(false);
        
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
                if (btn != null) btn.onClick.AddListener(() => OnSlotClicked(item, slotScript));
            }
        }
    }

    void OnSlotClicked(PickableItem item, InventorySlot clickedSlot)
    {
        markedItem = item;
        if (descriptionDisplay != null) descriptionDisplay.text = item.itemDescription;
        if (discardButton != null) discardButton.SetActive(true);

        foreach (InventorySlot slot in activeSlots)
        {
            slot.SetSelected(slot == clickedSlot);
        }
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
        yield return new WaitForSeconds(delay);
        if (feedbackText != null) feedbackText.text = "";
    }
    
    // --- Flashlight Finder ---
    public FlashlightItem FindFlashlight()
    {
        // Try to find an unequipped flashlight in the inventory list
        foreach (var item in collectedItems)
        {
            if (item is FlashlightItem flashlight)
            {
                // Ensure it's not the one currently equipped by the controller (if controller exists)
                if (flashlightController == null || flashlightController.GetEquippedItem() != flashlight)
                {
                    return flashlight;
                }
            }
        }
        return null;
    }

    // --- SAVE / LOAD HELPERS ---

    public List<string> GetItemNames()
    {
        List<string> names = new List<string>();
        foreach (var item in collectedItems)
        {
            names.Add(item.itemName);
        }
        return names;
    }

    public void LoadItems(List<string> itemNames)
    {
        Debug.Log($"[INVENTORY LOAD START] Attempting to load {itemNames.Count} items.");
        
        // 1. Clean up existing instantiated items 
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
        Debug.Log("[INVENTORY LOAD] Old items destroyed and list cleared.");


        // 2. Reset UI State
        markedItem = null;
        if (descriptionDisplay != null) descriptionDisplay.text = "";
        if (discardButton != null) discardButton.SetActive(false);

        // 3. Rebuild list from names by loading prefabs from Resources
        foreach (string name in itemNames)
        {
            Debug.Log($"[INVENTORY LOAD] Attempting to load item: {name}");
            string path = $"Items/{name}";
            GameObject itemPrefab = Resources.Load<GameObject>(path);
            
            if (itemPrefab != null)
            {
                Debug.Log($"[INVENTORY LOAD] Prefab FOUND for {name}. Instantiating...");
                GameObject itemObj = Instantiate(itemPrefab);
                
                itemObj.transform.SetParent(null);
                itemObj.transform.position = Vector3.zero;
                itemObj.transform.rotation = Quaternion.identity;
                itemObj.name = $"INVENTORY_ITEM_HIDDEN_{name}";
                
                itemObj.SetActive(false); 
                PickableItem pickable = itemObj.GetComponent<PickableItem>();
                
                if (pickable != null)
                {
                    if (pickable is FlashlightItem flashlight)
                    {
                        flashlight.ToggleLight(false);
                    }
                    
                    collectedItems.Add(pickable);
                    Debug.Log($"[INVENTORY LOAD] Item successfully instantiated and added to list. Total items: {collectedItems.Count}");
                }
                else
                {
                    Debug.LogError($"Inventory Load Error: Instantiated item '{name}' is missing PickableItem component! Item destroyed.");
                    Destroy(itemObj); 
                }
            }
            else
            {
                Debug.LogError($"Inventory Load Error: Could not find item prefab at path 'Resources/{path}'. Check the Resources/Items folder structure and ensure the prefab name matches the Item Name exactly.");
            }
        }

        Debug.Log($"[INVENTORY LOAD END] Final collected items count: {collectedItems.Count}");
        if (isOpen) RefreshUI();
    }
}