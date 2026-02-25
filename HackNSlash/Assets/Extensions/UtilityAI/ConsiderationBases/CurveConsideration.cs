using System;
using UnityEngine;

namespace Extensions.UtilityAI.ConsiderationBases
{
    /**
     * <summary>
     * A consideration that evaluates an input value using a customizable animation curve.
     * The curve maps input values (typically between 0 and 1) to output utility scores (also between 0 and 1).
     * The input value is retrieved from the AI's context using <see cref="contextKey"/>.
     * </summary>
     */
    [CreateAssetMenu(fileName = "CurveConsideration", menuName = "UtilityAI/Considerations/CurveConsideration", order = 0)]
    public class CurveConsideration : Consideration, ISerializationCallbackReceiver
    {
        public AnimationCurve curve;

        [Tooltip("The context key whose float value is fed into the curve.")]
        public EnumContextKey contextKey;

        // Legacy field kept for migration only. Do not use directly.
        [SerializeReference, HideInInspector, Obsolete]
        public ConsiderationKey legacyContextKey;

        public override float Evaluate(IContextBase context)
        {
            float inputValue = context.GetData<float>(contextKey.GetKey());
            return Mathf.Clamp01(curve.Evaluate(inputValue));
        }

        private void Reset()
        {
            curve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));
        }

        // Migrate old SerializeReference ConsiderationKey data into EnumContextKey on load.
        public void OnAfterDeserialize()
        {
#pragma warning disable CS0618
            if (legacyContextKey != null && string.IsNullOrEmpty(contextKey.enumTypeName))
            {
                object key = legacyContextKey.GetKey(out Type t);
                if (t != null && key != null)
                {
                    contextKey.enumTypeName = t.FullName;
                    contextKey.enumValue = (int)key;
                }
            }
#pragma warning restore CS0618
        }

        public void OnBeforeSerialize() { }
    }
}