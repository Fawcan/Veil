using UnityEngine;

/// <summary>
/// FlareItem - A throwable light source that activates when used and emits light for a duration.
/// </summary>
public class FlareItem : PickableItem
{
    [Header("Flare Settings")]
    [Tooltip("The light component that will be activated.")]
    public Light flareLight;
    
    [Tooltip("Duration the flare stays lit (in seconds). 0 = infinite.")]
    public float burnDuration = 60f;
    
    [Tooltip("Intensity of the light when active.")]
    public float lightIntensity = 1f;
    
    [Tooltip("Color of the flare light.")]
    public Color flareColor = Color.red;
    
    [Tooltip("Particle system for flare effects (optional).")]
    public ParticleSystem flareParticles;
    
    private bool isActive = false;
    private float burnTimer = 0f;

    protected override void Start()
    {
        base.Start();
        
        // Make flares throwable by default
        isThrowable = true;
        canBeUsedFromInventory = true;
        
        // Auto-find light if not assigned
        if (flareLight == null)
        {
            flareLight = GetComponentInChildren<Light>();
        }
        
        // Initialize light as off
        if (flareLight != null)
        {
            flareLight.enabled = false;
            flareLight.intensity = lightIntensity;
            flareLight.color = flareColor;
        }
        
        // Initialize particles as off
        if (flareParticles != null)
        {
            flareParticles.Stop();
        }
    }

    void Update()
    {
        if (isActive && burnDuration > 0)
        {
            burnTimer += Time.deltaTime;
            
            if (burnTimer >= burnDuration)
            {
                Extinguish();
            }
            else if (burnDuration > 10f && burnTimer >= burnDuration - 5f)
            {
                // Flicker in last 5 seconds
                float flickerAmount = Mathf.PerlinNoise(Time.time * 10f, 0f);
                if (flareLight != null)
                {
                    flareLight.intensity = lightIntensity * Mathf.Lerp(0.3f, 1f, flickerAmount);
                }
            }
        }
    }

    /// <summary>
    /// Activates the flare light.
    /// </summary>
    public void Ignite()
    {
        if (isActive) return;
        
        isActive = true;
        burnTimer = 0f;
        
        // Once ignited, disable pickup by making it non-storable
        isStorable = false;
        
        if (flareLight != null)
        {
            flareLight.enabled = true;
            flareLight.intensity = lightIntensity;
        }
        
        if (flareParticles != null)
        {
            flareParticles.Play();
        }
    }

    /// <summary>
    /// Deactivates the flare light.
    /// </summary>
    public void Extinguish()
    {
        isActive = false;
        
        if (flareLight != null)
        {
            flareLight.enabled = false;
        }
        
        if (flareParticles != null)
        {
            flareParticles.Stop();
        }
    }

    /// <summary>
    /// Check if flare is currently lit.
    /// </summary>
    public bool IsActive()
    {
        return isActive;
    }

    /// <summary>
    /// Override OnUse to ignite the flare when used from inventory.
    /// </summary>
    public override bool OnUse(InventorySystem inventory)
    {
        if (!isActive)
        {
            Ignite();
            
            // Remove from inventory and place in world
            if (inventory != null)
            {
                inventory.ConsumeItem(this);
                
                // Place flare in front of player
                FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
                if (player != null && player.cameraTransform != null)
                {
                    gameObject.SetActive(true);
                    transform.position = player.cameraTransform.position + player.cameraTransform.forward * 1.5f;
                    transform.rotation = Quaternion.identity;
                    
                    Rigidbody rb = GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                        rb.useGravity = true;
                    }
                }
                
                inventory.ShowFeedback($"{itemName} ignited!");
            }
            
            return true;
        }
        
        return false;
    }

    void OnDrawGizmosSelected()
    {
        // Draw light range indicator
        if (flareLight != null)
        {
            Gizmos.color = isActive ? flareColor : new Color(flareColor.r, flareColor.g, flareColor.b, 0.3f);
            Gizmos.DrawWireSphere(transform.position, flareLight.range);
        }
    }
}
