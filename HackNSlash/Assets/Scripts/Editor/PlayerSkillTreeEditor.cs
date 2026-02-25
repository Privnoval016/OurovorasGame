using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom inspector for PlayerSkillTree that allows inline editing of nodes
/// and quick access to the Skill Tree Editor window.
/// </summary>
[CustomEditor(typeof(PlayerSkillTree))]
public class PlayerSkillTreeEditor : Editor
{
    private SerializedProperty treeNameProp;
    private SerializedProperty startNodeIdProp;
    private SerializedProperty nodesProp;
    
    private bool showNodes = true;
    
    private void OnEnable()
    {
        treeNameProp = serializedObject.FindProperty("treeName");
        startNodeIdProp = serializedObject.FindProperty("startNodeId");
        nodesProp = serializedObject.FindProperty("nodes");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Skill Tree Configuration", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.PropertyField(treeNameProp);
        EditorGUILayout.PropertyField(startNodeIdProp);
        
        EditorGUILayout.Space();
        
        // Button to open Skill Tree Editor
        if (GUILayout.Button("Open Skill Tree Editor", GUILayout.Height(30)))
        {
            SkillTreeEditorWindow.OpenWindow();
        }
        
        EditorGUILayout.Space();
        
        // Nodes list
        showNodes = EditorGUILayout.BeginFoldoutHeaderGroup(showNodes, $"Nodes ({nodesProp.arraySize})");
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        if (showNodes)
        {
            EditorGUI.indentLevel++;
            
            // Add/Remove buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Node"))
            {
                nodesProp.arraySize++;
                SerializedProperty newNode = nodesProp.GetArrayElementAtIndex(nodesProp.arraySize - 1);
                
                // Initialize new node
                newNode.FindPropertyRelative("nodeId").stringValue = System.Guid.NewGuid().ToString();
                newNode.FindPropertyRelative("nodeName").stringValue = "New Node";
                newNode.FindPropertyRelative("cost").intValue = 1;
                newNode.FindPropertyRelative("uiPosition").vector2Value = new Vector2(0.5f, 0.5f);
            }
            
            if (nodesProp.arraySize > 0 && GUILayout.Button("Remove Last"))
            {
                nodesProp.arraySize--;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Draw each node
            for (int i = 0; i < nodesProp.arraySize; i++)
            {
                SerializedProperty nodeProp = nodesProp.GetArrayElementAtIndex(i);
                DrawNodeElement(nodeProp, i);
            }
            
            EditorGUI.indentLevel--;
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawNodeElement(SerializedProperty nodeProp, int index)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        SerializedProperty nodeIdProp = nodeProp.FindPropertyRelative("nodeId");
        SerializedProperty nodeNameProp = nodeProp.FindPropertyRelative("nodeName");
        SerializedProperty descriptionProp = nodeProp.FindPropertyRelative("description");
        SerializedProperty iconProp = nodeProp.FindPropertyRelative("icon");
        SerializedProperty videoProp = nodeProp.FindPropertyRelative("demonstrationVideo");
        SerializedProperty costProp = nodeProp.FindPropertyRelative("cost");
        SerializedProperty parentIdsProp = nodeProp.FindPropertyRelative("parentNodeIds");
        SerializedProperty uiPositionProp = nodeProp.FindPropertyRelative("uiPosition");
        SerializedProperty attacksProp = nodeProp.FindPropertyRelative("unlockedAttacks");
        SerializedProperty displayIndexProp = nodeProp.FindPropertyRelative("displayedAttackIndex");
        
        // Header with node name
        string headerName = string.IsNullOrEmpty(nodeNameProp.stringValue) ? 
            $"Node {index}" : nodeNameProp.stringValue;
        
        EditorGUILayout.LabelField(headerName, EditorStyles.boldLabel);
        
        // Basic info
        EditorGUILayout.PropertyField(nodeIdProp, new GUIContent("ID"));
        EditorGUILayout.PropertyField(nodeNameProp, new GUIContent("Name"));
        EditorGUILayout.PropertyField(descriptionProp, new GUIContent("Description"), GUILayout.Height(40));
        
        // Visual assets
        EditorGUILayout.PropertyField(iconProp, new GUIContent("Icon"));
        EditorGUILayout.PropertyField(videoProp, new GUIContent("Demo Video"));
        
        // Cost
        EditorGUILayout.PropertyField(costProp, new GUIContent("Unlock Cost"));
        
        // UI Position (read-only, set via editor window)
        GUI.enabled = false;
        EditorGUILayout.PropertyField(uiPositionProp, new GUIContent("UI Position"));
        GUI.enabled = true;
        
        // Parent connections
        EditorGUILayout.PropertyField(parentIdsProp, new GUIContent("Parent Node IDs"), true);
        
        // Unlockable attacks
        EditorGUILayout.PropertyField(attacksProp, new GUIContent("Unlocked Attacks"), true);
        
        if (attacksProp.arraySize > 0)
        {
            displayIndexProp.intValue = EditorGUILayout.IntSlider(
                "Displayed Attack Index", 
                displayIndexProp.intValue, 
                0, 
                attacksProp.arraySize - 1
            );
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }
}

