using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Image iconImage;

    // This method is called by the InventorySystem to fill the slot with data
    public void SetItem(PickableItem item)
    {
        if (nameText != null)
        {
            nameText.text = item.itemName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = item.itemDescription;
        }

        if (iconImage != null)
        {
            if (item.icon != null)
            {
                iconImage.sprite = item.icon;
                iconImage.enabled = true;
            }
            else
            {
                // If no icon is provided, hide the image component
                iconImage.enabled = false;
            }
        }
    }
}