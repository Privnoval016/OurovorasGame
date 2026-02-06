using System;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using Drakkar.GameUtils;
using UnityEngine;

public class WeaponBody : KinematicBehaviour, IContactDetector
{
    
    [Header("Weapon Control Info")]
    public WeaponType weaponType = WeaponType.None;
    
    [HideInInspector] public WeaponController weaponController;
    
    [SerializeField] protected Transform[] trailTransforms;
    
    protected Queue<Vector3[]> trailPositions = new Queue<Vector3[]>();
    public int trailLength => weaponController.trailLength;

    [HideInInspector] public HashSet<LockOnTarget> IntersectingTargets = new();
    
    
    [Header("Effects")]
    [SerializeField] private ElementObjectInfo[] vfxObjects;
    private Dictionary<ElementEffect, GameObject> vfxObjectDict = new Dictionary<ElementEffect, GameObject>();
    public DrakkarTrail vfxTrail;
    
    
    #region Monobehaviour Callbacks
    
    private void Awake()
    {
        SetKinematicAttributes(); 
        DeactivateVFX();
    }

    private void Update()
    {
        UpdateKinematicAttributes();
    }

    private void FixedUpdate()
    {
        UpdateTrail();
    }
    
    #endregion
    
    #region VFX

    public void ActivateVFX(ElementEffect elementEffect)
    {
        foreach (var vfx in vfxObjectDict)
        {
            if (vfx.Key == elementEffect && vfx.Value != null)
            {
                vfx.Value.SetActive(true);
            }
            else if (vfx.Value != null)
            {
                vfx.Value.SetActive(false);
            }
        }
    }
    
    public void DeactivateVFX()
    {
        foreach (var vfx in vfxObjectDict.Values)
        {
            if (vfx != null)
            {
                vfx.SetActive(false);
            }
        }
    }
    
    public void ActivateTrail()
    {
        if (vfxTrail != null)
        {
            vfxTrail.Begin();
        }
    }
    
    public void DeactivateTrail()
    {
        if (vfxTrail != null)
        {
            vfxTrail.End();
        }
    }
    
    public void SetTrailElement(ElementEffect elementEffect)
    {
        if (vfxTrail != null)
        {
            vfxTrail.TrailMaterial = Services.Get<ElementSystem>().GetElementData(elementEffect).weaponTrailMaterial;
        }
        
    }
    
    #endregion


    #region Weapon Trail
    
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
            if (tempQueue.Count < 2)
            {
                newPositions[i] = lastPositions[i];
                continue;
            }
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
    
    #endregion
    
    #region Inspector Events
    
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

    private void OnValidate()
    {
        if (vfxObjects == null || vfxObjects.Length == 0) return;

        vfxObjectDict.Clear();
        foreach (ElementObjectInfo info in vfxObjects)
        {
            if (info.elementObject != null && !vfxObjectDict.ContainsKey(info.elementType))
            {
                vfxObjectDict.Add(info.elementType, info.elementObject);
            }
        }
    }
    
    #endregion
    
    public Vector3 GetClosestPointOnCollider(Collider col)
    {
        Vector3 closestPoint = Vector3.zero;
        
        if (col == null) return closestPoint;

        float minDistance = float.MaxValue;
        foreach (Transform t in trailTransforms)
        {
            closestPoint = col.ClosestPoint(t.position);
            float distance = Vector3.Distance(t.position, closestPoint);
            if (distance < minDistance)
            {
                minDistance = distance;
            }
        }

        return closestPoint;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out LockOnTarget target))
        {
            IntersectingTargets.Add(target);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out LockOnTarget target))
        {
            IntersectingTargets.Remove(target);
        }
    }

    [Serializable]
    public class ElementObjectInfo
    {
        public ElementEffect elementType;
        public GameObject elementObject;
    }
}

