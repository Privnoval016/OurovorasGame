using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Extensions.CustomMath.LogicComposition;

namespace Quests.Editor
{
    /// <summary>
    /// Main editor window for visualizing and editing the quest system as a node graph.
    /// Shows all quest objectives, outcomes, and their dependencies in a visual hierarchy.
    /// </summary>
    public class QuestGraphWindow : EditorWindow
    {
        private List<QuestNode> nodes = new List<QuestNode>();
        private List<QuestConnection> connections = new List<QuestConnection>();
    
    private Vector2 offset;
    private Vector2 drag;
    
    private QuestNode selectedNode;
    private QuestNode connectingFrom;
    
    // Node position persistence
    private Dictionary<string, Vector2> savedNodePositions = new Dictionary<string, Vector2>();
    private const string PositionSaveKey = "QuestGraphNodePositions";
    
    private float zoom = 1f;
    private const float MinZoom = 0.5f;
    private const float MaxZoom = 2f;
    
    private Vector2 rightPanelScroll;
    private const float RightPanelWidth = 350f;
    
    private GUIStyle nodeStyle;
    private GUIStyle selectedNodeStyle;
    private GUIStyle objectiveNodeStyle;
    private GUIStyle outcomeNodeStyle;
    private GUIStyle conditionNodeStyle;
    
    [MenuItem("Tools/Quest System/Quest Graph Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<QuestGraphWindow>("Quest Graph");
        window.minSize = new Vector2(800, 600);
    }
    
    private void OnEnable()
    {
        InitializeStyles();
        LoadSavedPositions();
        LoadAllQuests();
    }
    
    private void OnDisable()
    {
        SaveNodePositions();
    }
    
    private void InitializeStyles()
    {
        nodeStyle = new GUIStyle();
        nodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
        nodeStyle.border = new RectOffset(12, 12, 12, 12);
        nodeStyle.padding = new RectOffset(10, 10, 10, 10);
        nodeStyle.alignment = TextAnchor.UpperLeft;
        nodeStyle.normal.textColor = Color.white;
        
        selectedNodeStyle = new GUIStyle(nodeStyle);
        selectedNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1 on.png") as Texture2D;
        
        objectiveNodeStyle = new GUIStyle(nodeStyle);
        objectiveNodeStyle.normal.textColor = new Color(0.6f, 0.9f, 1f);
        
        outcomeNodeStyle = new GUIStyle(nodeStyle);
        outcomeNodeStyle.normal.textColor = new Color(0.6f, 1f, 0.6f);
        
        conditionNodeStyle = new GUIStyle(nodeStyle);
        conditionNodeStyle.normal.textColor = new Color(1f, 0.9f, 0.5f);
    }
    
    private void LoadAllQuests()
    {
        nodes.Clear();
        connections.Clear();
        
        // Find all QuestObjective assets (includes QuestOutcome since it inherits from QuestObjective)
        string[] guids = AssetDatabase.FindAssets("t:QuestObjective");
        
        Dictionary<QuestObjective, QuestNode> nodeMap = new Dictionary<QuestObjective, QuestNode>();
        
        // Create nodes for all objectives
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            QuestObjective objective = AssetDatabase.LoadAssetAtPath<QuestObjective>(path);
            
            if (objective != null)
            {
                QuestNode node = new QuestNode(objective);
                
                // Restore saved position if available
                string assetPath = AssetDatabase.GetAssetPath(objective);
                if (savedNodePositions.TryGetValue(assetPath, out Vector2 savedPos))
                {
                    node.rect.position = savedPos;
                }
                
                nodes.Add(node);
                nodeMap[objective] = node;
            }
        }
        
        // Only auto-layout if no saved positions exist
        if (savedNodePositions.Count == 0)
        {
            AutoLayoutNodes();
        }
        
        // Create connections based on quest conditions
        foreach (var node in nodes)
        {
            if (node.objective is QuestOutcome outcome && outcome.questCondition != null)
            {
                ExtractConditionDependencies(outcome.questCondition, node, nodeMap);
            }
        }
    }
    
