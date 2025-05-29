using System;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using UnityEngine;

public class WeaponBody : KinematicBehaviour
{
    public WeaponType weaponType = WeaponType.None;
    
    [HideInInspector] public WeaponController weaponController;
    
    [SerializeField] protected Transform[] trailTransforms;
    
    protected Queue<Vector3[]> trailPositions = new Queue<Vector3[]>();
    public int trailLength => weaponController.trailLength;
    
    protected PlayerAttack _lastPlayerAttack;
    protected AnimancerState lastAnimation;
    
    private void Awake()
    {
        SetKinematicAttributes(); 
    }

    private void Update()
    {
        UpdateKinematicAttributes();
    }

    private void FixedUpdate()
    {
        UpdateTrail();
    }


    public void UpdateTrail()
    {
        if (trailTransforms == null || trailTransforms.Length == 0)
        {
            return;
        }
        
        Vector3[] positions = new Vector3[trailTransforms.Length];
        for (int i = 0; i < trailTransforms.Length; i++)
        {
            positions[i] = trailTransforms[i].position;
        }
        
        trailPositions.Enqueue(positions);
        
        if (trailPositions.Count > trailLength)
        {
            trailPositions.Dequeue();
        }
    }

    public void OnDrawGizmos()
    {
        if (trailPositions == null || trailPositions.Count == 0)
        {
            return;
        }
        
        for (int i = 0; i < trailTransforms.Length; i++)
        {
            Vector3[] positions = trailPositions.Select(x => x[i]).ToArray();
            for (int j = 0; j < positions.Length - 1; j++)
            {
                Gizmos.DrawLine(positions[j], positions[j + 1]);
            }
        }
    }
    
    public bool IsIntersecting(Collider col, float distToContinue = 0)
    {
        if (trailPositions.Count == 0)
        {
            return false;
        }
        
        // add another point to the queue to predict the next position (extend the last line drawn by the points)
        Queue<Vector3[]> tempQueue = new Queue<Vector3[]>(trailPositions);
        Vector3[] lastPositions = tempQueue.Last();
        Vector3[] newPositions = new Vector3[lastPositions.Length];
        for (int i = 0; i < lastPositions.Length; i++)
        {
            newPositions[i] = lastPositions[i] + (lastPositions[i] - tempQueue.ElementAt(tempQueue.Count - 2)[i]).normalized * distToContinue;
        }
        tempQueue.Enqueue(newPositions);
        
        // calculate if the trail intersects with the collider
        for (int i = 0; i < trailTransforms.Length; i++)
        {
            Vector3[] positions = tempQueue.Select(x => x[i]).ToArray();
            for (int j = 0; j < positions.Length - 1; j++)
            {
                if (col.ClosestPoint(positions[j]).Equals(positions[j]) || col.ClosestPoint(positions[j + 1]).Equals(positions[j + 1]))
                {
                    return true;
                }
                
                if (col.bounds.Intersects(new Bounds(positions[j], positions[j + 1] - positions[j])))
                {
                    return true;
                }
            }
        }

        return false;
    }
    
    public void ResetTrail()
    {
        trailPositions.Clear();
    }
}

