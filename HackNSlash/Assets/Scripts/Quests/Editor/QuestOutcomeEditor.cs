using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector.Editor;
using Extensions.CustomMath.LogicComposition;

namespace Quests.Editor
{
    /// <summary>
    /// Custom inspector for QuestOutcome using Odin Inspector for proper SerializeReference support
    /// </summary>
    [CustomEditor(typeof(QuestOutcome))]
    public class QuestOutcomeEditor : OdinEditor
    {
        private bool showConditionVisualization = true;
        
        public override void OnInspectorGUI()
        {
            QuestOutcome outcome = (QuestOutcome)target;

            // Header
            EditorGUILayout.Space();
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
            headerStyle.normal.textColor = new Color(0.6f, 1f, 0.6f);
            EditorGUILayout.LabelField("Quest Outcome", headerStyle);

            EditorGUILayout.Space();

            // Open Graph Editor button
            if (GUILayout.Button("Open in Quest Graph Editor", GUILayout.Height(30)))
            {
                QuestGraphWindow.ShowWindow();
            }

            EditorGUILayout.Space();

            // Draw all properties using Odin's system (this properly handles SerializeReference)
            base.OnInspectorGUI();

            // Add visual condition hierarchy display
            if (outcome.questCondition != null)
            {
                EditorGUILayout.Space();
                showConditionVisualization = EditorGUILayout.BeginFoldoutHeaderGroup(showConditionVisualization, "Condition Visualization");
                
                if (showConditionVisualization)
                {
                    EditorGUILayout.HelpBox("Visual representation of the condition logic:", MessageType.Info);
                    DrawConditionVisualization(outcome.questCondition, 0);
                }
                
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            EditorGUILayout.Space();

            // Runtime info
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Runtime Information", EditorStyles.boldLabel);
                var instance = QuestManager.Instance.GetOrCreateInstance(outcome);
                EditorGUILayout.LabelField($"Conditions Met Count: {instance.conditionsMetCount}");
                EditorGUILayout.LabelField($"Total Completions: {instance.totalCompletions}");
                EditorGUILayout.LabelField($"Is Complete: {instance.ConditionsMet()}");

                if (GUILayout.Button("Test Evaluate Condition"))
                {
                    bool result = outcome.questCondition?.Evaluate(outcome) ?? false;
                    Debug.Log($"Condition evaluated to: {result}");
                }
            }
        }
        
        private void DrawConditionVisualization(ICondition<QuestOutcome> condition, int depth)
        {
            if (condition == null) return;

            Color originalBg = GUI.backgroundColor;
            EditorGUI.indentLevel = depth;

            switch (condition)
            {
                case ObjectiveCondition objCond:
                    GUI.backgroundColor = new Color(0.6f, 0.9f, 1f);
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    GUILayout.Space(depth * 15);
                    EditorGUILayout.LabelField("[OBJ]", GUILayout.Width(50));
                    EditorGUILayout.ObjectField(objCond.objectiveKey, typeof(QuestObjective), false);
                    EditorGUILayout.EndHorizontal();
                    GUI.backgroundColor = originalBg;
                    break;

                case OutcomeCondition outCond:
                    GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    GUILayout.Space(depth * 15);
                    EditorGUILayout.LabelField("[OUT]", GUILayout.Width(50));
                    EditorGUILayout.ObjectField(outCond.outcomeKey, typeof(QuestOutcome), false);
                    EditorGUILayout.LabelField($"({outCond.requiredCompletions}x)", GUILayout.Width(40));
                    EditorGUILayout.EndHorizontal();
                    GUI.backgroundColor = originalBg;
                    break;

                case AndCondition<QuestOutcome> andCond:
                    GUI.backgroundColor = new Color(1f, 0.8f, 0.4f);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(depth * 15);
                    EditorGUILayout.LabelField("AND - All conditions must be true:", EditorStyles.boldLabel);
                    foreach (var child in andCond.children)
                    {
                        DrawConditionVisualization(child, depth + 1);
                    }
                    EditorGUILayout.EndVertical();
                    GUI.backgroundColor = originalBg;
                    break;

                case OrCondition<QuestOutcome> orCond:
                    GUI.backgroundColor = new Color(1f, 0.9f, 0.4f);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(depth * 15);
                    EditorGUILayout.LabelField("OR - Any condition must be true:", EditorStyles.boldLabel);
                    foreach (var child in orCond.children)
                    {
                        DrawConditionVisualization(child, depth + 1);
                    }
                    EditorGUILayout.EndVertical();
                    GUI.backgroundColor = originalBg;
                    break;

                case NotCondition<QuestOutcome> notCond:
                    GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(depth * 15);
                    EditorGUILayout.LabelField("NOT - Inverse condition:", EditorStyles.boldLabel);
                    DrawConditionVisualization(notCond.child, depth + 1);
                    EditorGUILayout.EndVertical();
                    GUI.backgroundColor = originalBg;
                    break;

                default:
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    GUILayout.Space(depth * 15);
                    EditorGUILayout.LabelField($"[{condition.GetType().Name}]");
                    EditorGUILayout.EndHorizontal();
                    break;
            }

            EditorGUI.indentLevel = 0;
        }
    }
}

