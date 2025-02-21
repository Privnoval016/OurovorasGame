using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Animancer;
using AYellowpaper.SerializedCollections;
using JetBrains.Annotations;
using UnityEngine.Serialization;
using UnityEngine.VFX;

public enum WeaponType
{
    None,
    SwordLeft,
    SwordRight,
    Katana,
}

public class WeaponController : MonoBehaviour
{
    [SerializedDictionary("WeaponType", "Weapon Object")]
    public SerializedDictionary<WeaponType, WeaponBody> weaponBodies;
    
    public WeaponType[] activeWeaponTypes;
    [HideInInspector] public List<WeaponBody> activeWeapons = new();
    public int trailLength = 10;
    
    [HideInInspector]
    public PlayerController player;
    
    void Awake()
    {
        foreach (WeaponBody weaponBody in weaponBodies.Values)
        {
            weaponBody.weaponController = this;
        }
        
        foreach (WeaponType weaponType in activeWeaponTypes)
        {
            activeWeapons.Add(weaponBodies[weaponType]);
        }
        
    }
    
    void Update()
    {

    }

    void FixedUpdate()
    {
        
    }
    
    public WeaponBody GetWeapon(WeaponType weaponType)
    {
        return weaponBodies[weaponType];
    }
    
    public void SwitchWeapon(WeaponType[] weaponTypes)
    {
        ResetTrail();
        activeWeapons.Clear();
        foreach (WeaponType weaponType in weaponTypes)
        {
            activeWeapons.Add(weaponBodies[weaponType]);
        }
    }
    
    
    public bool IsIntersecting(Collider col, float distToContinue = 0)
    {
        foreach (WeaponBody weaponBody in activeWeapons)
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
        foreach (WeaponBody weaponBody in activeWeapons)
        {
            weaponBody.ResetTrail();
        }
    }
}
