using System.Collections.Generic;
using UnityEngine;
using Extensions.UtilityAI.ConsiderationBases;

namespace Extensions.UtilityAI.Parser
{
    /** <summary>Result of generating one action ScriptableObject from an <see cref="ActionDefNode"/>.</summary> */
    public sealed class GeneratedAction
    {
        public string Name;
        public AIActionBase Asset;
    }

    /** <summary>
     * Context passed to every <see cref="IActionCodeGen"/> during generation.
     * </summary>
     */
    public sealed class CodeGenContext
    {
        public string OutputDirectory;
        public ConsiderationBuilder ConsiderationBuilder;
    }

    /** <summary>
     * Plug-in interface for generating a concrete <see cref="AIActionBase"/> from an
     * <see cref="ActionDefNode"/>. Register implementations via
     * <see cref="AiDefCodeGenerator.RegisterActionGen"/>.
     * </summary>
     */
    public interface IActionCodeGen
    {
        string ActionTypeKeyword { get; }
        GeneratedAction Generate(ActionDefNode node, CodeGenContext ctx);
        string Serialise(AIActionBase action);
    }

    // ── Consideration builder ────────────────────────────────────────────────────

    /** <summary>
     * Converts <see cref="ConsiderationNode"/> AST nodes into concrete
     * <see cref="Consideration"/> instances ready for assignment onto an action asset.
     * </summary>
     */
    public sealed class ConsiderationBuilder
    {
        public Consideration Build(ConsiderationNode node)
        {
            return node switch
            {
                ConstantConsiderationNode  c  => BuildConstant(c),
                RandomConsiderationNode    r  => BuildRandom(r),
                CurveConsiderationNode     c  => BuildCurve(c),
                InRangeConsiderationNode   i  => BuildInRange(i),
                BoolConsiderationNode      b  => BuildBoolC(b),
                TargetExistsConsiderationNode t  => BuildTargetExists(t),
                ThresholdConsiderationNode th => BuildThreshold(th),
                StringMatchConsiderationNode sm => BuildStringMatch(sm),
                CompositeConsiderationNode co => BuildComposite(co),
                null => null,
                _ => throw new CodeGenException($"Unhandled consideration node type: {node.GetType().Name}")
            };
        }

        private static ConstantConsideration BuildConstant(ConstantConsiderationNode n)
            => new ConstantConsideration { value = n.Value };

        private static RandomConsideration BuildRandom(RandomConsiderationNode n)
            => new RandomConsideration { minMax = new Vector2(n.Min, n.Max) };

        private static CurveConsideration BuildCurve(CurveConsiderationNode n)
            => new CurveConsideration
            {
                contextKey = ContextKeyField.FromId(n.ContextKeyId),
                curve      = BuildAnimCurve(n.Points)
            };

        private static InRangeConsideration BuildInRange(InRangeConsiderationNode n)
            => new InRangeConsideration
            {
                targetKey   = ContextKeyField.FromId(n.ContextKeyId),
                maxDistance = n.MaxDistance,
                maxAngle    = n.MaxAngle,
                curve       = BuildAnimCurve(n.Points)
            };

        private static BoolConsideration BuildBoolC(BoolConsiderationNode n)
            => new BoolConsideration { contextKey = ContextKeyField.FromId(n.ContextKeyId), invert = n.Invert };

        private static TargetExistsConsideration BuildTargetExists(TargetExistsConsiderationNode n)
            => new TargetExistsConsideration { targetKey = ContextKeyField.FromId(n.ContextKeyId), invert = n.Invert };

        private static ThresholdConsideration BuildThreshold(ThresholdConsiderationNode n)
        {
            var mode = n.Comparison?.ToLowerInvariant() switch
            {
                "greaterthan"    => ThresholdConsideration.ComparisonMode.GreaterThan,
                "lessthan"       => ThresholdConsideration.ComparisonMode.LessThan,
                "greaterorequal" => ThresholdConsideration.ComparisonMode.GreaterOrEqual,
                "lessorequal"    => ThresholdConsideration.ComparisonMode.LessOrEqual,
                _                => ThresholdConsideration.ComparisonMode.LessThan
            };
            return new ThresholdConsideration
            {
                contextKey   = ContextKeyField.FromId(n.ContextKeyId),
                comparison   = mode,
                threshold    = n.Threshold,
                scoreIfTrue  = n.IfTrue,
                scoreIfFalse = n.IfFalse
            };
        }

        private static StringMatchConsideration BuildStringMatch(StringMatchConsiderationNode n)
            => new StringMatchConsideration
            {
                contextKey     = ContextKeyField.FromId(n.ContextKeyId),
                matchValue     = n.MatchValue,
                scoreIfMatch   = n.IfMatch,
                scoreIfNoMatch = n.IfNoMatch
            };

        private CompositeConsideration BuildComposite(CompositeConsiderationNode n)
        {
            var comp = new CompositeConsideration
            {
                allMustBeNonZero = n.AllMustBeNonZero,
                first            = n.First != null ? Build(n.First) : null
            };
            var restList = new List<Consideration>();
            var opList   = new List<CompositeConsideration.AggregationType>();
            foreach (var (op, child) in n.Rest)
            {
                restList.Add(Build(child));
                opList.Add(op.ToLowerInvariant() switch
                {
                    "multiply" or "mul" or "*" => CompositeConsideration.AggregationType.Multiply,
                    "average"  or "avg"        => CompositeConsideration.AggregationType.Average,
                    "add"      or "+"          => CompositeConsideration.AggregationType.Add,
                    "min"                      => CompositeConsideration.AggregationType.Min,
                    "max"                      => CompositeConsideration.AggregationType.Max,
                    _                          => CompositeConsideration.AggregationType.Multiply
                });
            }
            comp.rest       = restList.ToArray();
            comp.operations = opList.ToArray();
            return comp;
        }

        private static AnimationCurve BuildAnimCurve(List<(float t, float v)> points)
        {
            if (points == null || points.Count == 0)
                return AnimationCurve.Linear(0f, 1f, 1f, 0f);
            var keys = new Keyframe[points.Count];
            for (int i = 0; i < points.Count; i++)
                keys[i] = new Keyframe(points[i].t, points[i].v);
            return new AnimationCurve(keys);
        }
    }

    /** <summary>Thrown when code generation fails.</summary> */
    public sealed class CodeGenException : System.Exception
    {
        public CodeGenException(string message) : base(message) { }
    }
}


