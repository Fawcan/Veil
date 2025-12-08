using UnityEngine;

public class FlashlightItem : PickableItem
{
    [Header("Flashlight Components")]
    [Tooltip("The actual light component (Spotlight)")]
    public Light lightSource;
    [Tooltip("The mesh of the flashlight model (Must be a child object).")]
    public GameObject flashlightMesh;

    [Header("Battery Management")]
    public float maxBatteryLife = 300f;
    public float drainRate = 1f;
    public float currentBatteryLife;

    private bool isLightOn = false;

    protected override void Start()
    {
        base.Start(); 
        
        if (currentBatteryLife <= 0) currentBatteryLife = maxBatteryLife;

        if (lightSource != null) lightSource.enabled = false;
        
    }
    
    void Update()
    {
        if (isLightOn)
        {
            currentBatteryLife -= drainRate * Time.deltaTime;
            currentBatteryLife = Mathf.Max(0f, currentBatteryLife);

            if (currentBatteryLife <= 0f)
            {
                ToggleLight(false);
            }
        }
    }

    public bool ToggleLight(bool isOn)
    {
        if (isOn && currentBatteryLife <= 0f)
        {
            isLightOn = false;
        }
        else
        {
            isLightOn = isOn;
        }
        
        if (lightSource != null) lightSource.enabled = isLightOn;
        
        if (flashlightMesh != null) flashlightMesh.SetActive(isLightOn);
        
        return isLightOn;
    }

    public bool IsOn()
    {
        return isLightOn;
    }
    
    public float GetBatteryPercentage()
    {
        return currentBatteryLife / maxBatteryLife;
    }
    
    public void Recharge(float amount)
    {
        currentBatteryLife = Mathf.Min(currentBatteryLife + amount, maxBatteryLife);
    }
}