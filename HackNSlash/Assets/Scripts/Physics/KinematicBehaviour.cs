using System;
using UnityEngine;

public class KinematicBehaviour : MonoBehaviour
{
    [Header("Velocity and Acceleration")]
    [HideInInspector] public Vector3 discreteVelocity;
    [HideInInspector] public Vector3 discreteAcceleration;
    private Vector3 lastPosition, lastVelocity;

    protected virtual void UpdateKinematicAttributes()
    {
        discreteVelocity = (transform.position - lastPosition) / Time.deltaTime;
        discreteAcceleration = (discreteVelocity - lastVelocity) / Time.deltaTime;

        lastPosition = transform.position;
        lastVelocity = discreteVelocity;
    }
    
    protected virtual void SetKinematicAttributes()
    {
        lastPosition = transform.position;
        lastVelocity = Vector3.zero;
    }
    
    public Vector3 DeltaPosition(float time)
    {
        return (discreteVelocity * time) + (0.5f * time * time * discreteAcceleration);
    }
    
    public Vector3 SinusoidalBob(Vector3 direction = default, float amplitude = 0.1f, float frequency = 2f)
    {
        if (direction == default)
        {
            direction = Vector3.up;
        }
        
        return Mathf.Sin(Time.time * frequency) * amplitude * direction;
    }
}
