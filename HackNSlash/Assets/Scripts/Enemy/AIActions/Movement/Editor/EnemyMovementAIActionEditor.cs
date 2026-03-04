using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

/** <summary>
 * Custom inspector for <see cref="EnemyMovementAIAction"/>.
 * Draws every section in explicit order with no overlapping labels.
 * </summary>
 */
[CustomEditor(typeof(EnemyMovementAIAction))]
public class EnemyMovementAIActionEditor : Editor
{
    // [field: SerializeField] auto-properties store their value under this name.
    private const string k_AggroField = "<AggroedAction>k__BackingField";

    private bool _showConsideration = true;
    private bool _showStuck = true;
    private bool _showSafety = true;

    // All concrete IMovementStrategy types, found once.
    private static Type[] _strategyTypes;
    private static string[] _strategyNames;

    static EnemyMovementAIActionEditor()
    {
        RebuildStrategyList();
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void RebuildStrategyList()
    {
        var found = new List<Type>();
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var t in asm.GetTypes())
                    if (!t.IsAbstract && !t.IsInterface && typeof(IMovementStrategy).IsAssignableFrom(t))
                        found.Add(t);
            }
            catch { }
        }
        found.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

        _strategyTypes = found.ToArray();
        _strategyNames = new string[found.Count + 1];
        _strategyNames[0] = "— None —";
        for (int i = 0; i < found.Count; i++)
            _strategyNames[i + 1] = FriendlyName(found[i]);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Script field (read-only).
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.ObjectField("Script",
                MonoScript.FromScriptableObject((ScriptableObject)target),
                typeof(MonoScript), false);

        EditorGUILayout.Space(6);

        // ── Action Settings ─────────────────────────────────────────────────
        Section("Action Settings", () =>
        {
            DrawProp(k_AggroField, "Aggro Action",
                "Marks the enemy as aware/aggro while this action runs.");
        });

        // ── Consideration ───────────────────────────────────────────────────
        _showConsideration = EditorGUILayout.Foldout(_showConsideration, "Consideration", true, EditorStyles.foldoutHeader);
        if (_showConsideration)
        {
            EditorGUI.indentLevel++;
            DrawProp("consideration", children: true);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.Space(4);

        // ── Movement Strategy ───────────────────────────────────────────────
        Section("Movement Strategy", DrawStrategySection);

        // ── Animation Override ──────────────────────────────────────────────
        Section("Animation", () =>
            DrawProp("animOverride", "Anim Override",
                "Override EnemyAnimData for this action. Leave null to use the brain's own data."));

        // ── Stuck Detection ─────────────────────────────────────────────────
        _showStuck = EditorGUILayout.Foldout(_showStuck, "Stuck Detection", true, EditorStyles.foldoutHeader);
        if (_showStuck)
        {
            EditorGUI.indentLevel++;
            DrawProp("stuckTimeout",         "Timeout (s)",       "Seconds below speed threshold before considered stuck.");
            DrawProp("movingSpeedThreshold", "Speed Threshold",   "Minimum XZ speed (m/s) to count as moving.");
            DrawProp("unstuckStepDistance",  "Unstuck Step Dist", "Distance (m) of the side-step nudge.");
            DrawProp("unstuckStepDuration",  "Unstuck Step Dur",  "Duration (s) of the unstuck side-step.");
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(2);
        }

        // ── Safety ──────────────────────────────────────────────────────────
        _showSafety = EditorGUILayout.Foldout(_showSafety, "Safety", true, EditorStyles.foldoutHeader);
        if (_showSafety)
        {
            EditorGUI.indentLevel++;
            DrawProp("maxDuration", "Max Duration (s)",
                "Hard cap on total action time. 0 = unlimited.");
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(2);
        }

        serializedObject.ApplyModifiedProperties();
    }

    // ── Strategy section ──────────────────────────────────────────────────────

    private void DrawStrategySection()
    {
        var stratProp = serializedObject.FindProperty("strategy");
        if (stratProp == null) { EditorGUILayout.HelpBox("'strategy' field not found.", MessageType.Warning); return; }

        // Resolve current type and selection index.
        Type currentType = stratProp.managedReferenceValue?.GetType();
        int currentIndex = 0;
        for (int i = 0; i < _strategyTypes.Length; i++)
        {
            if (_strategyTypes[i] == currentType) { currentIndex = i + 1; break; }
        }

        EditorGUI.BeginChangeCheck();
        int chosen = EditorGUILayout.Popup(
            new GUIContent("Type", "Which movement behaviour to execute."),
            currentIndex, _strategyNames);

        if (EditorGUI.EndChangeCheck())
        {
            stratProp.managedReferenceValue = chosen == 0
                ? null
                : Activator.CreateInstance(_strategyTypes[chosen - 1]);
        }

        // Draw only the serialized (public) child properties of the strategy — not
        // private runtime fields. We walk children manually and stop at depth > 1.
        if (stratProp.managedReferenceValue != null)
        {
            EditorGUILayout.Space(4);
            EditorGUI.indentLevel++;

            // Copy the property and iterate one level deep only.
            SerializedProperty it = stratProp.Copy();
            SerializedProperty end = stratProp.GetEndProperty(false);
            bool entered = it.NextVisible(true); // step into managed reference

            while (entered && !SerializedProperty.EqualContents(it, end))
            {
                // Only draw direct children (depth == stratProp.depth + 2 for managed refs).
                if (it.depth <= stratProp.depth + 2)
                    EditorGUILayout.PropertyField(it, true);
                entered = it.NextVisible(false); // siblings only, never recurse further
            }

            EditorGUI.indentLevel--;
        }
    }

    // ── Layout helpers ────────────────────────────────────────────────────────

    private static void Section(string title, Action content)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.Space(2);
            content();
            EditorGUILayout.Space(2);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space(4);
    }

    private void DrawProp(string propName, string label = null, string tooltip = null, bool children = false)
    {
        var prop = serializedObject.FindProperty(propName);
        if (prop == null)
        {
            EditorGUILayout.HelpBox($"Field not found: '{propName}'", MessageType.Warning);
            return;
        }
        var content = label != null
            ? new GUIContent(label, tooltip)
            : new GUIContent(prop.displayName, tooltip ?? prop.tooltip);
        EditorGUILayout.PropertyField(prop, content, children);
    }

    private static string FriendlyName(Type t)
    {
        string n = t.Name;
        if (n.EndsWith("Strategy")) n = n[..^"Strategy".Length];
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < n.Length; i++)
        {
            if (i > 0 && char.IsUpper(n[i])) sb.Append(' ');
            sb.Append(n[i]);
        }
        return sb.ToString();
    }
}