    private void AutoLayoutNodes()
    {
        if (nodes.Count == 0) return;
        
        // Build dependency graph
        var nodeToLayer = new Dictionary<QuestNode, int>();
        var nodeDependencies = new Dictionary<QuestNode, List<QuestNode>>();
        
        foreach (var node in nodes)
        {
            nodeDependencies[node] = new List<QuestNode>();
        }
        
        // Map dependencies from connections
        foreach (var conn in connections)
        {
            if (!nodeDependencies[conn.to].Contains(conn.from))
            {
                nodeDependencies[conn.to].Add(conn.from);
            }
        }
        
        // Assign layers using topological sort
        var visited = new HashSet<QuestNode>();
        var temp = new HashSet<QuestNode>();
        
        void AssignLayer(QuestNode node)
        {
            if (visited.Contains(node)) return;
            if (temp.Contains(node)) return; // Cycle detection
            
            temp.Add(node);
            
            int maxDependencyLayer = -1;
            foreach (var dep in nodeDependencies[node])
            {
                AssignLayer(dep);
                if (nodeToLayer.ContainsKey(dep))
                {
                    maxDependencyLayer = Mathf.Max(maxDependencyLayer, nodeToLayer[dep]);
                }
            }
            
            nodeToLayer[node] = maxDependencyLayer + 1;
            temp.Remove(node);
            visited.Add(node);
        }
        
        foreach (var node in nodes)
        {
            AssignLayer(node);
        }
        
        // Group nodes by layer
        var layers = new Dictionary<int, List<QuestNode>>();
        foreach (var kvp in nodeToLayer)
        {
            if (!layers.ContainsKey(kvp.Value))
            {
                layers[kvp.Value] = new List<QuestNode>();
            }
            layers[kvp.Value].Add(kvp.Key);
        }
        
        // Position nodes
        float layerSpacing = 400f;
        float nodeSpacing = 120f;
        float startX = 100f;
        float startY = 100f;
        
        var sortedLayers = layers.Keys.OrderBy(k => k).ToList();
        
        foreach (var layerIndex in sortedLayers)
        {
            var layerNodes = layers[layerIndex];
            float layerHeight = (layerNodes.Count - 1) * nodeSpacing;
            float currentY = startY - (layerHeight / 2f);
            
            for (int i = 0; i < layerNodes.Count; i++)
            {
                float x = startX + (layerIndex * layerSpacing);
                float y = currentY + (i * nodeSpacing);
                layerNodes[i].rect.position = new Vector2(x, y);
            }
        }
    }
    
    private void ExtractConditionDependencies(ICondition<QuestOutcome> condition, QuestNode targetNode, Dictionary<QuestObjective, QuestNode> nodeMap)
    {
        if (condition == null) return;
        
        switch (condition)
        {
            case ObjectiveCondition objCond:
                if (objCond.objectiveKey != null && nodeMap.TryGetValue(objCond.objectiveKey, out var sourceNode))
                {
                    connections.Add(new QuestConnection(sourceNode, targetNode, "Objective"));
                }
                break;
                
            case OutcomeCondition outCond:
                if (outCond.outcomeKey != null && nodeMap.TryGetValue(outCond.outcomeKey, out var outcomeNode))
                {
                    connections.Add(new QuestConnection(outcomeNode, targetNode, $"Outcome ({outCond.requiredCompletions}x)"));
                }
                break;
                
            case AndCondition<QuestOutcome> andCond:
                foreach (var child in andCond.children)
                {
                    ExtractConditionDependencies(child, targetNode, nodeMap);
                }
                break;
                
            case OrCondition<QuestOutcome> orCond:
                foreach (var child in orCond.children)
                {
                    ExtractConditionDependencies(child, targetNode, nodeMap);
                }
                break;
                
            case NotCondition<QuestOutcome> notCond:
                if (notCond.child != null)
                {
                    ExtractConditionDependencies(notCond.child, targetNode, nodeMap);
                }
                break;
        }
    }
    
