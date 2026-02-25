#if UNITY_EDITOR
using Extensions.UtilityAI.ConsiderationBases;
using UnityEditor;
using UnityEngine;

namespace Extensions.UtilityAI.Editor
{
    /** <summary>
     * Custom inspector for AIAction ScriptableObjects.
     * Shows the action key, an inline consideration reference, and — in play mode — a live utility score bar
     * pulled from the brain that currently owns this action.
     * </summary> */
    [CustomEditor(typeof(AIAction<>), true)]  // 'true' = also apply to all derived types
    public class AIActionEditor : UnityEditor.Editor
    {
        private static readonly Color ColBarBg = new Color(0.15f, 0.15f, 0.15f);
        private static readonly Color ColBar = new Color(0.3f, 0.6f, 1f);
        private static readonly Color ColBarHigh = new Color(0.2f, 0.85f, 0.3f);

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var actionSO = target as ScriptableObject;
            if (actionSO == null) { DrawDefaultInspector(); return; }

            EditorGUILayout.LabelField($"AI Action  —  {target.GetType().Name}", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            // Draw all serialized fields except 'consideration' which we handle below
            SerializedProperty prop = serializedObject.GetIterator();
            prop.NextVisible(true); // skip m_Script
            while (prop.NextVisible(false))
            {
                if (prop.name == "consideration") continue;
                EditorGUILayout.PropertyField(prop, true);
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Consideration", EditorStyles.boldLabel);

            var conProp = serializedObject.FindProperty("consideration");
            EditorGUILayout.PropertyField(conProp, new GUIContent("Asset",
                "The Consideration ScriptableObject that scores this action each brain tick."));

            // If a consideration is assigned, show a short summary of its type
            if (conProp.objectReferenceValue != null)
            {
                var con = conProp.objectReferenceValue as Consideration;
                if (con != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent($"Type: {con.GetType().Name}"), EditorStyles.miniLabel);
                    if (GUILayout.Button("Inspect", EditorStyles.miniButton, GUILayout.Width(56)))
                        Selection.activeObject = con;
                    if (GUILayout.Button("Brain Debugger", EditorStyles.miniButton, GUILayout.Width(100)))
                        EditorApplication.ExecuteMenuItem("Window/UtilityAI/Brain Debugger");
                    EditorGUILayout.EndHorizontal();
                }
            }

            // Live score bar when in play mode
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Live Utility Score", EditorStyles.boldLabel);
                DrawLiveScoreBar();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawLiveScoreBar()
        {
            // Try to find a brain in the scene that owns this action, then pull its score.
            float score = -1f;
            string brainName = null;

            foreach (var acc in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (acc is not IAIBrainAccessor accessor) continue;
                foreach (var info in accessor.GetActionDebugInfos())
                {
                    if (info.ActionName == target.name)
                    {
                        score = info.LastUtility;
                        brainName = acc.name;
                        if (info.IsChosen) break; // prefer the row where it's winning
                    }
                }
                if (score >= 0) break;
            }

            if (score < 0)
            {
                EditorGUILayout.LabelField("No live brain found with this action.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            EditorGUILayout.LabelField($"Brain: {brainName}", EditorStyles.miniLabel);
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(16));
            EditorGUI.DrawRect(r, ColBarBg);
            float filled = r.width * Mathf.Clamp01(score);
            EditorGUI.DrawRect(new Rect(r.x, r.y, filled, r.height), score > 0.5f ? ColBarHigh : ColBar);
            EditorGUI.LabelField(r, $"  {score:F4}", EditorStyles.miniLabel);
        }
    }
}
#endif

