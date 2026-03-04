using UnityEditor;
using UnityEngine;

/** <summary>
 * Custom inspector for <see cref="EnemyMovementAIAction"/>.
 * Draws the serialised strategy reference with a friendly dropdown for
 * picking the concrete type, followed by all strategy parameters inline.
 * </summary>
 */
[CustomEditor(typeof(EnemyMovementAIAction))]
public class EnemyMovementAIActionEditor : Editor
{
    private static readonly GUIContent _strategyLabel =
        new GUIContent("Movement Type", "Select the kind of motion this action performs.");

    // All concrete IMovementStrategy implementations, discovered at construction time.
    private static readonly System.Type[] _strategyTypes;

    static EnemyMovementAIActionEditor()
    {
        var baseType = typeof(IMovementStrategy);
        var found = new System.Collections.Generic.List<System.Type>();
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var t in asm.GetTypes())
            {
                if (!t.IsAbstract && !t.IsInterface && baseType.IsAssignableFrom(t))
                    found.Add(t);
            }
        }
        _strategyTypes = found.ToArray();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Standard script field.
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromScriptableObject((ScriptableObject)target),
            typeof(MonoScript), false);
        GUI.enabled = true;

        EditorGUILayout.Space(4);

        // ── Base fields (AggroedAction, consideration) ──────────────────────
        DrawPropertiesExcluding(serializedObject, "m_Script", "strategy");
        EditorGUILayout.Space(4);

        // ── Strategy dropdown ────────────────────────────────────────────────
        var stratProp = serializedObject.FindProperty("strategy");
        DrawStrategyField(stratProp);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawStrategyField(SerializedProperty stratProp)
    {
        EditorGUILayout.LabelField("Movement Strategy", EditorStyles.boldLabel);

        // Determine current type index.
        System.Type currentType = stratProp.managedReferenceValue?.GetType();
        int currentIndex = -1;
        var typeNames = new string[_strategyTypes.Length + 1];
        typeNames[0] = "— None —";
        for (int i = 0; i < _strategyTypes.Length; i++)
        {
            typeNames[i + 1] = FriendlyName(_strategyTypes[i]);
            if (_strategyTypes[i] == currentType) currentIndex = i + 1;
        }
        if (currentIndex < 0) currentIndex = 0;

        EditorGUI.BeginChangeCheck();
        int chosen = EditorGUILayout.Popup(_strategyLabel, currentIndex, typeNames);
        if (EditorGUI.EndChangeCheck())
        {
            if (chosen == 0)
                stratProp.managedReferenceValue = null;
            else
                stratProp.managedReferenceValue =
                    System.Activator.CreateInstance(_strategyTypes[chosen - 1]);
        }

        // Draw the strategy's own serialised fields inline.
        if (stratProp.managedReferenceValue != null)
        {
            EditorGUI.indentLevel++;
            SerializedProperty child = stratProp.Copy();
            SerializedProperty end = stratProp.GetEndProperty();
            bool enter = true;
            while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end))
            {
                enter = false;
                EditorGUILayout.PropertyField(child, true);
            }
            EditorGUI.indentLevel--;
        }
    }

    /** <summary>Converts a strategy class name like "StrafeStrategy" to "Strafe".</summary> */
    private static string FriendlyName(System.Type t)
    {
        string n = t.Name;
        if (n.EndsWith("Strategy")) n = n[..^"Strategy".Length];
        // Add spaces before capitals: "BackJump" → "Back Jump".
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < n.Length; i++)
        {
            if (i > 0 && char.IsUpper(n[i])) sb.Append(' ');
            sb.Append(n[i]);
        }
        return sb.ToString();
    }
}

