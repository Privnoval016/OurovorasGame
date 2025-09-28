using System;
using UnityEngine;

public class VFXHitbox : MonoBehaviour
{
    public VFXHitDetector HitDetector;
    [HideInInspector] public MeshRenderer meshRenderer;
    [HideInInspector] public Collider col;
    
    public bool useOnEnter = true;
    public bool useOnStay = true;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        col = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!useOnEnter) return;
        
        HitDetector.HitboxTriggerEnter(other);
    }
    
    private void OnTriggerStay(Collider other)
    {
        if (!useOnStay) return;
        
        HitDetector.HitboxTriggerStay(other);
    }
    
    public void DisableCollider()
    {
        if (col != null)
        {
            col.enabled = false;
        }
    }
    
    public void EnableCollider()
    {
        if (col != null)
        {
            col.enabled = true;
        }
    }
}
