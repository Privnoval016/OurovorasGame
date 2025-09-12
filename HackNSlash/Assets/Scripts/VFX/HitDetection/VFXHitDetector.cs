using System;
using UnityEngine;

public abstract class VFXHitDetector
{
    public VFXController vfx;
    public Transform creatorTransform;

    protected VFXHitDetector(VFXController vfx, Transform creatorTransform = null)
    {
        this.vfx = vfx;
        this.creatorTransform = creatorTransform;
    }

    public abstract void HitboxTriggerEnter(Collider other);

    public abstract void HitboxTriggerStay(Collider other);
}
