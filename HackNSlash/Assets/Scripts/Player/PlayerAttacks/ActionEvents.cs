using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ExtensionUtils;
using UnityEditor.PackageManager;

public enum OnAttackActions
{
    None,
    DashToTarget,
    LaunchUp,
    PlungeAttack
}

namespace OnActionCallbacks
{
    public static class ActionEvents
    {
        private static Dictionary<OnAttackActions, Action<PlayerController, Attack>> OnAttackActionMap;
        private static Dictionary<KeyBind, KeyBind> AttackToHoldAttackMap;

        #region OnAttack Instantaneous Actions

        public static void AddOnAttackMethods()
        {
            OnAttackActionMap = new Dictionary<OnAttackActions, Action<PlayerController, Attack>>();
            
            OnAttackActionMap.Add(OnAttackActions.None, (pc, a) => { });

            OnAttackActionMap.Add(OnAttackActions.DashToTarget, DashToTarget);
            OnAttackActionMap.Add(OnAttackActions.LaunchUp, LaunchUp);
            OnAttackActionMap.Add(OnAttackActions.PlungeAttack, PlungeAttack);
            
            
            AttackToHoldAttackMap = new Dictionary<KeyBind, KeyBind>();
            AttackToHoldAttackMap.Add(KeyBind.LightAttack, KeyBind.LightAttackHold);
            AttackToHoldAttackMap.Add(KeyBind.HeavyAttack, KeyBind.HeavyAttackHold);
            AttackToHoldAttackMap.Add(KeyBind.AnyAttack, KeyBind.AnyAttackHold);
        }

        private static void DashToTarget(this PlayerController pc, Attack a)
        {
            GameObject target = pc.cam.targetedEnemy;

            if (target != null)
            {
                Vector3 distance = target.transform.position - pc.transform.position;
                
                pc.transform.LookAt(target.transform);
                
                pc.rb.linearVelocity = pc.rb.linearVelocity.ZeroVector3Axis();
                
                pc.rb.AddForce(distance.normalized * pc.playerData.dashForce, ForceMode.Impulse);
            }
        }

        private static void LaunchUp(this PlayerController pc, Attack a)
        {
            pc.StartCoroutine(pc.BeginLaunchUp(a));
        }
        
        private static IEnumerator BeginLaunchUp(this PlayerController pc, Attack a)
        {
            KeyBind[] holdKeys = a.keyBinds.GetHoldVersion();
            
            yield return new WaitForSeconds(InputManager.Instance.holdTime);
            
            if (!holdKeys.Any(k => InputManager.Instance.KeyMap[k]())) yield break;
            
            pc.PlayAnimationClip(a.attackClips[1], 0.01f);
            pc.rb.AddForce(Vector3.up * pc.playerData.jumpForce, ForceMode.Impulse);
            
        }
        
        private static void PlungeAttack(this PlayerController pc, Attack a)
        {
            pc.StartCoroutine(pc.BeginPlungeAttack(a));
        }
        
        private static IEnumerator BeginPlungeAttack(this PlayerController pc, Attack a)
        {
            yield return new WaitForSeconds(a.attackClips[0].length);
            
            pc.rb.AddForce(Vector3.down * pc.playerData.dashForce, ForceMode.Impulse);
        }
        
        #endregion


        public static void InvokeOnAttack(this PlayerController pc, Attack a)
        {
            OnAttackActionMap[a.onAttackAction](pc, a);
        }

        public static KeyBind[] GetHoldVersion(this KeyBind[] keys)
        {
            HashSet<KeyBind> holdKeys = new();
            
            foreach (KeyBind key in keys)
            {
                if (AttackToHoldAttackMap.TryGetValue(key, out KeyBind holdKey))
                {
                    holdKeys.Add(holdKey);
                }
                
                if (AttackToHoldAttackMap.ContainsValue(key))
                {
                    holdKeys.Add(key);
                }
            }
            
            return holdKeys.ToArray();
            
            
        }

        
    }
}
