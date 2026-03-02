using System;
using UnityEngine;

namespace Extensions.UtilityAI.ConsiderationBases
{
    [Serializable]
    public class ConstantConsideration : Consideration
    {
        [Range(0f, 1f)] public float value = 0.5f;
        public override float Evaluate(IContextBase context) => value;
        public override string DisplayName => "Constant";
    }

    [Serializable]
    public class RandomConsideration : Consideration
    {
        public Vector2 minMax = new(0f, 1f);
        public override float Evaluate(IContextBase context) => UnityEngine.Random.Range(minMax.x, minMax.y);
        public override string DisplayName => "Random";
    }

    [Serializable]
    public class CurveConsideration : Consideration
    {
        public ContextKeyField contextKey;
        public AnimationCurve curve = AnimationCurve.Linear(0, 1, 1, 0);

        public override float Evaluate(IContextBase context)
        {
            var key = contextKey.Resolve();
            float v = key != null ? context.GetData<float>(key) : 0f;
            return Mathf.Clamp01(curve.Evaluate(v));
        }

        public override string DisplayName => "Curve";
    }

    [Serializable]
    public class InRangeConsideration : Consideration
    {
        public ContextKeyField targetKey;
        public float maxDistance = 10f;
        [Range(0f, 360f)] public float maxAngle = 360f;
        public AnimationCurve curve = AnimationCurve.Linear(0, 1, 1, 0);

        public override float Evaluate(IContextBase context)
        {
            // Resolve the ContextKey object so the dictionary lookup works correctly.
            var key = targetKey.Resolve() as ContextKey<Transform>;
            var target = key != null ? context.GetSensorTarget(key) : null;
            if (target == null) return 0f;

            var origin = context.GetBrainTransform();
            if (origin == null) return 0f;

            Vector3 toTarget = target.position - origin.position;
            float dist = new Vector3(toTarget.x, 0f, toTarget.z).magnitude;

            if (dist > maxDistance) return 0f;

            if (maxAngle < 360f)
            {
                float angle = Vector3.Angle(new Vector3(origin.forward.x, 0f, origin.forward.z),
                                            new Vector3(toTarget.x, 0f, toTarget.z));
                if (angle > maxAngle * 0.5f) return 0f;
            }

            return Mathf.Clamp01(curve.Evaluate(dist / maxDistance));
        }

        public override string DisplayName => "In Range";
    }

    [Serializable]
    public class BoolConsideration : Consideration
    {
        public ContextKeyField contextKey;
        public bool invert;

        public override float Evaluate(IContextBase context)
        {
            var key = contextKey.Resolve();
            bool v = key != null ? context.GetData<bool>(key) : false;
            return (invert ? !v : v) ? 1f : 0f;
        }

        public override string DisplayName => "Bool";
    }

    [Serializable]
    public class TargetExistsConsideration : Consideration
    {
        public ContextKeyField targetKey;
        public bool invert;

        public override float Evaluate(IContextBase context)
        {
            var key = targetKey.Resolve() as ContextKey<Transform>;
            var target = key != null ? context.GetSensorTarget(key) : null;
            bool exists = target != null;
            return (invert ? !exists : exists) ? 1f : 0f;
        }

        public override string DisplayName => "Target Exists";
    }

    [Serializable]
    public class ThresholdConsideration : Consideration
    {
        public enum ComparisonMode { GreaterThan, LessThan, GreaterOrEqual, LessOrEqual }

        public ContextKeyField contextKey;
        public ComparisonMode comparison = ComparisonMode.LessThan;
        [Range(0f, 1f)] public float threshold = 0.5f;
        [Range(0f, 1f)] public float scoreIfTrue = 1f;
        [Range(0f, 1f)] public float scoreIfFalse;

        public override float Evaluate(IContextBase context)
        {
            var key = contextKey.Resolve();
            float v = key != null ? context.GetData<float>(key) : 0f;
            bool pass = comparison switch
            {
                ComparisonMode.GreaterThan    => v > threshold,
                ComparisonMode.LessThan       => v < threshold,
                ComparisonMode.GreaterOrEqual => v >= threshold,
                ComparisonMode.LessOrEqual    => v <= threshold,
                _                             => false
            };
            return pass ? scoreIfTrue : scoreIfFalse;
        }

        public override string DisplayName => "Threshold";
    }

    [Serializable]
    public class StringMatchConsideration : Consideration
    {
        public ContextKeyField contextKey;
        public string matchValue;
        [Range(0f, 1f)] public float scoreIfMatch;
        [Range(0f, 1f)] public float scoreIfNoMatch = 1f;

        public override float Evaluate(IContextBase context)
        {
            var key = contextKey.Resolve();
            string v = key != null ? context.GetData<string>(key) : null;
            return v == matchValue ? scoreIfMatch : scoreIfNoMatch;
        }

        public override string DisplayName => "String Match";
    }

    [Serializable]
    public class CompositeConsideration : Consideration
    {
        public enum AggregationType { Multiply, Average, Add, Min, Max }

        public bool allMustBeNonZero;

        [SerializeReference] public Consideration first;
        [SerializeReference] public Consideration[] rest = Array.Empty<Consideration>();
        public AggregationType[] operations = Array.Empty<AggregationType>();

        public override float Evaluate(IContextBase context)
        {
            if (first == null) return 0f;
            float score = first.Evaluate(context);
            if (allMustBeNonZero && score == 0f) return 0f;

            int opCount = Mathf.Min(rest.Length, operations.Length);
            for (int i = 0; i < opCount; i++)
            {
                if (rest[i] == null) continue;
                float s = rest[i].Evaluate(context);
                if (allMustBeNonZero && s == 0f) return 0f;
                score = operations[i] switch
                {
                    AggregationType.Multiply => score * s,
                    AggregationType.Average  => (score + s) * 0.5f,
                    AggregationType.Add      => score + s,
                    AggregationType.Min      => Mathf.Min(score, s),
                    AggregationType.Max      => Mathf.Max(score, s),
                    _                        => score
                };
            }
            return Mathf.Clamp01(score);
        }

        public override string DisplayName => "Composite";
    }

    /** <summary>
     * Inline consideration that delegates to a linked <see cref="ConsiderationAsset"/> SO.
     * When the asset is changed, all actions using this link update automatically.
     * </summary>
     */
    [Serializable]
    public class LinkedAssetConsideration : Consideration
    {
        public ConsiderationAsset asset;

        public override float Evaluate(IContextBase context)
            => asset?.consideration?.Evaluate(context) ?? 0f;

        public override string DisplayName => asset != null ? $"↗ {asset.name}" : "↗ Linked (empty)";
    }
}

