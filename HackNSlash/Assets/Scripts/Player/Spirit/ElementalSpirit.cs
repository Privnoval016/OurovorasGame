using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Extensions.CustomMath;
using Extensions.StateMachine;
using Extensions.Utils;
using UnityEngine.Serialization;

public class ElementalSpirit : KinematicBehaviour, IContactDetector
{
    #region State Machine
   
    [HideInInspector] public StateController<SpiritState> sc;
    
    #endregion
    
    #region Components

    [HideInInspector] public PlayerController pc;
    [HideInInspector] public OnSpiritEvents ose;
    
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
        ose = GetComponent<OnSpiritEvents>();
        
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
        pc.psm.TimeSinceLastAttack.Reset();
        
        if (a.spiritActions == null || a.spiritActions.Length == 0) return;
        
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

    public Vector3 GetClosestPointOnCollider(Collider col)
    {
        if (col == null) return Vector3.zero;

        Vector3 closestPoint = col.ClosestPoint(transform.position);

        return closestPoint;
    }
}
