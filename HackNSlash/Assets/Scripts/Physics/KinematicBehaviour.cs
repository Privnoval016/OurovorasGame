using System;
using UnityEngine;

public class KinematicBehaviour : MonoBehaviour
{
    [HideInInspector] public Rigidbody rb;
    
    [Header("Velocity and Acceleration")]
    [HideInInspector] public Vector3 discreteVelocity;
    [HideInInspector] public Vector3 discreteAcceleration;
    private Vector3 lastPosition, lastVelocity;

    protected virtual void UpdateKinematicAttributes()
    {
        discreteVelocity = (transform.position - lastPosition) / Time.unscaledDeltaTime;
        discreteAcceleration = (discreteVelocity - lastVelocity) / Time.unscaledDeltaTime;

        lastPosition = transform.position;
        lastVelocity = discreteVelocity;
    }
    
    protected virtual void SetKinematicAttributes()
    {
        rb = GetComponent<Rigidbody>();
        lastPosition = transform.position;
        lastVelocity = Vector3.zero;
    }
    
    protected Vector3 DeltaPosition(float time)
    {
        return (discreteVelocity * time) + (0.5f * time * time * discreteAcceleration);
    }
    
    protected Vector3 SinusoidalBob(Vector3 direction = default, float amplitude = 0.1f, float frequency = 2f)
    {
        if (direction == default)
        {
            direction = Vector3.up;
        }
        
        return Mathf.Sin(Time.time * frequency) * amplitude * direction;
    }
}
