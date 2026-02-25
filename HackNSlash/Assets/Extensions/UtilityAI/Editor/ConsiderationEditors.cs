#if UNITY_EDITOR
using Extensions.UtilityAI.ConsiderationBases;
using UnityEditor;
using UnityEngine;

namespace Extensions.UtilityAI.Editor
{
    // ─────────────────────────────────────────────────────────────────
    //  CurveConsideration
    // ─────────────────────────────────────────────────────────────────

    /** <summary>Custom inspector for CurveConsideration. Shows the curve preview prominently and the context key with a readable label.</summary> */
    [CustomEditor(typeof(CurveConsideration))]
    public class CurveConsiderationEditor : UnityEditor.Editor
    {
        private const float CurveHeight = 80f;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var c = (CurveConsideration)target;

            EditorGUILayout.LabelField("Curve Consideration", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            var keyProp = serializedObject.FindProperty("contextKey");
            EditorGUILayout.PropertyField(keyProp,
                new GUIContent("Context Key", "Which float value from the context is fed into the curve."));

            var typeName = keyProp.FindPropertyRelative("enumTypeName").stringValue;
            if (string.IsNullOrEmpty(typeName))
                EditorGUILayout.HelpBox("⚠  Context Key is not set. This consideration will always return 0 at runtime.", MessageType.Warning);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Response Curve", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("X = normalised input from context key (0–1).  Y = output utility score (0–1).", MessageType.None);

            var curveProp = serializedObject.FindProperty("curve");
            EditorGUILayout.PropertyField(curveProp, new GUIContent("Curve"), GUILayout.Height(CurveHeight));

            // Live preview bar if we have a valid curve
            if (c.curve != null)
            {
                EditorGUILayout.Space(2);
                DrawCurvePreviewBar(c.curve);
            }

            serializedObject.ApplyModifiedProperties();
        }

        // Draws a small horizontal preview strip showing curve output across the [0,1] input range.
        private static void DrawCurvePreviewBar(AnimationCurve curve)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(12));
            EditorGUI.DrawRect(r, new Color(0.15f, 0.15f, 0.15f));

