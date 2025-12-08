using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("Position Settings")]
    [Tooltip("The relative position offset from the camera's center.")]
    public Vector3 itemEquipOffset = new Vector3(0.4f, -0.4f, 0.5f);
    
    [Header("Movement Sway/Bob Settings")]
    [Tooltip("The speed of the bobbing motion.")]
    public float bobFrequency = 1.5f;
    [Tooltip("The maximum side-to-side (X) movement.")]
    public float bobAmplitudeX = 0.005f;
    [Tooltip("The maximum up-down (Y) movement.")]
    public float bobAmplitudeY = 0.01f;
    [Tooltip("How quickly the light returns to its base position.")]
    public float smoothing = 10f;

    [Header("Dependencies")]
    public FirstPersonController fpc; 
    
    private PickableItem currentEquippedItem; 
    private Rigidbody currentRb; 
    private bool isEquipped = false;
    
    private Vector3 originalPosition; 
    private float bobTime; 

    void Start()
    {
        originalPosition = transform.localPosition;
        
        if (fpc == null)
        {
            fpc = GetComponentInParent<FirstPersonController>(); 
            if (fpc == null) Debug.LogError("FlashlightController requires a reference to FirstPersonController and should be a child of the camera.");
        }
    }

    void Update()
    {
        if (fpc != null)
        {
            ApplyBob();
        }
    }

    void ApplyBob()
    {
        if (!isEquipped)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, Time.deltaTime * smoothing);
            bobTime = 0f;
            return;
        }

        float speed = fpc.GetMoveInputMagnitude();
        
        Vector3 targetPosition = originalPosition;
        
        if (speed > 0.01f)
        {
            bobTime += Time.deltaTime * bobFrequency * speed;
            
            float bobX = Mathf.Sin(bobTime) * bobAmplitudeX;
            
            float bobY = Mathf.Abs(Mathf.Cos(bobTime)) * bobAmplitudeY;
            
            targetPosition.x += bobX;
            targetPosition.y += bobY;
        }
        else
        {
            bobTime = Mathf.Lerp(bobTime, 0f, Time.deltaTime * smoothing);
        }
        
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * smoothing);
    }
    
    public void EquipLightItem(PickableItem item)
    {
        UnequipCurrentLight(); 

        currentEquippedItem = item;
        currentRb = item.GetComponent<Rigidbody>(); 
        
        if (currentRb != null)
        {
            currentRb.linearVelocity = Vector3.zero; 
            currentRb.isKinematic = true; 
        }
        
        currentEquippedItem.gameObject.SetActive(true);
        currentEquippedItem.transform.SetParent(transform); 
        
        currentEquippedItem.transform.localPosition = itemEquipOffset;
        currentEquippedItem.transform.localRotation = Quaternion.identity;

        isEquipped = true;
    }

    public void UnequipCurrentLight()
    {
        if (currentEquippedItem != null)
        {
            FlashlightItem flItem = currentEquippedItem.GetComponent<FlashlightItem>();
            if (flItem != null) flItem.ToggleLight(false);
            
            GlowstickItem gsItem = currentEquippedItem.GetComponent<GlowstickItem>();
            if (gsItem != null) gsItem.ToggleLight(false);
            
            if (currentRb != null)
            {
                currentRb.isKinematic = false;
            }
            
            currentEquippedItem.transform.SetParent(null);
            currentEquippedItem.gameObject.SetActive(false);
        }
        currentEquippedItem = null;
        currentRb = null; 
        isEquipped = false;
    }
    
    public PickableItem GetEquippedItem()
    {
        return currentEquippedItem;
    }
}