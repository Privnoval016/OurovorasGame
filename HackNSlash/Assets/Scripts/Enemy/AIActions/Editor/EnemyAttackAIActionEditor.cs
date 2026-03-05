using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/** <summary>
 * Custom inspector for <see cref="EnemyAttackAIAction"/>.
 * Draws the nested <see cref="EnemyAttack"/> struct and provides a
 * type-picker dropdown for the <see cref="IEnemyAttackStrategy"/>
 * <c>[SerializeReference]</c> field, which Odin otherwise swallows.
 * </summary>
 */
[CustomEditor(typeof(EnemyAttackAIAction))]
public class EnemyAttackAIActionEditor : Editor
{
    private const string k_AggroField = "<AggroedAction>k__BackingField";

    private bool _showAttack = true;
    private bool _showConsideration = true;

    private static Type[] _strategyTypes;
    private static string[] _strategyNames;

    static EnemyAttackAIActionEditor()
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
                    if (!t.IsAbstract && !t.IsInterface && typeof(IEnemyAttackStrategy).IsAssignableFrom(t))
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

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.ObjectField("Script",
                MonoScript.FromScriptableObject((ScriptableObject)target),
                typeof(MonoScript), false);

        EditorGUILayout.Space(6);

        // ── Action Settings ──────────────────────────────────────────────────
        Section("Action Settings", () =>
        {
            DrawProp(k_AggroField, "Aggro Action",
                "Marks the enemy as aware/aggro while this action runs.");
            DrawProp("stats", "Player Attack Stats",
                "Optional stat override applied when this action is executed.");
            DrawProp("damageInfo", "Damage Info", children: true);
        });

        // ── Attack ───────────────────────────────────────────────────────────
        _showAttack = EditorGUILayout.Foldout(_showAttack, "Attack", true, EditorStyles.foldoutHeader);
        if (_showAttack)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.Space(2);

            var attackProp = serializedObject.FindProperty("attack");
            if (attackProp == null)
            {
                EditorGUILayout.HelpBox("'attack' field not found.", MessageType.Warning);
            }
            else
            {
                // Draw all non-strategy fields on EnemyAttack via the child properties.
                DrawAttackFieldsExceptStrategy(attackProp);

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Attack Strategy", EditorStyles.boldLabel);
                DrawStrategyDropdown(attackProp.FindPropertyRelative("enemyAttack"));
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(2);
        }

        // ── Consideration ────────────────────────────────────────────────────
        _showConsideration = EditorGUILayout.Foldout(_showConsideration, "Consideration", true, EditorStyles.foldoutHeader);
        if (_showConsideration)
        {
            EditorGUI.indentLevel++;
            DrawProp("consideration", children: true);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(2);
        }

        serializedObject.ApplyModifiedProperties();
    }

    // ── Strategy dropdown ─────────────────────────────────────────────────────

    private void DrawStrategyDropdown(SerializedProperty stratProp)
    {
        if (stratProp == null)
        {
            EditorGUILayout.HelpBox("'enemyAttack' SerializeReference field not found.", MessageType.Warning);
            return;
        }

        Type currentType = stratProp.managedReferenceValue?.GetType();
        int currentIndex = 0;
        for (int i = 0; i < _strategyTypes.Length; i++)
        {
            if (_strategyTypes[i] == currentType) { currentIndex = i + 1; break; }
        }

        EditorGUI.BeginChangeCheck();
        int chosen = EditorGUILayout.Popup(
            new GUIContent("Type", "Concrete IEnemyAttackStrategy that executes when this action fires."),
            currentIndex, _strategyNames);

        if (EditorGUI.EndChangeCheck())
        {
            stratProp.managedReferenceValue = chosen == 0
                ? null
                : Activator.CreateInstance(_strategyTypes[chosen - 1]);
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
            stratProp = serializedObject.FindProperty("attack")?.FindPropertyRelative("enemyAttack");
        }

        if (stratProp?.managedReferenceValue == null) return;

        EditorGUILayout.Space(4);

        // Draw all public instance fields of the concrete strategy type.
        var concreteType = stratProp.managedReferenceValue.GetType();
        EditorGUI.indentLevel++;
        foreach (var field in concreteType.GetFields(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            if (field.Name.StartsWith("<", StringComparison.Ordinal)) continue;
            var child = stratProp.FindPropertyRelative(field.Name);
            if (child != null)
                EditorGUILayout.PropertyField(child, true);
        }
        EditorGUI.indentLevel--;
    }

    // ── EnemyAttack fields ────────────────────────────────────────────────────

    /** <summary>Draws every field on <see cref="EnemyAttack"/> except <c>enemyAttack</c>,
     * which is drawn separately with the strategy picker.</summary>
     */
    private static void DrawAttackFieldsExceptStrategy(SerializedProperty attackProp)
    {
        var skip = new HashSet<string>(StringComparer.Ordinal) { "enemyAttack" };
        var iter = attackProp.Copy();
        var end  = attackProp.GetEndProperty();
        bool enterChildren = true;

        while (iter.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iter, end))
        {
            enterChildren = false;
            if (skip.Contains(iter.name)) continue;
            EditorGUILayout.PropertyField(iter, true);
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
        if (prop == null) return;
        var content = label != null
            ? new GUIContent(label, tooltip)
            : new GUIContent(prop.displayName, tooltip ?? prop.tooltip);
        EditorGUILayout.PropertyField(prop, content, children);
    }

    private static string FriendlyName(Type t)
    {
        string n = t.Name;
        if (n.EndsWith("EnemyAttackStrategy", StringComparison.Ordinal))
            n = n[..^"EnemyAttackStrategy".Length];
        else if (n.EndsWith("Strategy", StringComparison.Ordinal))
            n = n[..^"Strategy".Length];

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < n.Length; i++)
        {
            if (i > 0 && char.IsUpper(n[i])) sb.Append(' ');
            sb.Append(n[i]);
        }
        return sb.ToString().Trim();
    }
}

