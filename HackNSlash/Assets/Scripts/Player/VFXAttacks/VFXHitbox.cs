using System;
using UnityEngine;
using UnityEngine.Serialization;

public class VFXHitbox : MonoBehaviour
{
    [FormerlySerializedAs("vc")] public VFXController vfxController;
    [HideInInspector] public MeshRenderer meshRenderer;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    private void OnTriggerEnter(Collider other)
    {
        vfxController.HitboxTriggerEnter(other);
    }
    
    private void OnTriggerStay(Collider other)
    {
        vfxController.HitboxTriggerStay(other);
    }
}