            int steps = Mathf.RoundToInt(r.width);
            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)(steps - 1);
                float v = Mathf.Clamp01(curve.Evaluate(t));
                Color c = Color.Lerp(new Color(0.7f, 0.2f, 0.1f), new Color(0.2f, 0.85f, 0.3f), v);
                EditorGUI.DrawRect(new Rect(r.x + i, r.yMax - r.height * v, 1, r.height * v), c);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────
    //  InRangeConsideration
    // ─────────────────────────────────────────────────────────────────

    /** <summary>Custom inspector for InRangeConsideration. Shows a visualised range ring and the distance-to-score curve.</summary> */
    [CustomEditor(typeof(InRangeConsideration))]
    public class InRangeConsiderationEditor : UnityEditor.Editor
    {
        private const float CurveHeight = 80f;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("In-Range Consideration", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            var keyProp = serializedObject.FindProperty("targetKey");
            EditorGUILayout.PropertyField(keyProp,
                new GUIContent("Target Key", "Which sensor target to query for distance."));

            var typeName = keyProp.FindPropertyRelative("enumTypeName").stringValue;
            if (string.IsNullOrEmpty(typeName))
                EditorGUILayout.HelpBox("⚠  Target Key is not set. This consideration will always return 0 at runtime.", MessageType.Warning);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Range Settings", EditorStyles.boldLabel);

            var maxDistProp = serializedObject.FindProperty("maxDistance");
            var maxAngProp = serializedObject.FindProperty("maxAngle");
            EditorGUILayout.PropertyField(maxDistProp, new GUIContent("Max Distance"));
            EditorGUILayout.PropertyField(maxAngProp, new GUIContent("Max Angle (degrees)", "Full cone angle centred on forward. 360 = omnidirectional."));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Distance → Score Curve", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("X = normalised distance (0 = at agent, 1 = at max distance).  Y = utility score.", MessageType.None);

            var curveProp = serializedObject.FindProperty("curve");
            EditorGUILayout.PropertyField(curveProp, new GUIContent("Curve"), GUILayout.Height(CurveHeight));

            var c = (InRangeConsideration)target;
            if (c.curve != null)
            {
                EditorGUILayout.Space(2);
                DrawCurvePreview(c.curve);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawCurvePreview(AnimationCurve curve)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(12));
            EditorGUI.DrawRect(r, new Color(0.15f, 0.15f, 0.15f));
            int steps = Mathf.RoundToInt(r.width);
            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)(steps - 1);
                float v = Mathf.Clamp01(curve.Evaluate(t));
                Color col = Color.Lerp(new Color(0.7f, 0.2f, 0.1f), new Color(0.2f, 0.85f, 0.3f), v);
                EditorGUI.DrawRect(new Rect(r.x + i, r.yMax - r.height * v, 1, r.height * v), col);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────
    //  ConstantConsideration
    // ─────────────────────────────────────────────────────────────────

    /** <summary>Custom inspector for ConstantConsideration. Shows the value as a coloured slider so its meaning is immediately clear.</summary> */
    [CustomEditor(typeof(ConstantConsideration))]
    public class ConstantConsiderationEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Constant Consideration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Always returns the fixed value below. Useful as a base priority to bias an action.", MessageType.None);
            EditorGUILayout.Space(2);

            var valueProp = serializedObject.FindProperty("value");
            valueProp.floatValue = EditorGUILayout.Slider(new GUIContent("Value", "The fixed utility score [0–1] returned every evaluation."), valueProp.floatValue, 0f, 1f);

            // Colour-coded bar
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(10));
            EditorGUI.DrawRect(r, new Color(0.15f, 0.15f, 0.15f));
            float filled = r.width * Mathf.Clamp01(valueProp.floatValue);
            Color col = Color.Lerp(new Color(0.7f, 0.2f, 0.1f), new Color(0.2f, 0.85f, 0.3f), valueProp.floatValue);
            EditorGUI.DrawRect(new Rect(r.x, r.y, filled, r.height), col);

            serializedObject.ApplyModifiedProperties();
        }
    }

    // ─────────────────────────────────────────────────────────────────
    //  RandomConsideration
    // ─────────────────────────────────────────────────────────────────

    /** <summary>Custom inspector for RandomConsideration. Shows the min/max range as a labelled gradient strip.</summary> */
    [CustomEditor(typeof(RandomConsideration))]
    public class RandomConsiderationEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Random Consideration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Returns a random score inside [min, max] each evaluation tick.", MessageType.None);
            EditorGUILayout.Space(2);

            var mmProp = serializedObject.FindProperty("minMax");
            Vector2 mm = mmProp.vector2Value;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Range", GUILayout.Width(80));
            mm.x = EditorGUILayout.FloatField(mm.x, GUILayout.Width(50));
            EditorGUILayout.MinMaxSlider(ref mm.x, ref mm.y, 0f, 1f);
            mm.y = EditorGUILayout.FloatField(mm.y, GUILayout.Width(50));
            mm.x = Mathf.Clamp01(mm.x);
            mm.y = Mathf.Clamp01(mm.y);
            if (mm.x > mm.y) mm.x = mm.y;
            mmProp.vector2Value = mm;
            EditorGUILayout.EndHorizontal();

            // Visual bar showing the range
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(10));
            EditorGUI.DrawRect(r, new Color(0.15f, 0.15f, 0.15f));
            float startX = r.x + r.width * mm.x;
            float endX = r.x + r.width * mm.y;
            EditorGUI.DrawRect(new Rect(startX, r.y, endX - startX, r.height), new Color(0.3f, 0.6f, 1f));

            serializedObject.ApplyModifiedProperties();
        }
    }

    // ─────────────────────────────────────────────────────────────────
    //  CompositeConsideration
    // ─────────────────────────────────────────────────────────────────

    /** <summary>Custom inspector for CompositeConsideration. Shows a clear visual chain of child considerations and their operations.</summary> */
    [CustomEditor(typeof(CompositeConsideration))]
    public class CompositeConsiderationEditor : UnityEditor.Editor
    {
        private bool _childrenFoldout = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Composite Consideration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Combines multiple considerations into one score.\n" +
                "Evaluation starts with the First consideration, then applies each operation in order.",
                MessageType.None);
            EditorGUILayout.Space(2);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("allMustBeNonZero"),
                new GUIContent("All Must Be Non-Zero", "If true, any child scoring 0 immediately returns 0 (short-circuit AND)."));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Consideration Chain", EditorStyles.boldLabel);

            // First consideration
            DrawStepBox("First", null, serializedObject.FindProperty("firstConsideration"));

            // Additional operations
            var listProp = serializedObject.FindProperty("considerations");
            _childrenFoldout = EditorGUILayout.Foldout(_childrenFoldout, $"Additional Operations ({listProp.arraySize})", true);
            if (_childrenFoldout)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < listProp.arraySize; i++)
                {
                    var elem = listProp.GetArrayElementAtIndex(i);
                    var opProp = elem.FindPropertyRelative("operation");
                    var conProp = elem.FindPropertyRelative("consideration");

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Step {i + 1}", GUILayout.Width(48));
                    EditorGUILayout.PropertyField(opProp, GUIContent.none, GUILayout.Width(90));
                    EditorGUILayout.PropertyField(conProp, GUIContent.none);
                    if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                    {
                        listProp.DeleteArrayElementAtIndex(i);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;

                if (GUILayout.Button("+ Add Operation", GUILayout.Height(22)))
                {
                    listProp.InsertArrayElementAtIndex(listProp.arraySize);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawStepBox(string stepLabel, SerializedProperty opProp, SerializedProperty conProp)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(stepLabel, GUILayout.Width(48));
            if (opProp != null)
                EditorGUILayout.PropertyField(opProp, GUIContent.none, GUILayout.Width(90));
            else
                GUILayout.Space(94);
            EditorGUILayout.PropertyField(conProp, GUIContent.none);
            GUILayout.Space(26); // align with delete button column above
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif



