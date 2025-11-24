using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections.Generic;

public class InventorySystem : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryUI; 
    public Transform itemsContainer; 
    public GameObject itemSlotPrefab; 

    [Header("Settings")]
    public bool freezeTimeWhenOpen = false;

    // --- CHANGED: Made public so Controller can access it ---
    public bool isOpen = false; 
    
    private List<PickableItem> collectedItems = new List<PickableItem>();

    void Start()
    {
        if (inventoryUI != null) inventoryUI.SetActive(false);
    }

    public void ToggleInventory()
    {
        isOpen = !isOpen;
        if (isOpen) OpenInventory(); else CloseInventory();
    }

    void OpenInventory()
    {
        inventoryUI.SetActive(true);
        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (freezeTimeWhenOpen) Time.timeScale = 0f;
        RefreshUI();
    }

    public void CloseInventory()
    {
        inventoryUI.SetActive(false);
        isOpen = false;
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (freezeTimeWhenOpen) Time.timeScale = 1f;
    }

    public void AddItem(PickableItem item)
    {
        collectedItems.Add(item);
        RefreshUI(); 
    }

    void RefreshUI()
    {
        foreach (Transform child in itemsContainer) Destroy(child.gameObject);

        foreach (PickableItem item in collectedItems)
        {
            GameObject slot = Instantiate(itemSlotPrefab, itemsContainer);
            TextMeshProUGUI textComp = slot.GetComponentInChildren<TextMeshProUGUI>();
            if (textComp != null) textComp.text = item.itemName;

            Image[] images = slot.GetComponentsInChildren<Image>();
            foreach(Image img in images)
            {
                if (img.gameObject != slot && item.icon != null)
                {
                    img.sprite = item.icon;
                    img.enabled = true;
                    break;
                }
            }
        }
    }

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
        collectedItems.Clear();

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
        }
        RefreshUI();
    }
}