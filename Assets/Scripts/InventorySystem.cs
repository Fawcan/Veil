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
            ShowFeedback("Inventory Full!");
            return false; 
        }

        collectedItems.Add(item);
        
        if (isOpen) RefreshUI(); 
        else ShowFeedback("Picked up " + item.itemName);

        return true; 
    }

    public void DiscardMarkedItem()
    {
        if (markedItem == null) return;

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

        collectedItems.Remove(markedItem);
        
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

    void ShowFeedback(string message)
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

    // --- SAVE / LOAD HELPERS (Fixed) ---

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
        // 1. Clean up existing instantiated items to prevent memory leaks
        foreach (var item in collectedItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        collectedItems.Clear();

        // 2. Reset UI State
        markedItem = null;
        if (descriptionDisplay != null) descriptionDisplay.text = "";
        if (discardButton != null) discardButton.SetActive(false);

        // 3. Rebuild list from names
        foreach (string name in itemNames)
        {
            GameObject itemPrefab = Resources.Load<GameObject>("Items/" + name);
            
            if (itemPrefab != null)
            {
                GameObject itemObj = Instantiate(itemPrefab);
                itemObj.SetActive(false); 
                PickableItem pickable = itemObj.GetComponent<PickableItem>();
                
                if (pickable != null)
                {
                    collectedItems.Add(pickable);
                }
            }
            else
            {
                Debug.LogWarning("Inventory Load: Could not find item in Resources/Items/" + name);
            }
        }

        // 4. Refresh UI if open
        if (isOpen) RefreshUI();
    }
}