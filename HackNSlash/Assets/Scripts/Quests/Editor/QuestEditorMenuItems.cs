using UnityEditor;
using UnityEngine;

namespace Quests.Editor
{
    /// <summary>
    /// Helper menu items for quick access to quest editor functionality
    /// </summary>
    public static class QuestEditorMenuItems
    {
        [MenuItem("Assets/Quest System/Open in Graph Editor", true)]
        private static bool ValidateOpenInGraphEditor()
        {
            return Selection.activeObject is QuestObjective;
        }
        
        [MenuItem("Assets/Quest System/Open in Graph Editor", false, 20)]
        private static void OpenInGraphEditor()
        {
            if (Selection.activeObject is QuestObjective)
            {
                QuestGraphWindow.ShowWindow();
            }
        }
        
        [MenuItem("Assets/Quest System/View Dependencies", true)]
        private static bool ValidateViewDependencies()
        {
            return Selection.activeObject is QuestObjective;
        }
        
        [MenuItem("Assets/Quest System/View Dependencies", false, 21)]
        private static void ViewDependencies()
        {
            if (!(Selection.activeObject is QuestObjective objective)) return;
            
            // Find all quest outcomes that depend on this objective
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
                string message = $"'{objective.name}' is required by {dependentOutcomes.Count} quest outcome(s):\n\n";
                foreach (var outcome in dependentOutcomes)
                {
                    message += $"• {outcome.name}\n";
                }
                EditorUtility.DisplayDialog("Quest Dependencies", message, "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Quest Dependencies", 
                    $"No quest outcomes depend on '{objective.name}' yet.", "OK");
            }
        }
        
        private static bool ConditionDependsOnObjective(Extensions.CustomMath.LogicComposition.ICondition<QuestOutcome> condition, QuestObjective objective)
        {
            if (condition == null) return false;
            
            switch (condition)
            {
                case ObjectiveCondition objCond:
                    return objCond.objectiveKey == objective;
                    
                case Extensions.CustomMath.LogicComposition.AndCondition<QuestOutcome> andCond:
                    return System.Linq.Enumerable.Any(andCond.children, child => ConditionDependsOnObjective(child, objective));
                    
                case Extensions.CustomMath.LogicComposition.OrCondition<QuestOutcome> orCond:
                    return System.Linq.Enumerable.Any(orCond.children, child => ConditionDependsOnObjective(child, objective));
                    
                case Extensions.CustomMath.LogicComposition.NotCondition<QuestOutcome> notCond:
                    return ConditionDependsOnObjective(notCond.child, objective);
                    
                default:
                    return false;
            }
        }
        
        [MenuItem("GameObject/Quest System/Add Quest Event Broadcaster", false, 10)]
        private static void CreateQuestEventBroadcaster()
        {
            GameObject go = new GameObject("QuestEventBroadcaster");
            go.AddComponent<QuestEventBroadcaster>();
            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Create Quest Event Broadcaster");
        }
        
        [MenuItem("GameObject/Quest System/Add Quest Event Receiver", false, 11)]
        private static void CreateQuestEventReceiver()
        {
            GameObject go = new GameObject("QuestEventReceiver");
            go.AddComponent<QuestEventReceiver>();
            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Create Quest Event Receiver");
        }
    }
}

