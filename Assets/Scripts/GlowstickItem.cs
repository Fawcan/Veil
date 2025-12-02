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
        
        // Ensure the light source is disabled at startup, but leave the mesh visible
        // so the player can see and pick up the item in the world.
        if (lightSource != null) lightSource.enabled = false;
        
        // REMOVED: if (glowstickMesh != null) glowstickMesh.SetActive(false);
    }
    
    public bool ToggleLight(bool isOn)
    {
        isLightOn = isOn;
        
        // Toggle Light
        if (lightSource != null) lightSource.enabled = isLightOn;
        
        // Toggle Mesh Visibility (This handles the 'invisible when off in hand' requirement)
        if (glowstickMesh != null) glowstickMesh.SetActive(isLightOn);
        
        return isLightOn;
    }

    public bool IsOn()
    {
        return isLightOn;
    }
}