using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Events;

public class InteractableValve : MonoBehaviour
{
    [Header("Valve Settings")]
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
        if (!isComplete) isInteracting = true;
    }

    public void StopInteract()
    {
        isInteracting = false;
    }

    public void UpdateInteraction(float input)
    {
        if (!isInteracting || isComplete) return;

        // Rotate based on mouse input
        // We flip the input if target is negative to ensure dragging 'right' always adds progress visually if desired
        // But usually, simple addition works best: Right (+X) = Positive Angle, Left (-X) = Negative Angle.
        float rotationStep = input * sensitivity * 1f; 
        
        currentAngle += rotationStep;

        // Clamp current angle so you can't turn past 0 (start) or Target (end)
        if (targetAngle > 0)
            currentAngle = Mathf.Clamp(currentAngle, 0, targetAngle);
        else
            currentAngle = Mathf.Clamp(currentAngle, targetAngle, 0);

        // Apply Visual Rotation
        Vector3 axisVector = Vector3.forward;
        if (rotationAxis == Axis.X) axisVector = Vector3.right;
        if (rotationAxis == Axis.Y) axisVector = Vector3.up;

        transform.localRotation = initialRotation * Quaternion.AngleAxis(currentAngle, axisVector);

        // Check for completion
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
        onValveComplete.Invoke(); // Fires the event to unlock your hatch
        visibleValve.gameObject.SetActive(false); // Hide the visible hatch
        hiddenValve.gameObject.SetActive(true); // Show the hidden hatch
    }
}