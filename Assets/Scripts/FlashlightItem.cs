using UnityEngine;

// Use the existing PickableItem script as the base class
public class FlashlightItem : PickableItem
{
    [Header("Flashlight Components")]
    [Tooltip("The actual light component (Spotlight)")]
    public Light lightSource;
    [Tooltip("The mesh of the flashlight model")]
    public GameObject flashlightMesh;

    private bool isLightOn = false;

    // We use 'new' because MonoBehaviour.Start is not virtual. 
    protected new void Start()
    {
        base.Start(); 

        // CRITICAL FIX: We only disable the light source on start.
        // The root PickableItem MUST stay active so the player can interact with it.
        if (lightSource != null) lightSource.enabled = false;
        
        // Ensure the mesh is initially enabled so the player can see and pick it up.
        if (flashlightMesh != null) flashlightMesh.SetActive(true);
    }

    public bool ToggleLight(bool isOn)
    {
        isLightOn = isOn;
        if (lightSource != null) lightSource.enabled = isLightOn;
        // The mesh only needs to be toggled if the flashlight is EQUIPPED
        if (flashlightMesh != null) flashlightMesh.SetActive(isLightOn);
        return isLightOn;
    }

    public bool IsOn()
    {
        return isLightOn;
    }
}