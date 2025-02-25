using System;
using System.Collections.Generic;
using MEC;
using UnityEngine;
using PrimeTween;
using UnityEngine.VFX;


namespace ExtensionUtils
{
    public static class GameUtil
    {
        
        public static IEnumerator<float> RunAfterDelay(float delay, Action action)
        {
            yield return Timing.WaitForSeconds(delay);
            action();
        }
        
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
        
        #region Vector Transformations
        
        /*
         * Given a vector u, returns a vector that is radius units away from the origin and rotated by angle degrees
         */
        public static Vector3 FindRadialVector3(this Vector3 u, float radius, float angle)
        {
            u.Normalize();
            Vector3 axisOfRotation = Vector3.Cross(Vector3.forward, u);
            return Quaternion.AngleAxis(angle, axisOfRotation) * u * radius;
        }
        
        #endregion
        
        
        #region VFX Graph
        
        #region Safe Setters (String)
        public static bool SafeSetVector4(this VisualEffect vfx, string name, Vector4 value)
        {
            if (!vfx.HasVector4(name)) return false;
            vfx.SetVector4(name, value);
            return true;
        }
        
        public static bool SafeSetFloat(this VisualEffect vfx, string name, float value)
        {
            if (!vfx.HasFloat(name)) return false;
            vfx.SetFloat(name, value);
            return true;
        }
        
        public static bool SafeSetInt(this VisualEffect vfx, string name, int value)
        {
            if (!vfx.HasInt(name)) return false;
            vfx.SetInt(name, value);
            return true;
        }
        
        public static bool SafeSetBool(this VisualEffect vfx, string name, bool value)
        {
            if (!vfx.HasBool(name)) return false;
            vfx.SetBool(name, value);
            return true;
        }
        
        public static bool SafeSetTexture(this VisualEffect vfx, string name, Texture value)
        {
            if (!vfx.HasTexture(name)) return false;
            vfx.SetTexture(name, value);
            return true;
        }
        
        public static bool SafeSetMesh(this VisualEffect vfx, string name, Mesh value)
        {
            if (!vfx.HasMesh(name)) return false;
            vfx.SetMesh(name, value);
            return true;
        }
        
        public static bool SafeSetVector3(this VisualEffect vfx, string name, Vector3 value)
        {
            if (!vfx.HasVector3(name)) return false;
            vfx.SetVector3(name, value);
            return true;
        }
        
        public static bool SafeSetVector2(this VisualEffect vfx, string name, Vector2 value)
        {
            if (!vfx.HasVector2(name)) return false;
            vfx.SetVector2(name, value);
            return true;
        }
        
        public static bool SafeSetGradient(this VisualEffect vfx, string name, Gradient value)
        {
            if (!vfx.HasGradient(name)) return false;
            vfx.SetGradient(name, value);
            return true;
        }
        
        #endregion
        
        #region Safe Setters (ID)
        
        public static bool SafeSetVector4(this VisualEffect vfx, int id, Vector4 value)
        {
            if (!vfx.HasVector4(id)) return false;
            vfx.SetVector4(id, value);
            return true;
        }
        
        public static bool SafeSetFloat(this VisualEffect vfx, int id, float value)
        {
            if (!vfx.HasFloat(id)) return false;
            vfx.SetFloat(id, value);
            return true;
        }
        
        public static bool SafeSetInt(this VisualEffect vfx, int id, int value)
        {
            if (!vfx.HasInt(id)) return false;
            vfx.SetInt(id, value);
            return true;
        }
        
        public static bool SafeSetBool(this VisualEffect vfx, int id, bool value)
        {
            if (!vfx.HasBool(id)) return false;
            vfx.SetBool(id, value);
            return true;
        }
        
        public static bool SafeSetTexture(this VisualEffect vfx, int id, Texture value)
        {
            if (!vfx.HasTexture(id)) return false;
            vfx.SetTexture(id, value);
            return true;
        }
        
        public static bool SafeSetMesh(this VisualEffect vfx, int id, Mesh value)
        {
            if (!vfx.HasMesh(id)) return false;
            vfx.SetMesh(id, value);
            return true;
        }
        
        public static bool SafeSetVector3(this VisualEffect vfx, int id, Vector3 value)
        {
            if (!vfx.HasVector3(id)) return false;
            vfx.SetVector3(id, value);
            return true;
        }
        
        public static bool SafeSetVector2(this VisualEffect vfx, int id, Vector2 value)
        {
            if (!vfx.HasVector2(id)) return false;
            vfx.SetVector2(id, value);
            return true;
        }
        
        public static bool SafeSetGradient(this VisualEffect vfx, int id, Gradient value)
        {
            if (!vfx.HasGradient(id)) return false;
            vfx.SetGradient(id, value);
            return true;
            vfx.TryGetComponent(out VisualEffect vfxInstanceVFX);
        }
        
        #endregion
        
        
        #endregion
        
        public static bool TryGetComponentInChildren<T>(this GameObject go, out T component) where T : Component
        {
            component = go.GetComponentInChildren<T>();
            return component != null;
        }
    }
    
    [Serializable]
    public class TransformInfo
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale = Vector3.one;
        
        public Vector3 Forward => Rotation * Vector3.forward;
        public Vector3 Right => Rotation * Vector3.right;
        public Vector3 Up => Rotation * Vector3.up;

        public TransformInfo(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.Position = position;
            this.Rotation = rotation;
            this.Scale = scale;
        }
        
        
        public TransformInfo(Transform t, bool local = true)
        {
            if (local)
            {
                Position = t.localPosition;
                Rotation = t.localRotation;
                Scale = t.localScale;
            }
            else
            {
                Position = t.position;
                Rotation = t.rotation;
                Scale = t.lossyScale;
            }
        }
    
        /*
         * Converts the TransformInfo from a local space relative to the parent transform to world space
         */
        public TransformInfo ConvertToWorldSpace(Transform parent)
        {
            Vector3 newPos = parent.TransformPoint(Position);
            Quaternion newRot = parent.rotation * Rotation;
            Vector3 newScale = new Vector3(Scale.x * parent.lossyScale.x, Scale.y * parent.lossyScale.y, Scale.z * parent.lossyScale.z);
            
            return new TransformInfo(newPos, newRot, newScale);
        }

        /*
         * Converts the TransformInfo from world space to a local space relative to the parent transform
         */
        public TransformInfo ConvertToLocalSpace(Transform parent)
        {
            Vector3 newPos = parent.InverseTransformPoint(Position);
            Quaternion newRot = Quaternion.Inverse(parent.rotation) * Rotation;
            Vector3 newScale = new Vector3(Scale.x / parent.lossyScale.x, Scale.y / parent.lossyScale.y, Scale.z / parent.lossyScale.z);
            
            return new TransformInfo(newPos, newRot, newScale);
        }
    }
}
