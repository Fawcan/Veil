using UnityEngine;

public class GlowstickItem : PickableItem
{
    [Header("Glowstick Components")]
    [Tooltip("The actual light component (Point Light/Spotlight)")]
    public Light lightSource;
    [Tooltip("The mesh of the glowstick model (Must be a child object).")]
    public GameObject glowstickMesh;
    
    private bool isLightOn = false;

    protected override void Start()
    {
        base.Start(); 
        
        if (lightSource != null) lightSource.enabled = false;
        
    }
    
    public bool ToggleLight(bool isOn)
    {
        isLightOn = isOn;
        
        if (lightSource != null) lightSource.enabled = isLightOn;
        
        if (glowstickMesh != null) glowstickMesh.SetActive(isLightOn);
        
        return isLightOn;
    }

    public bool IsOn()
    {
        return isLightOn;
    }
}