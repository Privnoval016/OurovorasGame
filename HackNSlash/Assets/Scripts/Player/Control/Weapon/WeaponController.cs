using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum WeaponType
{
    None,
    SwordLeft,
    SwordRight,
    Katana,
    Sheath
}

public class WeaponController : MonoBehaviour
{
    [Header("Weapon Bodies")]
    
    public Dictionary<WeaponType, WeaponBody> weaponBodies = new Dictionary<WeaponType, WeaponBody>();
    public WeaponBody[] weaponBodyArray;
    
    public WeaponType[] activeWeaponTypes;
    [HideInInspector] public List<WeaponBody> activeWeapons = new();
    public int trailLength = 10;

    [Header("Following Weapons")] 
    public FollowWeapon[] followWeapons;
    
    
    [HideInInspector]
    public PlayerController pc;
    
    #region Monobehaviour Callbacks
    
    void Awake()
    {
        pc = GetComponent<PlayerController>();

        foreach (WeaponBody weaponBody in weaponBodyArray)
        {
            if (weaponBody != null)
            {
                weaponBodies.TryAdd(weaponBody.weaponType, weaponBody);
            }
        }
        
        
        foreach (WeaponBody weaponBody in weaponBodies.Values)
        {
            weaponBody.weaponController = this;
        }
        
        foreach (FollowWeapon followWeapon in followWeapons)
        {
            followWeapon.weaponController = this;
            followWeapon.mainWeaponBody = GetWeapon(WeaponType.Katana);
        }

        SwitchWeapon(activeWeaponTypes);

    }
    
    void Update()
    {

    }

    void FixedUpdate()
    {
        
    }
    
    #endregion
    
    #region Weapon Methods
    
    public WeaponBody GetWeapon(WeaponType weaponType)
    {
        return weaponBodies.GetValueOrDefault(weaponType, null);
    }

    public void SwitchWeapon(WeaponType[] weaponTypes, MovingStates nextState = MovingStates.NonCombat)
    {
        ResetTrail();
        foreach (WeaponBody weaponBody in weaponBodies.Values)
        {
            weaponBody.gameObject.SetActive(false);
        }
        activeWeapons.Clear();
        
        foreach (WeaponType weaponType in weaponTypes)
        {
            activeWeapons.Add(weaponBodies[weaponType]);
            weaponBodies[weaponType].gameObject.SetActive(true);
        }

        ActivateWeaponProperties(nextState);
    }

    private void ActivateWeaponProperties(MovingStates movingState)
    {
        switch (movingState)
        {
            case MovingStates.DualSword:
                foreach (var weapon in followWeapons)
                {
                    weapon.Activate(false, null);
                }
                break;
            case MovingStates.Katana:
                foreach (var weapon in followWeapons)
                {
                    weapon.Activate(true, GetWeapon(WeaponType.Katana));
                }

                break;
            
            case MovingStates.NonCombat:
                break;
                
        }
    }
    
    #endregion
    
    #region Follow Weapons

    public HashSet<LockOnTarget> EnemiesFromFollowWeapons(Attack a)
    {
        HashSet<LockOnTarget> enemies = new HashSet<LockOnTarget>();

        foreach (var weapon in pc.wc.activeWeapons)
        {
            if (weapon == null) continue;

            enemies = enemies.Union(weapon.IntersectingTargets).ToHashSet();
        }
        
        HashSet<Collider> cols = pc.psm
            .GetAllEnemiesInCapsule(pc.psm.playerData.largeRadius, pc.psm.playerData.largeRadius, 360)
            .Select(e => e.GetComponent<Collider>()).ToHashSet();
        
        if (cols.Count == 0) return enemies;
        
        cols = cols.Where(e => pc.wc.IsIntersecting(e)).ToHashSet();
        
        var enemiesFromTrail = cols.Select(e => e.GetComponent<LockOnTarget>()).ToHashSet();
        
        enemies = enemies.Union(enemiesFromTrail).ToHashSet();
        
        enemies = enemies.Where(e => e != null && e.TookDamageThisAction(a) == false).ToHashSet();
        
        
        return enemies;
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
    
    #endregion
    
    #region VFX Methods


    public void ActivateWeaponVFXByAttack(Attack a)
    {
        ElementEffect element = ElementData.GetElementFromAttack(a.element, pc);

        foreach (var weaponBody in activeWeapons)
        {
            weaponBody.ActivateVFX(element);
        }

        if (pc.psm.movingState != MovingStates.Katana) return;

        foreach (var weapon in followWeapons)
        {
            weapon.ActivateVFX(element);
        }
    }
    
    public void ActivateImbuedWeaponVFX()
    {
        if (pc.pcc.imbuedElementEffect != ElementEffect.None)
        {
            foreach (var weaponBody in activeWeapons)
            {
                weaponBody.ActivateVFX(pc.pcc.imbuedElementEffect);
            }

            if (pc.psm.movingState != MovingStates.Katana) return;

            foreach (var weapon in followWeapons)
            {
                weapon.ActivateVFX(pc.pcc.imbuedElementEffect);
            }
        }
        else
        {
            foreach (var weaponBody in activeWeapons)
            {
                weaponBody.DeactivateVFX();
            }

            if (pc.psm.movingState != MovingStates.Katana) return;

            foreach (var weapon in followWeapons)
            {
                weapon.DeactivateVFX();
            }
        }
    }
    
    public void DeactivateAllWeaponVFX()
    {
        ElementEffect element = pc.psm.currentPlayerAttack != null ? 
            ElementData.GetElementFromAttack(pc.psm.currentPlayerAttack.element, pc) : ElementEffect.None;
        
        foreach (var weaponBody in activeWeapons)
        {
            weaponBody.SetTrailElement(element);
            weaponBody.DeactivateVFX();
        }

        if (pc.psm.movingState != MovingStates.Katana) return;

        foreach (var weapon in followWeapons)
        {
            weapon.SetTrailElement(element);
            weapon.DeactivateVFX();
        }
    }
    
    public void ActivateWeaponTrailByAttack(Attack a)
    {
        ElementEffect element = ElementData.GetElementFromAttack(a.element, pc);
        
        foreach (var weaponBody in activeWeapons)
        {
            weaponBody.SetTrailElement(element);
            weaponBody.ActivateTrail();
        }
        
        if (pc.psm.movingState != MovingStates.Katana) return;

        foreach (var weapon in followWeapons)
        {
            weapon.SetTrailElement(element);
            weapon.ActivateTrail();
        }
    }
    
    public void DeactivateWeaponTrail()
    {
        foreach (var weaponBody in activeWeapons)
        {
            weaponBody.DeactivateTrail();
        }
        
        if (pc.psm.movingState != MovingStates.Katana) return;

        foreach (var weapon in followWeapons)
        {
            weapon.DeactivateTrail();
        }
    }
    
    #endregion
}