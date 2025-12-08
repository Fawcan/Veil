using UnityEngine;

public interface IDestructible
{
    float DestructionThreshold { get; }

    void OnSmashHit(Collision collision, float calculatedForce);
}