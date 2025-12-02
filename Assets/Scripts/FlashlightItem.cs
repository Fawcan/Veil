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

        // Ensure the light source is disabled at startup, but leave the mesh visible
        // so the player can see and pick up the item in the world.
        if (lightSource != null) lightSource.enabled = false;
        
        // REMOVED: if (flashlightMesh != null) flashlightMesh.SetActive(false);
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
                // Optional: Play flicker sound here
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
        
        // Toggle the Light Component
        if (lightSource != null) lightSource.enabled = isLightOn;
        
        // Toggle the 3D Model Visibility (This handles the 'invisible when off in hand' requirement)
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