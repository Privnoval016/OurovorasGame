using System;
using System.Collections.Generic;
using Extensions.CustomMath;
using PrimeTween;
using UnityEngine;
using UnityEngine.Serialization;

public class FollowWeapon : WeaponBody
{
    [HideInInspector] public WeaponBody mainWeaponBody;
    
    public Vector3 mainWeaponOffset;
    public Transform boundingCenter;
    public float maxDisplacement = 1;
    
    private SODEvaluator evaluator;

    public bool active;

    private void Awake()
    {
        SetKinematicAttributes();
        
        evaluator = GetComponent<SODEvaluator>();
        
        gameObject.SetActive(false);
    }
    
    private void Update()
    {
        UpdateKinematicAttributes();

        if (!active) return;

        MoveToTarget();
    }

    public void Activate(bool isActive, WeaponBody target)
    {
        if (isActive)
        {
            mainWeaponBody = target;
            active = true;
            gameObject.SetActive(true);
        }
        else
        {
            active = false;
            gameObject.SetActive(false);
        }
    }

    private void MoveToTarget()
    {
        if (mainWeaponBody == null || boundingCenter == null)
        {
            return;
        }
        
        evaluator.SetTargetTransform(mainWeaponBody.transform);
        Vector3 offset = mainWeaponOffset.x * weaponController.transform.right +
                         mainWeaponOffset.y * weaponController.transform.up +
                         mainWeaponOffset.z * weaponController.transform.forward;
        Vector3 targetPos = evaluator.output + offset;
        
        Vector3 displacement = targetPos - boundingCenter.position;
        if (displacement.magnitude > maxDisplacement)
        {
            targetPos = boundingCenter.position + displacement.normalized * maxDisplacement;
        }

        transform.position = targetPos + SinusoidalBob();

        transform.rotation = Quaternion.Slerp(transform.rotation,
            mainWeaponBody.transform.rotation, 0.2f);

    }
}
