using System;
using System.Collections.Generic;
using MEC;
using UnityEngine;
using PrimeTween;

namespace ExtensionUtils
{
    public static class GameUtil
    {
        
        /* 
         *  Converts the target vector to a vector relative to the basis vector (treated as the forward vector)
         */
        public static Vector3 GetRelativeVector3(this Vector3 target, Vector3 basis)
        {
            return Quaternion.FromToRotation(Vector3.forward, basis) * target;
        }
        
        public static Vector2 GetRelativeVector2(this Vector2 target, Vector2 basis)
        {
            return Quaternion.FromToRotation(Vector2.up, basis) * target;
        }
        
        #region Tweening
        
        public static void TweenDistance(this Rigidbody rb, Vector3 direction, float distance, float time, Ease ease = Ease.Default)
        {
            direction.Normalize();
            rb.linearVelocity = Vector3.zero;
            Tween.RigidbodyMovePosition(rb, rb.position + direction * distance, time, ease);
        }
        
        public static void TweenDistance(this Transform t, Vector3 direction, float distance, float time, Ease ease = Ease.Default)
        {
            direction.Normalize();
            Tween.Position(t, t.position + direction * distance, time, ease);
        }
        
        #endregion
        
        #region Rigidbody Physics
        
        public static IEnumerator<float> TraverseDistanceInTime(this Rigidbody rb, Vector3 direction, float distance, float time, Func<bool> condition = null)
        {
            rb.linearVelocity = Vector3.zero;
        
            direction.Normalize();
            Vector3 initialPosition = rb.position;
            float startTime = Time.time;

            float impulse = rb.mass * (distance / time - rb.linearVelocity.magnitude);
            rb.AddForce(direction * impulse, ForceMode.Impulse);
        
            yield return Timing.WaitUntilTrue(() => Vector3.Distance(rb.position, initialPosition) >= distance || 
                                                    Time.time - startTime >= time || (condition != null && condition()));
        
            rb.linearVelocity = Vector3.zero;
        }

        public static IEnumerator<float> TraverseWithVelocity(this Rigidbody rb, Vector3 direction, float magnitude,
            Func<bool> loopCondition)
        {
            direction.Normalize();
        
            while (loopCondition())
            {
                rb.linearVelocity = direction * magnitude;
                yield return Timing.WaitForOneFrame;
            }
        
            rb.linearVelocity = Vector3.zero;
        }
        
        #endregion
    }
}
