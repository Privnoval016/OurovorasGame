using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Extensions.CustomMath;
using Extensions.StateMachine;
using Extensions.Utils;
using UnityEngine.Serialization;

public class ElementalSpirit : KinematicBehaviour
{
    #region State Machine
   
    [HideInInspector] public StateController<SpiritState> sc;
    
    #endregion
    
    #region Components

    [HideInInspector] public PlayerController pc;
    
    #endregion
    
    #region Attack Parameters

    public bool canAttack = true;
    
    [FormerlySerializedAs("lastAttackHoldTime")] public float lastAttackHoldDuration;
    
    #endregion
    
    #region Movement Parameters
    
    [HideInInspector] public SODEvaluator evaluator;

    public Transform[] targets;
    
    public Transform ClosestTarget => targets.GetClosestTransform(transform.position);
    
    #endregion

    #region MonoBehaviour Callbacks
    private void Awake()
    {
        SetKinematicAttributes();
        
        evaluator = GetComponent<SODEvaluator>(); 
        
        sc = new StateController<SpiritState>(this);
        
        sc.ChangeState(new SpiritFollowing());
    }

    private void Update()
    {
        UpdateKinematicAttributes();
    }
    
    #endregion
    
    #region Movement Methods
    
    public void FollowPlayer()
    {
        evaluator.SetTargetTransform(ClosestTarget);

        transform.position = evaluator.output + SinusoidalBob();
    }

    public void SetRotation()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, pc.transform.rotation, 0.2f);
    }
    
    #endregion
    
    #region Attack Methods
    
    public void InvokeOnSpiritAttack(SpiritAttack a)
    {
        if (a.onSpiritAction == OnSpiritActions.None) return;
        
        sc.ChangeState(new SpiritAttacking(a));
    }

    public bool CheckSpiritAction()
    {
        if (!canAttack) return false;

        foreach (SpiritAttack a in pc.psm.attackData.AttackMap[AttackTypes.Spirit])
        {
            if (!pc.psm.AttackIsAvailable(a)) continue;
            
            InvokeOnSpiritAttack(a);
            
            return true;
        }
        
        return false;
    }
    
    #endregion
    
    #region Utility Methods
    
    public HashSet<LockOnTarget> HitScanEnemies(int numTargets, float radius, float height, float angle)
    {
        HashSet<LockOnTarget> enemies = new();
        
        if (pc.cam.IsLockedOn) enemies.Add(pc.psm.NearestHEnemy);
        
        int enemiesNeeded = numTargets - enemies.Count;
        
        if (enemiesNeeded > 0)
        {
            var enemyList = pc.psm.GetAllEnemiesInCapsule(radius, height, angle);
            if (enemyList != null && enemyList.Length > 0)
                enemies = enemies.Union(enemyList[0..enemiesNeeded]).ToHashSet();
        }
        
        enemies.RemoveWhere(e => e.tookDamageThisAction);
        
        return enemies;
    }
    
    #endregion
}
