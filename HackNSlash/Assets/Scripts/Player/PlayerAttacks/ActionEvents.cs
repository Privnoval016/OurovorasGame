using System;
using System.Collections.Generic;
using UnityEngine;
using ExtensionUtils;
using UnityEditor.PackageManager;

public enum OnAttackActions
{
    None,
    DashToTarget,
    LaunchUp
}

namespace OnActionCallbacks
{
    public static class ActionEvents
    {
        private static Dictionary<OnAttackActions, Action<PlayerController>> OnAttackActionMap;

        #region OnAttack Methods

        public static void AddOnAttackMethods()
        {
            OnAttackActionMap = new Dictionary<OnAttackActions, Action<PlayerController>>();
            
            OnAttackActionMap.Add(OnAttackActions.None, (pc) => { });

            OnAttackActionMap.Add(OnAttackActions.DashToTarget, DashToTarget);
            OnAttackActionMap.Add(OnAttackActions.LaunchUp, LaunchUp);
        }

        private static void DashToTarget(this PlayerController pc)
        {
            GameObject target = pc.cam.targetedEnemy;

            if (target != null)
            {
                Vector3 targetDirection = target.transform.position - pc.transform.position;
                pc.rb.AddForce(targetDirection.normalized * 10, ForceMode.Impulse);
            }
        }

        private static void LaunchUp(this PlayerController pc)
        {
            float force = pc.playerData.doubleJumpForce;

            pc.rb.linearVelocity = pc.rb.linearVelocity.ZeroVector3Axis();

            pc.rb.AddForce(Vector3.up * force, ForceMode.Impulse);
        }



        public static void InvokeOnAttack(this PlayerController pc, Attack a)
        {
            OnAttackActionMap[a.onAttackAction](pc);
        }

        #endregion
    }
}
