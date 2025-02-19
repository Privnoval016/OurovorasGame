using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Animancer;
using UnityEngine.VFX;

public enum WeaponType
{
    SwordLeft,
    SwordRight,
    Katana,
}

public class WeaponController : MonoBehaviour
{
    public WeaponBody[] WeaponBodies;
    public int trailLength = 10;
    
    [HideInInspector]
    public PlayerController player;
    
    void Awake()
    {
        foreach (WeaponBody weaponBody in WeaponBodies)
        {
            weaponBody.weaponController = this;
        }
    }
    
    void Update()
    {

    }

    void FixedUpdate()
    {
        
    }
    
    
    public bool IsIntersecting(Collider col, float distToContinue = 0)
    {
        foreach (WeaponBody weaponBody in WeaponBodies)
        {
            if (weaponBody.IsIntersecting(col, distToContinue))
            {
                return true;
            }
        }

        return false;
    }
    
    public void ResetTrail()
    {
        foreach (WeaponBody weaponBody in WeaponBodies)
        {
            weaponBody.ResetTrail();
        }
    }
}
