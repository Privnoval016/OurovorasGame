using UnityEditor;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector.Editor;

namespace Quests.Editor
{
    /// <summary>
    /// Custom inspector for QuestObjective using Odin Inspector
    /// </summary>
    [CustomEditor(typeof(QuestObjective))]
    public class QuestObjectiveEditor : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            QuestObjective objective = (QuestObjective)target;

            // Don't draw this inspector for QuestOutcome (it has its own)
            if (objective is QuestOutcome)
            {
                base.OnInspectorGUI();
                return;
            }

            // Header
            EditorGUILayout.Space();
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
            headerStyle.normal.textColor = new Color(0.6f, 0.9f, 1f);
            EditorGUILayout.LabelField("Quest Objective", headerStyle);

            EditorGUILayout.Space();

            // Open Graph Editor button
            if (GUILayout.Button("Open in Quest Graph Editor", GUILayout.Height(30)))
            {
                QuestGraphWindow.ShowWindow();
            }

            EditorGUILayout.Space();

            // Draw all properties using Odin
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            // Show dependent outcomes
            DrawDependentOutcomes(objective);

            EditorGUILayout.Space();

            // Runtime info
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Runtime Information", EditorStyles.boldLabel);
                var instance = QuestManager.Instance.GetOrCreateInstance(objective);
                EditorGUILayout.LabelField($"Conditions Met Count: {instance.conditionsMetCount}");
                EditorGUILayout.LabelField($"Total Completions: {instance.totalCompletions}");
                EditorGUILayout.LabelField($"Is Complete: {instance.ConditionsMet()}");
            }
        }
    
    private void DrawDependentOutcomes(QuestObjective objective)
    {
        EditorGUILayout.LabelField("Dependent Quest Outcomes", EditorStyles.boldLabel);
        
        // Find all QuestOutcomes that depend on this objective
        string[] guids = AssetDatabase.FindAssets("t:QuestOutcome");
        var dependentOutcomes = new System.Collections.Generic.List<QuestOutcome>();
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            QuestOutcome outcome = AssetDatabase.LoadAssetAtPath<QuestOutcome>(path);
            
            if (outcome != null && outcome.questCondition != null)
            {
                if (ConditionDependsOnObjective(outcome.questCondition, objective))
                {
                    dependentOutcomes.Add(outcome);
                }
            }
        }
        
        if (dependentOutcomes.Count > 0)
        {
            EditorGUILayout.HelpBox($"This objective is required by {dependentOutcomes.Count} quest outcome(s):", MessageType.Info);
            
            foreach (var outcome in dependentOutcomes)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.ObjectField(outcome, typeof(QuestOutcome), false);
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = outcome;
                    EditorGUIUtility.PingObject(outcome);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No quest outcomes depend on this objective yet.", MessageType.None);
        }
    }
    
    private bool ConditionDependsOnObjective(Extensions.CustomMath.LogicComposition.ICondition<QuestOutcome> condition, QuestObjective objective)
    {
        if (condition == null) return false;
        
        switch (condition)
        {
            case ObjectiveCondition objCond:
                return objCond.objectiveKey == objective;
                
            case Extensions.CustomMath.LogicComposition.AndCondition<QuestOutcome> andCond:
                return andCond.children.Any(child => ConditionDependsOnObjective(child, objective));
                
            case Extensions.CustomMath.LogicComposition.OrCondition<QuestOutcome> orCond:
                return orCond.children.Any(child => ConditionDependsOnObjective(child, objective));
                
            case Extensions.CustomMath.LogicComposition.NotCondition<QuestOutcome> notCond:
                return ConditionDependsOnObjective(notCond.child, objective);
                
            default:
                return false;
        }
    }
}
}

