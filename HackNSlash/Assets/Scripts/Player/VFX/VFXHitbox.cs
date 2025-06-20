using System;
using UnityEngine;
using UnityEngine.Serialization;

public class VFXHitbox : MonoBehaviour
{
    [FormerlySerializedAs("vc")] public VFXController vfxController;
    [HideInInspector] public MeshRenderer meshRenderer;
    [HideInInspector] public Collider col;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        col = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        vfxController.HitboxTriggerEnter(other);
    }
    
    private void OnTriggerStay(Collider other)
    {
        vfxController.HitboxTriggerStay(other);
    }
    
    public void DisableCollider()
    {
        if (col != null)
        {
            col.enabled = false;
        }
    }
}
