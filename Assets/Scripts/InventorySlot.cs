using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public Image selectionHighlight;
    public Button slotButton;

    private PickableItem currentItem;

    void Start()
    {
        if (selectionHighlight != null)
        {
            selectionHighlight.enabled = false;
        }
    }

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
            selectionHighlight.enabled = isSelected;
        }
    }
}