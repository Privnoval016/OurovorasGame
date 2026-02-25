using System;
using Extensions.Utils;
using UnityEngine;

namespace Extensions.UtilityAI.ConsiderationBases
{
    /** <summary>Scores based on how close a sensor target is to the agent, within a max distance and angle cone.</summary> */
    [CreateAssetMenu(fileName = "InRangeConsideration", menuName = "UtilityAI/Considerations/InRangeConsideration")]
    public class InRangeConsideration : Consideration, ISerializationCallbackReceiver
    {
        public float maxDistance = 10f;
        public float maxAngle = 360f;

        [Tooltip("The sensor key used to look up the nearest detected target.")]
        public EnumContextKey targetKey;

        [Tooltip("Curve mapping normalised distance [0=closest, 1=farthest] to utility score.")]
        public AnimationCurve curve;

        // Legacy field kept for migration only. Do not use directly.
        [SerializeReference, HideInInspector, Obsolete]
        public ConsiderationKey legacyTargetKey;

        public override float Evaluate(IContextBase context)
        {
            var target = context.GetSensorTarget(targetKey.GetKey());
            if (target == null) return 0f;

            var brainTransform = context.GetBrainTransform();
            Vector3 toTarget = target.position - brainTransform.position;

            bool isInRange = brainTransform.forward.IsInDirectionCone(toTarget, maxAngle)
                && Vector3.Distance(brainTransform.position, target.position) <= maxDistance;

            if (!isInRange) return 0f;

            float distanceToTarget = toTarget.ZeroVector3Axis().magnitude;
            float normalizedDistance = Mathf.Clamp01(distanceToTarget / maxDistance);
            return Mathf.Clamp01(curve.Evaluate(normalizedDistance));
        }

        private void Reset()
        {
            curve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));
        }

        // Migrate old SerializeReference ConsiderationKey data into EnumContextKey on load.
        public void OnAfterDeserialize()
        {
#pragma warning disable CS0618
            if (legacyTargetKey != null && string.IsNullOrEmpty(targetKey.enumTypeName))
            {
                object key = legacyTargetKey.GetKey(out Type t);
                if (t != null && key != null)
                {
                    targetKey.enumTypeName = t.FullName;
                    targetKey.enumValue = (int)key;
                }
            }
#pragma warning restore CS0618
        }

        public void OnBeforeSerialize() { }
    }
}