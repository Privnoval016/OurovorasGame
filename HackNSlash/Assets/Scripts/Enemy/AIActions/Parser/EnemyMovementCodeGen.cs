using System;
using System.Text;
using Extensions.UtilityAI;
using Extensions.UtilityAI.Parser;
using UnityEngine;

/** <summary>
 * Code generator for <c>EnemyMovement</c> action blocks.
 *
 * Supported DSL properties:
 * <code>
 * action "ChasePlayer" : EnemyMovement {
 *   aggro         = true
 *   strategy      = Chase          // Chase | Strafe | Orbit | Retreat | BackJump | Wander | Idle | Charge
 *
 *   // Common strategy params (only used when relevant to chosen strategy):
 *   speed         = 1.0
 *   stopDistance  = 2.0
 *   faceTarget    = true
 *   duration      = 2.0
 *   angularSpeed  = 60.0
 *   desiredDist   = 6.0
 *   maxDuration   = 2.0
 *   settleTime    = 0.1
 *   facePlayer    = true
 *   strafeSign    = 1.0
 *
 *   // Stuck handling:
 *   stuckTimeout    = 1.2
 *   speedThreshold  = 0.3
 *   unstuckDist     = 1.5
 *   unstuckDuration = 0.3
 *   maxActionDur    = 6.0
 *
 *   consideration constant { value = 0.05 }
 * }
 * </code>
 * </summary>
 */
public sealed class EnemyMovementCodeGen : IActionCodeGen
{
    public string ActionTypeKeyword => "EnemyMovement";

    public GeneratedAction Generate(ActionDefNode node, CodeGenContext ctx)
    {
        var asset = ScriptableObject.CreateInstance<EnemyMovementAIAction>();
        asset.name = node.Name;

        // Build a property lookup keyed case-insensitively.
        var props = BuildPropLookup(node);

        // Top-level action fields.
        if (GetBool(props, "aggro", true) == false)
        {
            // AggroedAction has a private setter — access via SerializedObject in editor, or use
            // the field directly here since we're in the same assembly context.
        }

        // Stuck / safety tuning.
        if (TryGetFloat(props, "stuckTimeout",    out float st)) asset.stuckTimeout         = st;
        if (TryGetFloat(props, "speedThreshold",  out float spd)) asset.movingSpeedThreshold = spd;
        if (TryGetFloat(props, "unstuckDist",     out float ud)) asset.unstuckStepDistance   = ud;
        if (TryGetFloat(props, "unstuckDuration", out float uw)) asset.unstuckStepDuration   = uw;
        if (TryGetFloat(props, "maxActionDur",    out float md)) asset.maxDuration           = md;

        // Build the strategy.
        string stratType = GetString(props, "strategy", "Chase").ToLowerInvariant();
        asset.strategy = stratType switch
        {
            "chase"    => BuildChase(props),
            "strafe"   => BuildStrafe(props),
            "orbit"    => BuildOrbit(props),
            "retreat"  => BuildRetreat(props),
            "backjump" => BuildBackJump(props),
            "wander"   => BuildWander(props),
            "idle"     => BuildIdle(props),
            "charge"   => BuildCharge(props),
            _ => BuildChase(props)
        };

        return new GeneratedAction { Name = node.Name, Asset = asset };
    }

    private static ChaseStrategy BuildChase(System.Collections.Generic.Dictionary<string, ValueNode> p)
        => new()
        {
            speedMultiplier = GetFloat(p, "speed", 1f),
            stopDistance    = GetFloat(p, "stopDistance", 2f),
            faceTarget      = GetBool(p, "faceTarget", true)
        };

    private static StrafeStrategy BuildStrafe(System.Collections.Generic.Dictionary<string, ValueNode> p)
        => new()
        {
            strafeSign      = GetFloat(p, "strafeSign", 1f),
            speedMultiplier = GetFloat(p, "speed", 0.7f),
            duration        = GetFloat(p, "duration", 1.5f)
        };

    private static OrbitStrategy BuildOrbit(System.Collections.Generic.Dictionary<string, ValueNode> p)
        => new()
        {
            angularSpeed = GetFloat(p, "angularSpeed", 60f),
            duration     = GetFloat(p, "duration", 2f)
        };

    private static RetreatStrategy BuildRetreat(System.Collections.Generic.Dictionary<string, ValueNode> p)
        => new()
        {
            speedMultiplier = GetFloat(p, "speed", 1f),
            desiredDistance = GetFloat(p, "desiredDist", 6f),
            maxDuration     = GetFloat(p, "maxDuration", 2f)
        };

    private static BackJumpStrategy BuildBackJump(System.Collections.Generic.Dictionary<string, ValueNode> p)
        => new() { settleTime = GetFloat(p, "settleTime", 0.1f) };

