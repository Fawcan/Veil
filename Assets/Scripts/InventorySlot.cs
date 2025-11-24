using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Image iconImage;
    
    [Header("Selection Visuals")]
    public Button slotButton; // The Button component on this slot
    public Image backgroundToHighlight; // The image to color (usually the slot background)
    public Color defaultColor = Color.white;
    public Color selectedColor = Color.yellow; // Color when marked

    private PickableItem myItem;

    // We store the data here
    public void SetItem(PickableItem item)
    {
        myItem = item;

        if (nameText != null) nameText.text = item.itemName;
        if (descriptionText != null) descriptionText.text = item.itemDescription;

        if (iconImage != null)
        {
            if (item.icon != null)
            {
                iconImage.sprite = item.icon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
            }
        }
        
        // Reset visual state
        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        if (backgroundToHighlight != null)
        {
            backgroundToHighlight.color = isSelected ? selectedColor : defaultColor;
        }
    }
}