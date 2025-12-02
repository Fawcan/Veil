using UnityEngine;

public interface IDestructible
{
    // The amount of force (or kinetic energy) required to destroy the object.
    float DestructionThreshold { get; }

    // Called when another object hits it with sufficient force.
    void OnSmashHit(Collision collision, float calculatedForce);
}