    private void OnGUI()
    {
        DrawGrid(20, 0.2f, Color.gray);
        DrawGrid(100, 0.4f, Color.gray);
        
        DrawConnections();
        DrawNodes();
        DrawRightPanel();
        
        ProcessNodeEvents(Event.current);
        ProcessEvents(Event.current);
        
        if (GUI.changed) Repaint();
    }
    
    private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
    {
        int widthDivs = Mathf.CeilToInt((position.width - RightPanelWidth) / gridSpacing);
        int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);
        
        Handles.BeginGUI();
        Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);
        
        Vector3 newOffset = new Vector3(offset.x % gridSpacing, offset.y % gridSpacing, 0);
        
        for (int i = 0; i < widthDivs; i++)
        {
            Handles.DrawLine(
                new Vector3(gridSpacing * i, -gridSpacing, 0) + newOffset,
                new Vector3(gridSpacing * i, position.height, 0f) + newOffset
            );
        }
        
        for (int j = 0; j < heightDivs; j++)
        {
            Handles.DrawLine(
                new Vector3(-gridSpacing, gridSpacing * j, 0) + newOffset,
                new Vector3(position.width - RightPanelWidth, gridSpacing * j, 0f) + newOffset
            );
        }
        
        Handles.color = Color.white;
        Handles.EndGUI();
    }
    
    private void DrawConnections()
    {
        if (connections != null)
        {
            foreach (var connection in connections)
            {
                DrawConnection(connection);
            }
        }
        
        // Draw temporary connection while connecting
        if (connectingFrom != null)
        {
            Handles.BeginGUI();
            Handles.color = Color.yellow;
            Vector2 start = connectingFrom.rect.center + offset;
            Vector2 end = Event.current.mousePosition;
            DrawBezier(start, end);
            Handles.EndGUI();
        }
    }
    
    private void DrawConnection(QuestConnection connection)
    {
        Handles.BeginGUI();
        
        Vector2 start = connection.from.rect.center + offset;
        Vector2 end = connection.to.rect.center + offset;
        
        // Color based on connection type
        if (connection.label.Contains("AND"))
            Handles.color = Color.cyan;
        else if (connection.label.Contains("OR"))
            Handles.color = Color.yellow;
        else if (connection.label.Contains("NOT"))
            Handles.color = Color.red;
        else if (connection.label.Contains("Outcome"))
            Handles.color = Color.green;
        else
            Handles.color = Color.white;
        
        DrawBezier(start, end);
        
        // Draw label in the middle
        Vector2 labelPos = (start + end) / 2f;
        GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
        labelStyle.normal.textColor = Handles.color;
        labelStyle.fontSize = 9;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        
        Vector2 labelSize = labelStyle.CalcSize(new GUIContent(connection.label));
        Rect labelRect = new Rect(labelPos.x - labelSize.x / 2f, labelPos.y - labelSize.y / 2f, labelSize.x, labelSize.y);
        
        // Background for label
        EditorGUI.DrawRect(labelRect, new Color(0.1f, 0.1f, 0.1f, 0.8f));
        GUI.Label(labelRect, connection.label, labelStyle);
        
        Handles.EndGUI();
    }
    
    private void DrawBezier(Vector2 start, Vector2 end)
    {
        Vector2 startTangent = start + Vector2.right * 50f;
        Vector2 endTangent = end - Vector2.right * 50f;
        
        Handles.DrawBezier(start, end, startTangent, endTangent, Handles.color, null, 3f);
        
        // Draw arrow at end
        Vector2 direction = (end - endTangent).normalized;
        Vector2 arrowTip = end;
        Vector2 arrowLeft = arrowTip - direction * 10f + new Vector2(-direction.y, direction.x) * 5f;
        Vector2 arrowRight = arrowTip - direction * 10f - new Vector2(-direction.y, direction.x) * 5f;
        
        Handles.DrawLine(arrowTip, arrowLeft);
        Handles.DrawLine(arrowTip, arrowRight);
    }
    
    private void DrawNodes()
    {
        if (nodes != null)
        {
            foreach (QuestNode node in nodes)
            {
                node.Draw(offset, selectedNode == node, objectiveNodeStyle, outcomeNodeStyle);
            }
        }
    }
    
    private void DrawRightPanel()
    {
        Rect panelRect = new Rect(position.width - RightPanelWidth, 0, RightPanelWidth, position.height);
        GUILayout.BeginArea(panelRect);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("Quest System Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Reload All Quests", GUILayout.Height(30)))
        {
            LoadAllQuests();
        }
        
        if (GUILayout.Button("Auto Layout", GUILayout.Height(25)))
        {
            AutoLayoutNodes();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Total Objectives: {nodes.Count(n => !(n.objective is QuestOutcome))}");
        EditorGUILayout.LabelField($"Total Outcomes: {nodes.Count(n => n.objective is QuestOutcome)}");
        EditorGUILayout.LabelField($"Total Connections: {connections.Count}");
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("• Left Click: Select node\n• Right Click: Create new quest\n• Middle Mouse: Pan view\n• Double Click: Select in inspector\n• Click 'Edit in Inspector' to modify\n• Positions are saved automatically", MessageType.Info);
        
        if (selectedNode != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selected Quest", EditorStyles.boldLabel);
            
            rightPanelScroll = EditorGUILayout.BeginScrollView(rightPanelScroll);
            DrawSelectedNodeDetails();
            EditorGUILayout.EndScrollView();
        }
        
        EditorGUILayout.EndVertical();
        GUILayout.EndArea();
    }
    
    private void DrawSelectedNodeDetails()
    {
        if (selectedNode == null || selectedNode.objective == null) return;
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // Header with asset name
        EditorGUILayout.LabelField(selectedNode.objective.name, EditorStyles.whiteLargeLabel);
        
        EditorGUILayout.Space();
        
        // Type (read-only)
        string type = selectedNode.objective is QuestOutcome ? "Quest Outcome" : "Quest Objective";
        EditorGUILayout.LabelField("Type:", type);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Description:", EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(selectedNode.objective.description, EditorStyles.wordWrappedLabel, GUILayout.MinHeight(40));
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Max Completions: {selectedNode.objective.maxCompletions}");
        EditorGUILayout.LabelField($"Max Execution Queue: {selectedNode.objective.maxExecutionQueueLength}");
        
        // Outcome-specific info
        if (selectedNode.objective is QuestOutcome outcome)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Outcome Settings", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Evaluation Mode: {outcome.evaluationMode}");
            
            EditorGUILayout.Space();
            if (outcome.questCondition != null)
            {
                EditorGUILayout.LabelField("Has Quest Condition", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.HelpBox("No condition set", MessageType.Warning);
            }
        }
        
        EditorGUILayout.Space();
        
        // Big edit button
        if (GUILayout.Button("Edit in Inspector", GUILayout.Height(35)))
        {
            Selection.activeObject = selectedNode.objective;
            EditorGUIUtility.PingObject(selectedNode.objective);
        }
        
        EditorGUILayout.Space();
        
        // Action buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Focus", GUILayout.Height(25)))
        {
            FocusOnNode(selectedNode);
        }
        
        if (GUILayout.Button("Delete", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Delete Quest", 
                $"Are you sure you want to delete '{selectedNode.objective.name}'?", 
                "Delete", "Cancel"))
            {
                string path = AssetDatabase.GetAssetPath(selectedNode.objective);
                nodes.Remove(selectedNode);
                selectedNode = null;
                AssetDatabase.DeleteAsset(path);
                LoadAllQuests();
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawConditionHierarchy(ICondition<QuestOutcome> condition, int depth)
    {
        if (condition == null) return;
        
        EditorGUI.indentLevel = depth;
        
        switch (condition)
        {
            case ObjectiveCondition objCond:
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField("✓", GUILayout.Width(20));
                EditorGUILayout.LabelField(objCond.objectiveKey != null ? objCond.objectiveKey.name : "[NULL]", EditorStyles.label);
                if (objCond.objectiveKey != null && GUILayout.Button("→", GUILayout.Width(30)))
                {
                    var node = nodes.FirstOrDefault(n => n.objective == objCond.objectiveKey);
                    if (node != null)
                    {
                        selectedNode = node;
                        FocusOnNode(node);
                    }
                }
                EditorGUILayout.EndHorizontal();
                break;
                
            case OutcomeCondition outCond:
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField("⚑", GUILayout.Width(20));
                string label = outCond.outcomeKey != null ? $"{outCond.outcomeKey.name} ({outCond.requiredCompletions}x)" : "[NULL]";
                EditorGUILayout.LabelField(label, EditorStyles.label);
                if (outCond.outcomeKey != null && GUILayout.Button("→", GUILayout.Width(30)))
                {
                    var node = nodes.FirstOrDefault(n => n.objective == outCond.outcomeKey);
                    if (node != null)
                    {
                        selectedNode = node;
                        FocusOnNode(node);
                    }
                }
                EditorGUILayout.EndHorizontal();
                break;
                
            case AndCondition<QuestOutcome> andCond:
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("AND (All must be true)", EditorStyles.boldLabel);
                foreach (var child in andCond.children)
                {
                    DrawConditionHierarchy(child, depth + 1);
                }
                EditorGUILayout.EndVertical();
                break;
                
            case OrCondition<QuestOutcome> orCond:
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("OR (Any must be true)", EditorStyles.boldLabel);
                foreach (var child in orCond.children)
                {
                    DrawConditionHierarchy(child, depth + 1);
                }
                EditorGUILayout.EndVertical();
                break;
                
            case NotCondition<QuestOutcome> notCond:
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("NOT (Inverse)", EditorStyles.boldLabel);
                DrawConditionHierarchy(notCond.child, depth + 1);
                EditorGUILayout.EndVertical();
                break;
                
            default:
                EditorGUILayout.LabelField($"[{condition.GetType().Name}]", EditorStyles.label);
                break;
        }
        
        EditorGUI.indentLevel = 0;
    }
    
    private void FocusOnNode(QuestNode node)
    {
        if (node == null) return;
        
        // Center the view on the node
        Vector2 windowCenter = new Vector2((position.width - RightPanelWidth) / 2f, position.height / 2f);
        offset = windowCenter - node.rect.center;
        Repaint();
    }
    
    private void SaveNodePositions()
    {
        if (nodes == null) return;
        
        savedNodePositions.Clear();
        foreach (var node in nodes)
        {
            if (node.objective != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(node.objective);
                savedNodePositions[assetPath] = node.rect.position;
            }
        }
        
        // Save to EditorPrefs as JSON
        string json = EditorJsonUtility.ToJson(new NodePositionData { positions = savedNodePositions });
        EditorPrefs.SetString(PositionSaveKey, json);
    }
    
    private void LoadSavedPositions()
    {
        savedNodePositions.Clear();
        
        if (EditorPrefs.HasKey(PositionSaveKey))
        {
            string json = EditorPrefs.GetString(PositionSaveKey);
            try
            {
                NodePositionData data = new NodePositionData();
                EditorJsonUtility.FromJsonOverwrite(json, data);
                savedNodePositions = data.positions ?? new Dictionary<string, Vector2>();
            }
            catch
            {
                savedNodePositions = new Dictionary<string, Vector2>();
            }
        }
    }
    
    private void ProcessNodeEvents(Event e)
    {
        if (nodes != null)
        {
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                bool guiChanged = nodes[i].ProcessEvents(e, offset);
                
                if (guiChanged)
                {
                    if (e.button == 0) // Left click
                    {
                        selectedNode = nodes[i];
                    }
                    GUI.changed = true;
                }
            }
        }
    }
    
    private void ProcessEvents(Event e)
    {
        drag = Vector2.zero;
        
        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 1) // Right click
                {
                    ProcessContextMenu(e.mousePosition);
                }
                else if (e.button == 2) // Middle mouse button
                {
                    // Start panning
                }
                break;
                
            case EventType.MouseDrag:
                if (e.button == 2) // Middle mouse drag
                {
                    OnDrag(e.delta);
                }
                break;
                
            case EventType.ScrollWheel:
                // Could add zoom functionality here
                break;
        }
    }
    
    private void OnDrag(Vector2 delta)
    {
        drag = delta;
        
        if (nodes != null)
        {
            foreach (QuestNode node in nodes)
            {
                node.Drag(delta);
            }
        }
        
        offset += delta;
        GUI.changed = true;
    }
    
    private void ProcessContextMenu(Vector2 mousePosition)
    {
        GenericMenu genericMenu = new GenericMenu();
        genericMenu.AddItem(new GUIContent("Reload Quests"), false, () => LoadAllQuests());
        genericMenu.AddItem(new GUIContent("Auto Layout"), false, () => AutoLayoutNodes());
        genericMenu.AddSeparator("");
        genericMenu.AddItem(new GUIContent("Create Quest Objective"), false, () => CreateNewQuestObjective(mousePosition));
        genericMenu.AddItem(new GUIContent("Create Quest Outcome"), false, () => CreateNewQuestOutcome(mousePosition));
        genericMenu.ShowAsContext();
    }
    
    private void CreateNewQuestObjective(Vector2 mousePos)
    {
        CreateQuestAssetDialog.ShowDialog(false, (name, path) =>
        {
            QuestObjective newObj = ScriptableObject.CreateInstance<QuestObjective>();
            newObj.description = "New quest objective";
            
            string fullPath = System.IO.Path.Combine(path, name + ".asset");
            AssetDatabase.CreateAsset(newObj, fullPath);
            AssetDatabase.SaveAssets();
            
            // Add to graph
            QuestNode node = new QuestNode(newObj);
            Vector2 graphPos = mousePos - offset;
            node.rect.position = graphPos;
            nodes.Add(node);
            selectedNode = node;
            
            Repaint();
        });
    }
    
    private void CreateNewQuestOutcome(Vector2 mousePos)
    {
        CreateQuestAssetDialog.ShowDialog(true, (name, path) =>
        {
            QuestOutcome newOutcome = ScriptableObject.CreateInstance<QuestOutcome>();
            newOutcome.description = "New quest outcome";
            
            string fullPath = System.IO.Path.Combine(path, name + ".asset");
            AssetDatabase.CreateAsset(newOutcome, fullPath);
            AssetDatabase.SaveAssets();
            
            // Add to graph
            QuestNode node = new QuestNode(newOutcome);
            Vector2 graphPos = mousePos - offset;
            node.rect.position = graphPos;
            nodes.Add(node);
            selectedNode = node;
            
            Repaint();
        });
    }
    }

