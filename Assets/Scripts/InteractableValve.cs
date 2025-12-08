using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Events;

public class InteractableValve : MonoBehaviour
{
    [Header("Valve Settings")]
    [Tooltip("Unique identifier for this valve. Required for save/load.")]
    public string valveID = "";
    [Tooltip("If true, the valve cannot be interacted with.")]
    public bool isLocked = false;
    [Tooltip("The axis to rotate around.")]
    public Axis rotationAxis = Axis.Z;
    [Tooltip("How fast the valve spins when dragging.")]
    public float sensitivity = 0.1f;
    [Tooltip("Total degrees needed to turn to unlock/open (e.g. 720 for 2 full spins).")]
    public float targetAngle = 720f; 

    [Header("Hatch References")]
    [Tooltip("The visible valve that is present before the valve is fully turned.")]
    public Transform visibleValve;
    [Tooltip("The hidden valve  that appears when the valve is fully turned.")]
    public Transform hiddenValve;

    public enum Axis { X, Y, Z }

    [Header("Completion Event")]
    [Tooltip("What happens when the valve is fully turned? (e.g., Unlock a door)")]
    public UnityEvent onValveComplete;

    private float currentAngle = 0f;
    private Quaternion initialRotation;
    private bool isInteracting = false;
    private bool isComplete = false;

    void Start()
    {
        initialRotation = transform.localRotation;
    }

    public void StartInteract()
    {
        if (!isComplete && !isLocked) isInteracting = true;

        if (isLocked)
        {
            Debug.Log("Valve is locked and cannot be interacted with.");
        }
    }

    public void StopInteract()
    {
        isInteracting = false;
    }

    public void UpdateInteraction(float input)
    {
        if (!isInteracting || isComplete || isLocked) return;

        float rotationStep = input * sensitivity * 1f; 
        
        currentAngle += rotationStep;

        if (targetAngle > 0)
            currentAngle = Mathf.Clamp(currentAngle, 0, targetAngle);
        else
            currentAngle = Mathf.Clamp(currentAngle, targetAngle, 0);

        Vector3 axisVector = Vector3.forward;
        if (rotationAxis == Axis.X) axisVector = Vector3.right;
        if (rotationAxis == Axis.Y) axisVector = Vector3.up;

        transform.localRotation = initialRotation * Quaternion.AngleAxis(currentAngle, axisVector);

        if (Mathf.Abs(currentAngle) >= Mathf.Abs(targetAngle))
        {
            CompleteValve();
        }
    }

    private void CompleteValve()
    {
        isComplete = true;
        isInteracting = false;
        Debug.Log("Valve Complete!");
        onValveComplete.Invoke(); 
        visibleValve.gameObject.SetActive(false); 
        hiddenValve.gameObject.SetActive(true); 
    }

    public SaveData.SavedValveState GetSaveState()
    {
        bool visibleActive = visibleValve != null && visibleValve.gameObject.activeSelf;
        bool hiddenActive = hiddenValve != null && hiddenValve.gameObject.activeSelf;
        return new SaveData.SavedValveState(valveID, currentAngle, isComplete, isLocked, visibleActive, hiddenActive);
    }

    public void LoadState(SaveData.SavedValveState state)
    {
        currentAngle = state.currentAngle;
        isComplete = state.isComplete;
        isLocked = state.isLocked;

        Vector3 axisVector = Vector3.forward;
        if (rotationAxis == Axis.X) axisVector = Vector3.right;
        if (rotationAxis == Axis.Y) axisVector = Vector3.up;

        transform.localRotation = initialRotation * Quaternion.AngleAxis(currentAngle, axisVector);

        if (visibleValve != null) visibleValve.gameObject.SetActive(state.visibleValveActive);
        if (hiddenValve != null) hiddenValve.gameObject.SetActive(state.hiddenValveActive);
    }

    /// <summary>
    /// Locks the valve, preventing interaction. Can be called from Unity events.
    /// </summary>
    public void SetLocked(bool locked)
    {
        isLocked = locked;
        if (locked)
        {
            isInteracting = false; // Stop any current interaction
        }
    }

    /// <summary>
    /// Unlocks the valve. Can be called from Unity events.
    /// </summary>
    public void Unlock()
    {
        SetLocked(false);
    }

    /// <summary>
    /// Locks the valve. Can be called from Unity events.
    /// </summary>
    public void Lock()
    {
        SetLocked(true);
    }
}