using UnityEngine;
using UnityEngine.UI;

// NOTE: This component should be attached to the Inventory Slot UI Prefab.
public class InventorySlot : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public Image selectionHighlight;
    public Button slotButton;

    private PickableItem currentItem;

    // --- NEW: Ensure the highlight is off on Start ---
    void Start()
    {
        if (selectionHighlight != null)
        {
            selectionHighlight.enabled = false;
        }
    }
    // --- END NEW ---

    public void SetItem(PickableItem item)
    {
        currentItem = item;

        if (iconImage != null && item != null && item.icon != null)
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = true;
        }
        else
        {
            iconImage.enabled = false;
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (selectionHighlight != null)
        {
            // This line controls whether the highlight image is visible.
            selectionHighlight.enabled = isSelected;
        }
    }
}