/// <summary>
/// Represents a visual node in the quest graph
/// </summary>
public class QuestNode
{
    public Rect rect;
    public QuestObjective objective;
    public string title;
    
    private bool isDragged;
    private const float NodeWidth = 220f;
    private const float NodeHeight = 90f;
    
    public QuestNode(QuestObjective objective)
    {
        this.objective = objective;
        this.title = objective != null ? objective.name : "Unnamed";
        this.rect = new Rect(0, 0, NodeWidth, NodeHeight);
    }
    
    public void Drag(Vector2 delta)
    {
        if (isDragged)
        {
            rect.position += delta;
        }
    }
    
    public void Draw(Vector2 offset, bool isSelected, GUIStyle objectiveStyle, GUIStyle outcomeStyle)
    {
        Rect offsetRect = new Rect(rect.position + offset, rect.size);
        bool isOutcome = objective is QuestOutcome;
        
        // Background color based on type
        Color bgColor = isOutcome 
            ? new Color(0.2f, 0.35f, 0.25f, 1f)  // Dark green for outcomes
            : new Color(0.25f, 0.3f, 0.4f, 1f);   // Dark blue for objectives
        
        // Draw background
        EditorGUI.DrawRect(offsetRect, bgColor);
        
        // Draw border
        Color borderColor = isOutcome 
            ? new Color(0.4f, 0.8f, 0.5f, 1f)  // Bright green
            : new Color(0.5f, 0.7f, 1f, 1f);   // Bright blue
            
        if (isSelected)
        {
            borderColor = new Color(1f, 0.75f, 0f, 1f); // Orange for selection
            DrawThickBorder(offsetRect, borderColor, 3f);
        }
        else
        {
            DrawThickBorder(offsetRect, borderColor, 2f);
        }
        
        // Icon area on the left
        float iconSize = 20f;
        float padding = 8f;
        Rect iconRect = new Rect(offsetRect.x + padding, offsetRect.y + padding, iconSize, iconSize);
        
        // Draw icon background
        Color iconBgColor = isOutcome 
            ? new Color(0.3f, 0.6f, 0.4f, 1f) 
            : new Color(0.4f, 0.5f, 0.7f, 1f);
        EditorGUI.DrawRect(iconRect, iconBgColor);
        
        // Draw icon text
        GUIStyle iconStyle = new GUIStyle(EditorStyles.boldLabel);
        iconStyle.alignment = TextAnchor.MiddleCenter;
        iconStyle.normal.textColor = Color.white;
        iconStyle.fontSize = 10;
        GUI.Label(iconRect, isOutcome ? "OUT" : "OBJ", iconStyle);
        
        // Content area (next to icon)
        float contentX = offsetRect.x + padding + iconSize + padding;
        float contentY = offsetRect.y + padding;
        float contentWidth = offsetRect.width - (padding * 3 + iconSize);
        float contentHeight = offsetRect.height - (padding * 2);
        Rect contentRect = new Rect(contentX, contentY, contentWidth, contentHeight);
        
        GUILayout.BeginArea(contentRect);
        GUILayout.BeginVertical();
        
        // Title
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.normal.textColor = Color.white;
        titleStyle.fontSize = 11;
        titleStyle.wordWrap = true;
        titleStyle.clipping = TextClipping.Clip;
        GUILayout.Label(title, titleStyle);
        
        GUILayout.Space(2);
        
        // Type label
        GUIStyle typeStyle = new GUIStyle(EditorStyles.miniLabel);
        typeStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        typeStyle.fontSize = 9;
        string typeLabel = isOutcome ? "Quest Outcome" : "Quest Objective";
        GUILayout.Label(typeLabel, typeStyle);
        
        // Description preview
        if (!string.IsNullOrEmpty(objective.description))
        {
            GUILayout.Space(2);
            GUIStyle descStyle = new GUIStyle(EditorStyles.miniLabel);
            descStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            descStyle.fontSize = 8;
            descStyle.wordWrap = true;
            descStyle.clipping = TextClipping.Clip;
            
            string shortDesc = objective.description.Length > 45 
                ? objective.description.Substring(0, 42) + "..." 
                : objective.description;
            GUILayout.Label(shortDesc, descStyle);
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    
    private void DrawThickBorder(Rect rect, Color color, float thickness)
    {
        // Top
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        // Bottom
        EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), color);
        // Left
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        // Right
        EditorGUI.DrawRect(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), color);
    }
    
    public bool ProcessEvents(Event e, Vector2 offset)
    {
        Rect offsetRect = new Rect(rect.position + offset, rect.size);
        
        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0) // Left click
                {
                    if (offsetRect.Contains(e.mousePosition))
                    {
                        isDragged = true;
                        GUI.changed = true;
                        
                        // Check for double click
                        if (e.clickCount == 2)
                        {
                            Selection.activeObject = objective;
                            EditorGUIUtility.PingObject(objective);
                        }
                        
                        return true;
                    }
                }
                break;
                
            case EventType.MouseUp:
                isDragged = false;
                break;
                
            case EventType.MouseDrag:
                if (e.button == 0 && isDragged)
                {
                    Drag(e.delta);
                    e.Use();
                    return true;
                }
                break;
        }
        
        return false;
    }
}

/// <summary>
/// Represents a connection between two quest nodes
/// </summary>
public class QuestConnection
{
    public QuestNode from;
    public QuestNode to;
    public string label;
    
    public QuestConnection(QuestNode from, QuestNode to, string label = "")
    {
        this.from = from;
        this.to = to;
        this.label = label;
    }
}

/// <summary>
/// Serializable data structure for saving node positions
/// </summary>
[System.Serializable]
public class NodePositionData
{
    public Dictionary<string, Vector2> positions = new Dictionary<string, Vector2>();
}
}
