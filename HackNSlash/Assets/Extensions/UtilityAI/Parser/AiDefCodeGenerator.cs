using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Extensions.UtilityAI.Parser
{
    /** <summary>
     * Orchestrates the full import/export pipeline for the AI definition language.
     *
     * <list type="bullet">
     *   <item>Import: text → AST (Lexer + AiDefParser) → ScriptableObject assets (IActionCodeGen).</item>
     *   <item>Export: list of AIActionBase assets → text file.</item>
     * </list>
     *
     * Extensibility:
     * Call <see cref="RegisterActionGen"/> to add support for a new action type.
     * The generator for that type handles all property mapping and serialisation —
     * the orchestrator never needs to be modified.
     * </summary>
     */
    public sealed class AiDefCodeGenerator
    {
        private readonly Dictionary<string, IActionCodeGen> _gens =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly ConsiderationBuilder _considerationBuilder = new();

        // ── Registration ──────────────────────────────────────────────────────

        /** <summary>Register a code generator for one action type keyword.</summary> */
        public void RegisterActionGen(IActionCodeGen gen)
        {
            if (gen == null) throw new ArgumentNullException(nameof(gen));
            _gens[gen.ActionTypeKeyword] = gen;
        }

        // ── Import ────────────────────────────────────────────────────────────

        /** <summary>
         * Parse <paramref name="source"/> and generate ScriptableObject assets
         * in <paramref name="outputDir"/> (path relative to Assets/).
         * Returns the list of created assets.
         * </summary>
         */
        public List<GeneratedAction> Import(string source, string outputDir)
        {
#if !UNITY_EDITOR
            throw new InvalidOperationException("AiDefCodeGenerator.Import is editor-only.");
#else
            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentException("Source is empty.", nameof(source));

            // ── Lex + parse ───────────────────────────────────────────────────
            var tokens = new Lexer(source).Tokenize();
            var doc    = new AiDefParser(tokens).ParseDocument();

            // ── Ensure output directory exists ────────────────────────────────
            string fullDir = Path.Combine(Application.dataPath, "..", outputDir);
            Directory.CreateDirectory(fullDir);

            var ctx = new CodeGenContext
            {
                OutputDirectory      = outputDir,
                ConsiderationBuilder = _considerationBuilder
            };

            var results = new List<GeneratedAction>(doc.Actions.Count);

            foreach (var actionNode in doc.Actions)
            {
                if (!_gens.TryGetValue(actionNode.ActionType, out var gen))
                    throw new CodeGenException($"No generator registered for action type '{actionNode.ActionType}' (action '{actionNode.Name}').");

                GeneratedAction ga = gen.Generate(actionNode, ctx);
                if (ga?.Asset == null) continue;

                // Build the consideration from the parsed node.
                ConsiderationBases.Consideration builtConsideration = null;
                if (actionNode.Consideration != null)
                    builtConsideration = _considerationBuilder.Build(actionNode.Consideration);

                // Save or update the asset.
                string safeName = SanitiseName(ga.Name);
                string assetPath = $"{outputDir}/{safeName}.asset";

                var existing = AssetDatabase.LoadAssetAtPath<AIActionBase>(assetPath);
                if (existing != null)
                {
                    // Selective update: only overwrite what the .aidef file actually specifies.
                    // Fields like attack data, hitbox references, etc. that are not in the DSL
                    // are left untouched so manual inspector work is never wiped.
                    if (builtConsideration != null)
                        existing.consideration = builtConsideration;

                    // Let the generator apply its own owned fields onto the existing asset.
                    gen.ApplyToExisting(actionNode, ga.Asset, existing, ctx);

                    EditorUtility.SetDirty(existing);
                    // Point ga.Asset to the existing so callers get the right reference.
                    ga.Asset = existing;
                }
                else
                {
                    if (builtConsideration != null)
                        ga.Asset.consideration = builtConsideration;
                    AssetDatabase.CreateAsset(ga.Asset, assetPath);
                }

                results.Add(ga);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return results;
#endif
        }

        // ── Export ────────────────────────────────────────────────────────────

        /** <summary>
         * Serialise a list of <see cref="AIActionBase"/> assets back to the AI definition text format.
         * </summary>
         */
        public string Export(IEnumerable<AIActionBase> actions)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// Generated by AiDefCodeGenerator — do not edit manually unless you know what you are doing.");
            sb.AppendLine();

            foreach (var action in actions)
            {
                if (action == null) continue;

                // Find the generator that claims this type.
                IActionCodeGen gen = null;
                foreach (var g in _gens.Values)
                {
                    if (action.GetType().Name.IndexOf(g.ActionTypeKeyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        gen = g;
                        break;
                    }
                }

                if (gen == null)
                {
                    sb.AppendLine($"// WARNING: no serialiser for action type '{action.GetType().Name}' — skipped.");
                    continue;
                }

                sb.AppendLine(gen.Serialise(action));
            }

            return sb.ToString();
        }

        // ── Consideration serialiser ──────────────────────────────────────────

        /** <summary>Serialise any <see cref="ConsiderationBases.Consideration"/> to text.</summary> */
        public static string SerialiseConsideration(ConsiderationBases.Consideration c, int indent = 1)
        {
            if (c == null) return string.Empty;
            string pad = new string(' ', indent * 2);
            string inner = c switch
            {
                ConsiderationBases.ConstantConsideration  cc => $"{pad}  value = {cc.value}",
                ConsiderationBases.RandomConsideration    rc => $"{pad}  min = {rc.minMax.x}\n{pad}  max = {rc.minMax.y}",
                ConsiderationBases.CurveConsideration     cv => SerialiseKeyedCurve(cv.contextKey.ResolveId(), cv.curve, pad),
                ConsiderationBases.InRangeConsideration   ir => SerialiseInRange(ir, pad),
                ConsiderationBases.BoolConsideration      bc => $"{pad}  key = \"{bc.contextKey.ResolveId()}\"\n{pad}  invert = {bc.invert.ToString().ToLower()}",
                ConsiderationBases.TargetExistsConsideration te => $"{pad}  key = \"{te.targetKey.ResolveId()}\"\n{pad}  invert = {te.invert.ToString().ToLower()}",
                ConsiderationBases.ThresholdConsideration th => SerialiseThreshold(th, pad),
                ConsiderationBases.StringMatchConsideration sm => SerialiseStringMatch(sm, pad),
                ConsiderationBases.CompositeConsideration co => SerialiseComposite(co, indent),
                _ => $"{pad}  // unknown consideration type"
            };
            string typeName = c switch
            {
                ConsiderationBases.ConstantConsideration  _ => "constant",
                ConsiderationBases.RandomConsideration    _ => "random",
                ConsiderationBases.CurveConsideration     _ => "curve",
                ConsiderationBases.InRangeConsideration   _ => "inrange",
                ConsiderationBases.BoolConsideration      _ => "bool",
                ConsiderationBases.TargetExistsConsideration _ => "targetexists",
                ConsiderationBases.ThresholdConsideration _ => "threshold",
                ConsiderationBases.StringMatchConsideration _ => "stringmatch",
                ConsiderationBases.CompositeConsideration _ => "composite",
                _ => "unknown"
            };
            return $"{pad}consideration {typeName} {{\n{inner}\n{pad}}}";
        }

        private static string SerialiseKeyedCurve(string keyId, AnimationCurve curve, string pad)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{pad}  key = \"{keyId}\"");
            sb.Append($"{pad}  points = [");
            if (curve != null && curve.length > 0)
            {
                for (int i = 0; i < curve.length; i++)
                {
                    var kf = curve[i];
                    sb.Append($"[{kf.time:F3}, {kf.value:F3}]");
                    if (i < curve.length - 1) sb.Append(", ");
                }
            }
            sb.Append("]");
            return sb.ToString();
        }

        private static string SerialiseInRange(ConsiderationBases.InRangeConsideration ir, string pad)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{pad}  key = \"{ir.targetKey.ResolveId()}\"");
            sb.AppendLine($"{pad}  maxdist = {ir.maxDistance}");
            sb.AppendLine($"{pad}  maxangle = {ir.maxAngle}");
            sb.Append($"{pad}  points = [");
            if (ir.curve != null)
                for (int i = 0; i < ir.curve.length; i++)
                {
                    var kf = ir.curve[i];
                    sb.Append($"[{kf.time:F3}, {kf.value:F3}]");
                    if (i < ir.curve.length - 1) sb.Append(", ");
                }
            sb.Append("]");
            return sb.ToString();
        }

        private static string SerialiseThreshold(ConsiderationBases.ThresholdConsideration th, string pad)
        {
            string comp = th.comparison switch
            {
                ConsiderationBases.ThresholdConsideration.ComparisonMode.GreaterThan    => "greaterthan",
                ConsiderationBases.ThresholdConsideration.ComparisonMode.LessThan       => "lessthan",
                ConsiderationBases.ThresholdConsideration.ComparisonMode.GreaterOrEqual => "greaterorequal",
                ConsiderationBases.ThresholdConsideration.ComparisonMode.LessOrEqual    => "lessorequal",
                _ => "lessthan"
            };
            return $"{pad}  key = \"{th.contextKey.ResolveId()}\"\n{pad}  comparison = {comp}\n{pad}  threshold = {th.threshold}\n{pad}  iftrue = {th.scoreIfTrue}\n{pad}  iffalse = {th.scoreIfFalse}";
        }

        private static string SerialiseStringMatch(ConsiderationBases.StringMatchConsideration sm, string pad)
            => $"{pad}  key = \"{sm.contextKey.ResolveId()}\"\n{pad}  match = \"{sm.matchValue}\"\n{pad}  ifmatch = {sm.scoreIfMatch}\n{pad}  ifnomatch = {sm.scoreIfNoMatch}";

        private static string SerialiseComposite(ConsiderationBases.CompositeConsideration co, int indent)
        {
            string pad = new string(' ', indent * 2);
            var sb = new StringBuilder();
            sb.AppendLine($"{pad}  allMustBeNonZero = {co.allMustBeNonZero.ToString().ToLower()}");
            if (co.first != null)
            {
                sb.AppendLine($"{pad}  first");
                sb.AppendLine(SerialiseConsideration(co.first, indent + 2));
            }
            if (co.rest?.Length > 0)
            {
                sb.AppendLine($"{pad}  rest [");
                for (int i = 0; i < co.rest.Length; i++)
                {
                    string op = i < co.operations.Length ? co.operations[i].ToString().ToLower() : "multiply";
                    sb.AppendLine($"{pad}    {op}:");
                    sb.AppendLine(SerialiseConsideration(co.rest[i], indent + 3));
                    if (i < co.rest.Length - 1) sb.AppendLine(",");
                }
                sb.Append($"{pad}  ]");
            }
            return sb.ToString();
        }

        private static string SanitiseName(string name)
            => string.IsNullOrEmpty(name) ? "UnnamedAction"
               : System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z0-9_\-]", "_");
    }
}




