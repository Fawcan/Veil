using UnityEngine;

public class BatteryItem : PickableItem
{
    [Header("Battery Properties")]
    [Tooltip("The amount of battery life (in seconds) this item restores. Use 0 for full recharge.")]
    public float rechargeAmount = 300f;

    protected new void Start()
    {
        base.Start();
        isStorable = true; 
        itemName = "Battery";
    }

    public float GetRechargeAmount()
    {
        return rechargeAmount;
    }
}