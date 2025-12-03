using UnityEngine;

// This should inherit from PickableItem so it can be picked up and stored in InventorySystem
public class BatteryItem : PickableItem
{
    [Header("Battery Properties")]
    [Tooltip("The amount of battery life (in seconds) this item restores. Use 0 for full recharge.")]
    public float rechargeAmount = 300f; // Default: 5 minutes of life

    // When the player interacts with this item, we ensure it's storable
    protected new void Start()
    {
        base.Start();
        // Batteries should always be storable so they can be carried and used later
        isStorable = true; 
        itemName = "Battery"; // Default name for inventory display
    }

    // You can add an Interact() or custom logic here if needed, 
    // but the actual recharging happens in FirstPersonController.OnInteract() 
    // when the player selects the BatteryItem in inventory and clicks on the flashlight.

    public float GetRechargeAmount()
    {
        return rechargeAmount;
    }
}