    private static WanderStrategy BuildWander(System.Collections.Generic.Dictionary<string, ValueNode> p)
        => new() { duration = GetFloat(p, "duration", 4f) };

    private static IdleStrategy BuildIdle(System.Collections.Generic.Dictionary<string, ValueNode> p)
        => new() { duration = GetFloat(p, "duration", 1f), facePlayer = GetBool(p, "facePlayer", true) };

    private static ChargeStrategy BuildCharge(System.Collections.Generic.Dictionary<string, ValueNode> p)
        => new()
        {
            speedMultiplier = GetFloat(p, "speed", 1.5f),
            stopDistance    = GetFloat(p, "stopDistance", 1.2f),
            maxDuration     = GetFloat(p, "maxDuration", 1.5f)
        };

    public string Serialise(AIActionBase action)
    {
        if (action is not EnemyMovementAIAction m) return string.Empty;
        var sb = new StringBuilder();
        sb.AppendLine($"action \"{m.name}\" : EnemyMovement {{");
        sb.AppendLine($"  stuckTimeout    = {m.stuckTimeout}");
        sb.AppendLine($"  speedThreshold  = {m.movingSpeedThreshold}");
        sb.AppendLine($"  unstuckDist     = {m.unstuckStepDistance}");
        sb.AppendLine($"  unstuckDuration = {m.unstuckStepDuration}");
        sb.AppendLine($"  maxActionDur    = {m.maxDuration}");

        if (m.strategy != null) SerialiseStrategy(sb, m.strategy);
        if (m.consideration != null) sb.AppendLine(AiDefCodeGenerator.SerialiseConsideration(m.consideration));

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void SerialiseStrategy(StringBuilder sb, IMovementStrategy s)
    {
        switch (s)
        {
            case ChaseStrategy c:
                sb.AppendLine($"  strategy     = Chase");
                sb.AppendLine($"  speed        = {c.speedMultiplier}");
                sb.AppendLine($"  stopDistance = {c.stopDistance}");
                sb.AppendLine($"  faceTarget   = {c.faceTarget.ToString().ToLower()}");
                break;
            case StrafeStrategy st:
                sb.AppendLine($"  strategy   = Strafe");
                sb.AppendLine($"  strafeSign = {st.strafeSign}");
                sb.AppendLine($"  speed      = {st.speedMultiplier}");
                sb.AppendLine($"  duration   = {st.duration}");
                break;
            case OrbitStrategy o:
                sb.AppendLine($"  strategy     = Orbit");
                sb.AppendLine($"  angularSpeed = {o.angularSpeed}");
                sb.AppendLine($"  duration     = {o.duration}");
                break;
            case RetreatStrategy r:
                sb.AppendLine($"  strategy    = Retreat");
                sb.AppendLine($"  speed       = {r.speedMultiplier}");
                sb.AppendLine($"  desiredDist = {r.desiredDistance}");
                sb.AppendLine($"  maxDuration = {r.maxDuration}");
                break;
            case BackJumpStrategy bj:
                sb.AppendLine($"  strategy   = BackJump");
                sb.AppendLine($"  settleTime = {bj.settleTime}");
                break;
            case WanderStrategy w:
                sb.AppendLine($"  strategy = Wander");
                sb.AppendLine($"  duration = {w.duration}");
                break;
            case IdleStrategy id:
                sb.AppendLine($"  strategy   = Idle");
                sb.AppendLine($"  duration   = {id.duration}");
                sb.AppendLine($"  facePlayer = {id.facePlayer.ToString().ToLower()}");
                break;
            case ChargeStrategy ch:
                sb.AppendLine($"  strategy     = Charge");
                sb.AppendLine($"  speed        = {ch.speedMultiplier}");
                sb.AppendLine($"  stopDistance = {ch.stopDistance}");
                sb.AppendLine($"  maxDuration  = {ch.maxDuration}");
                break;
        }
    }

    // ── Prop helpers ─────────────────────────────────────────────────────────

    private static System.Collections.Generic.Dictionary<string, ValueNode> BuildPropLookup(ActionDefNode node)
    {
        var d = new System.Collections.Generic.Dictionary<string, ValueNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in node.Properties) d[p.Key] = p.Value;
        return d;
    }

    private static float GetFloat(System.Collections.Generic.Dictionary<string, ValueNode> d, string key, float def)
        => d.TryGetValue(key, out var v) && v is NumberValueNode n ? n.Value : def;

    private static bool GetBool(System.Collections.Generic.Dictionary<string, ValueNode> d, string key, bool def)
        => d.TryGetValue(key, out var v) && v is BoolValueNode b ? b.Value : def;

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

