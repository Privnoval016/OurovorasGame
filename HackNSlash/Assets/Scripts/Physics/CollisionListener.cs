using System;
using System.Collections.Generic;
using Extensions.Utils;
using UnityEngine;

public class CollisionListener : MonoBehaviour
{
    public bool activeMover = true;
    public float downwardStrength = 1f;

    private Dictionary<Collider, Vector3> pushbackDirections = new Dictionary<Collider, Vector3>();

    private void OnTriggerStay(Collider other)
    {
        if (!activeMover) return;
        
        if (other.TryGetComponent(out CollisionListener otherListener))
        {
            Vector3 direction = -(other.transform.position - transform.position);
            direction = (direction.ZeroVector3Axis().normalized - downwardStrength * transform.up).normalized;
            
            if (direction == Vector3.zero) direction = transform.forward;
            
            if (pushbackDirections.ContainsKey(other))
            {
                pushbackDirections[other] = direction;
            }
            else
            {
                pushbackDirections.Add(other, direction);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!activeMover) return;
        
        if (pushbackDirections.ContainsKey(other))
        {
            pushbackDirections.Remove(other);
        }
    }

    public Vector3 GetCombinedDirection()
    {
        Vector3 synthesizedDirection = Vector3.zero;
        foreach (var direction in pushbackDirections.Values)
        {
            synthesizedDirection += direction;
        }
        
        return synthesizedDirection.normalized;
    }
}
