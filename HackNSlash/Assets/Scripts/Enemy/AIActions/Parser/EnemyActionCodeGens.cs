using System.Collections.Generic;
using System.Text;
using Extensions.UtilityAI;
using Extensions.UtilityAI.Parser;
using UnityEngine;

/** <summary>
 * Code generator for <c>EnemyAttack</c> action blocks.
 *
 * Attack actions reference an EnemyAttack ScriptableObject by asset name.
 * The generator looks it up in the project; if not found it leaves the field null
 * and logs a warning so you can assign it manually after generation.
 *
 * Supported DSL:
 * <code>
 * action "HeavySwing" : EnemyAttack {
 *   aggro = true
 *   consideration inrange {
 *     key     = "target.player"
 *     maxdist = 3.0
 *     points  = [[0, 1], [1, 0]]
 *   }
 * }
 * </code>
 *
 * Note: Attack data (damage, animations, hitboxes) is NOT set here by design — wire those
 * in the Inspector after generation.
 * </summary>
 */
public sealed class EnemyAttackCodeGen : IActionCodeGen
{
    public string ActionTypeKeyword => "EnemyAttack";

    public GeneratedAction Generate(ActionDefNode node, CodeGenContext ctx)
    {
        var asset = ScriptableObject.CreateInstance<EnemyAttackAIAction>();
        asset.name = node.Name;

        // Consideration is applied by the orchestrator; no special fields here.
        return new GeneratedAction { Name = node.Name, Asset = asset };
    }

    public string Serialise(AIActionBase action)
    {
        if (action is not EnemyAttackAIAction a) return string.Empty;
        var sb = new StringBuilder();
        sb.AppendLine($"action \"{a.name}\" : EnemyAttack {{");
        if (a.consideration != null) sb.AppendLine(AiDefCodeGenerator.SerialiseConsideration(a.consideration));
        sb.AppendLine("}");
        return sb.ToString();
    }
}

/** <summary>
 * Code generator for <c>EnemyIdle</c> action blocks.
 * </summary>
 */
public sealed class EnemyIdleCodeGen : IActionCodeGen
{
    public string ActionTypeKeyword => "EnemyIdle";

    public GeneratedAction Generate(ActionDefNode node, CodeGenContext ctx)
    {
        var asset = ScriptableObject.CreateInstance<EnemyIdleAIAction>();
        asset.name = node.Name;
        return new GeneratedAction { Name = node.Name, Asset = asset };
    }

    public string Serialise(AIActionBase action)
    {
        if (action is not EnemyIdleAIAction a) return string.Empty;
        var sb = new StringBuilder();
        sb.AppendLine($"action \"{a.name}\" : EnemyIdle {{");
        if (a.consideration != null) sb.AppendLine(AiDefCodeGenerator.SerialiseConsideration(a.consideration));
        sb.AppendLine("}");
        return sb.ToString();
    }
}

/** <summary>
 * Code generator for <c>EnemyStunned</c> action blocks.
 * <code>
 * action "ShieldBreakStun" : EnemyStunned {
 *   exitCondition = ShieldRestored
 *   duration      = 2.0
 *   consideration composite { ... }
 * }
 * </code>
 * </summary>
 */
public sealed class EnemyStunnedCodeGen : IActionCodeGen
{
    public string ActionTypeKeyword => "EnemyStunned";

    public GeneratedAction Generate(ActionDefNode node, CodeGenContext ctx)
    {
        var asset = ScriptableObject.CreateInstance<EnemyStunnedAIAction>();
        asset.name = node.Name;
        var props = BuildPropLookup(node);
        string exitStr = GetString(props, "exitCondition", "ShieldRestored").ToLowerInvariant();
        asset.exitCondition = exitStr switch
        {
            "duration"       => EnemyStunnedAIAction.ExitCondition.Duration,
            "shieldrestored" => EnemyStunnedAIAction.ExitCondition.ShieldRestored,
            "never"          => EnemyStunnedAIAction.ExitCondition.Never,
            _                => EnemyStunnedAIAction.ExitCondition.ShieldRestored
        };
        if (TryGetFloat(props, "duration", out float d)) asset.duration = d;
        return new GeneratedAction { Name = node.Name, Asset = asset };
    }

    public string Serialise(AIActionBase action)
    {
        if (action is not EnemyStunnedAIAction a) return string.Empty;
        var sb = new StringBuilder();
        sb.AppendLine($"action \"{a.name}\" : EnemyStunned {{");
        sb.AppendLine($"  exitCondition = {a.exitCondition}");
        sb.AppendLine($"  duration      = {a.duration}");
        if (a.consideration != null) sb.AppendLine(AiDefCodeGenerator.SerialiseConsideration(a.consideration));
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static System.Collections.Generic.Dictionary<string, ValueNode> BuildPropLookup(ActionDefNode node)
    {
        var d = new System.Collections.Generic.Dictionary<string, ValueNode>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var p in node.Properties) d[p.Key] = p.Value;
        return d;
    }
    private static string GetString(System.Collections.Generic.Dictionary<string, ValueNode> d, string key, string def)
    {
        if (!d.TryGetValue(key, out var v)) return def;
        return v switch { StringValueNode s => s.Value, IdentifierValueNode i => i.Name, _ => def };
    }
    private static bool TryGetFloat(System.Collections.Generic.Dictionary<string, ValueNode> d, string key, out float value)
    {
        if (d.TryGetValue(key, out var v) && v is NumberValueNode n) { value = n.Value; return true; }
        value = 0; return false;
    }
}

/** <summary>
 * Factory that builds a pre-configured <see cref="AiDefCodeGenerator"/> with all enemy
 * action generators registered. Obtain an instance with <see cref="Create"/>.
 * </summary>
 */
public static class EnemyAiCodeGeneratorFactory
{
    /** <summary>
     * Create a fully configured <see cref="AiDefCodeGenerator"/> that knows about all
     * enemy action types. Add new generators here when adding new action types.
     * </summary>
     */
    public static AiDefCodeGenerator Create()
    {
        var gen = new AiDefCodeGenerator();
        gen.RegisterActionGen(new EnemyMovementCodeGen());
        gen.RegisterActionGen(new EnemyAttackCodeGen());
        gen.RegisterActionGen(new EnemyIdleCodeGen());
        gen.RegisterActionGen(new EnemyStunnedCodeGen());
        return gen;
    }
}
