using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Custom editor window for visually editing the skill tree layout.
/// Allows placing nodes, connecting them, and editing properties.
/// </summary>
public class SkillTreeEditorWindow : EditorWindow
{
    private PlayerSkillTree skillTree;
    private Vector2 scrollPosition;
    private Vector2 canvasOffset = Vector2.zero;
    private float zoom = 1f;
    
    private SkillTreeNode selectedNode;
    private SkillTreeNode connectingFrom;
    private bool isDraggingNode = false;
    
    private const float NODE_SIZE = 60f;
    private const float CANVAS_SIZE = 2000f;
    
    [Header("Grid Settings")]
    [SerializeField] private bool enableGridSnap = false;
    [SerializeField] private float gridSnapX = 0.05f;
    [SerializeField] private float gridSnapY = 0.05f;
    
    [MenuItem("Tools/Skill Tree Editor")]
    public static void OpenWindow()
    {
        SkillTreeEditorWindow window = GetWindow<SkillTreeEditorWindow>("Skill Tree Editor");
        window.minSize = new Vector2(800, 600);
        window.Show();
    }
    
    private void OnGUI()
    {
        DrawToolbar();
        
        if (skillTree == null)
        {
            EditorGUILayout.HelpBox("Select a PlayerSkillTree asset to edit", MessageType.Info);
            return;
        }
        
        DrawCanvas();
        DrawInspector();
        
        // Repaint on mouse movement for connection line
        if (connectingFrom != null)
        {
            Repaint();
        }
    }
    
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        PlayerSkillTree newTree = (PlayerSkillTree)EditorGUILayout.ObjectField(
            skillTree, typeof(PlayerSkillTree), false, GUILayout.Width(200));
        
        if (newTree != skillTree)
        {
            skillTree = newTree;
            selectedNode = null;
            connectingFrom = null;
        }
        
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("Add Node", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            AddNewNode();
        }
        
        if (GUILayout.Button("Center View", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            canvasOffset = Vector2.zero;
        }
        
        GUILayout.Space(10);
        
        enableGridSnap = GUILayout.Toggle(enableGridSnap, "Snap", EditorStyles.toolbarButton, GUILayout.Width(50));
        if (enableGridSnap)
        {
            GUILayout.Label("X:", GUILayout.Width(15));
            gridSnapX = EditorGUILayout.FloatField(gridSnapX, GUILayout.Width(40));
            GUILayout.Label("Y:", GUILayout.Width(15));
            gridSnapY = EditorGUILayout.FloatField(gridSnapY, GUILayout.Width(40));
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawCanvas()
    {
        Rect canvasRect = new Rect(0, EditorGUILayout.GetControlRect().y, position.width * 0.7f, position.height - 20);
        
        // Background
        EditorGUI.DrawRect(canvasRect, new Color(0.2f, 0.2f, 0.2f));
        
        // Handle canvas panning
        HandleCanvasPanning(canvasRect);
        
        // Begin scroll view
        GUI.BeginGroup(canvasRect);
        
        // Draw connections first (behind nodes)
        DrawConnections();
        
        // Draw nodes
        DrawNodes(canvasRect);
        
        // Draw connection line if connecting
        if (connectingFrom != null)
        {
            Vector2 fromPos = CanvasToScreenPos(connectingFrom.uiPosition, canvasRect);
            Vector2 toPos = Event.current.mousePosition;
            Handles.color = Color.yellow;
            Handles.DrawAAPolyLine(3f, fromPos, toPos);
        }
        
        GUI.EndGroup();
    }
    
    private void HandleCanvasPanning(Rect canvasRect)
    {
        Event e = Event.current;
        
        if (e.type == EventType.MouseDrag && e.button == 2) // Middle mouse button
        {
            canvasOffset += e.delta;
            e.Use();
            Repaint();
        }
        
        if (e.type == EventType.ScrollWheel)
        {
            zoom = Mathf.Clamp(zoom - e.delta.y * 0.01f, 0.5f, 2f);
            e.Use();
            Repaint();
        }
    }
    
    private void DrawConnections()
    {
        if (skillTree == null) return;
        
        foreach (var node in skillTree.nodes)
        {
            foreach (string parentId in node.parentNodeIds)
            {
                var parent = skillTree.GetNodeById(parentId);
                if (parent != null)
                {
                    Vector2 fromPos = CanvasToScreenPos(parent.uiPosition, new Rect(0, 0, position.width * 0.7f, position.height - 20));
                    Vector2 toPos = CanvasToScreenPos(node.uiPosition, new Rect(0, 0, position.width * 0.7f, position.height - 20));
                    
                    Handles.color = new Color(0.5f, 0.7f, 1f, 0.8f);
                    Handles.DrawAAPolyLine(2f, fromPos, toPos);
                }
            }
        }
    }
    
    private void DrawNodes(Rect canvasRect)
    {
        if (skillTree == null) return;
        
        Event e = Event.current;
        
        foreach (var node in skillTree.nodes)
        {
            Vector2 screenPos = CanvasToScreenPos(node.uiPosition, canvasRect);
            Rect nodeRect = new Rect(screenPos.x - NODE_SIZE / 2, screenPos.y - NODE_SIZE / 2, NODE_SIZE, NODE_SIZE);
            
            // Skip if outside visible area
            if (!nodeRect.Overlaps(new Rect(0, 0, canvasRect.width, canvasRect.height)))
                continue;
            
            // Node background
            Color nodeColor = selectedNode == node ? new Color(1f, 0.8f, 0.3f) : 
                             connectingFrom == node ? new Color(1f, 1f, 0.3f) :
                             new Color(0.4f, 0.6f, 0.8f);
            EditorGUI.DrawRect(nodeRect, nodeColor);
            
            // Node border
            Handles.color = Color.black;
            Handles.DrawSolidRectangleWithOutline(nodeRect, Color.clear, Color.black);
            
            // Node label
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.fontSize = 10;
            labelStyle.normal.textColor = Color.white;
            GUI.Label(nodeRect, string.IsNullOrEmpty(node.nodeName) ? node.nodeId : node.nodeName, labelStyle);
            
            // Handle node interaction
            if (e.type == EventType.MouseDown && nodeRect.Contains(e.mousePosition))
            {
                if (e.button == 0) // Left click - select and start potential drag
                {
                    selectedNode = node;
                    isDraggingNode = true;
                    e.Use();
                    Repaint();
                }
                else if (e.button == 1) // Right click - context menu
                {
                    ShowNodeContextMenu(node);
                    e.Use();
                }
            }
            
            // Handle node dragging - only if we initiated drag on this node
            if (e.type == EventType.MouseDrag && e.button == 0 && isDraggingNode && selectedNode == node)
            {
                Vector2 canvasPos = ScreenToCanvasPos(e.mousePosition, canvasRect);
                
                // Apply grid snapping if enabled
                if (enableGridSnap)
                {
                    canvasPos.x = Mathf.Round(canvasPos.x / gridSnapX) * gridSnapX;
                    canvasPos.y = Mathf.Round(canvasPos.y / gridSnapY) * gridSnapY;
                }
                
                node.uiPosition = new Vector2(
                    Mathf.Clamp(canvasPos.x, 0f, 1f),
                    Mathf.Clamp(canvasPos.y, 0f, 1f)
                );
                EditorUtility.SetDirty(skillTree);
                e.Use();
                Repaint();
            }
            
            // Stop dragging on mouse up
            if (e.type == EventType.MouseUp && e.button == 0)
            {
                isDraggingNode = false;
            }
        }
    }
    
    private void ShowNodeContextMenu(SkillTreeNode node)
    {
        GenericMenu menu = new GenericMenu();
        
        if (connectingFrom == null)
        {
            menu.AddItem(new GUIContent("Connect From This Node"), false, () => { connectingFrom = node; });
        }
        else if (connectingFrom == node)
        {
            menu.AddItem(new GUIContent("Cancel Connection"), false, () => { connectingFrom = null; });
        }
        else
        {
            menu.AddItem(new GUIContent("Connect To This Node"), false, () => ConnectNodes(connectingFrom, node));
        }
        
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Set as Start Node"), false, () => SetStartNode(node));
        menu.AddItem(new GUIContent("Delete Node"), false, () => DeleteNode(node));
        
        menu.ShowAsContext();
    }
    
    private void ConnectNodes(SkillTreeNode parent, SkillTreeNode child)
    {
        if (!child.parentNodeIds.Contains(parent.nodeId))
        {
            child.parentNodeIds.Add(parent.nodeId);
            EditorUtility.SetDirty(skillTree);
        }
        connectingFrom = null;
    }
    
    private void SetStartNode(SkillTreeNode node)
    {
        skillTree.startNodeId = node.nodeId;
        EditorUtility.SetDirty(skillTree);
    }
    
    private void DeleteNode(SkillTreeNode node)
    {
        if (EditorUtility.DisplayDialog("Delete Node", $"Delete node '{node.nodeName}'?", "Delete", "Cancel"))
        {
            // Remove from all parent references
            foreach (var n in skillTree.nodes)
            {
                n.parentNodeIds.Remove(node.nodeId);
            }
            
            skillTree.nodes.Remove(node);
            
            if (selectedNode == node)
                selectedNode = null;
            
            EditorUtility.SetDirty(skillTree);
        }
    }
    
    private void AddNewNode()
    {
        if (skillTree == null) return;
        
        SkillTreeNode newNode = new SkillTreeNode
        {
            nodeId = System.Guid.NewGuid().ToString(),
            nodeName = "New Node",
            uiPosition = new Vector2(0.5f, 0.5f)
        };
        
        skillTree.nodes.Add(newNode);
        selectedNode = newNode;
        
        EditorUtility.SetDirty(skillTree);
    }
    
    private void DrawInspector()
    {
        Rect inspectorRect = new Rect(position.width * 0.7f, 20, position.width * 0.3f, position.height - 20);
        
        GUILayout.BeginArea(inspectorRect);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        if (selectedNode != null)
        {
            EditorGUILayout.LabelField("Node Inspector", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            selectedNode.nodeId = EditorGUILayout.TextField("Node ID", selectedNode.nodeId);
            selectedNode.nodeName = EditorGUILayout.TextField("Name", selectedNode.nodeName);
            selectedNode.description = EditorGUILayout.TextArea(selectedNode.description, GUILayout.Height(60));
            
            EditorGUILayout.Space();
            selectedNode.icon = (Sprite)EditorGUILayout.ObjectField("Icon", selectedNode.icon, typeof(Sprite), false);
            selectedNode.demonstrationVideo = (UnityEngine.Video.VideoClip)EditorGUILayout.ObjectField(
                "Video", selectedNode.demonstrationVideo, typeof(UnityEngine.Video.VideoClip), false);
            
            EditorGUILayout.Space();
            selectedNode.cost = EditorGUILayout.IntField("Cost", selectedNode.cost);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("UI Position", $"({selectedNode.uiPosition.x:F3}, {selectedNode.uiPosition.y:F3})");
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Parent Connections:", EditorStyles.boldLabel);
            for (int i = selectedNode.parentNodeIds.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(selectedNode.parentNodeIds[i], GUILayout.Width(150));
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    selectedNode.parentNodeIds.RemoveAt(i);
                    EditorUtility.SetDirty(skillTree);
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorUtility.SetDirty(skillTree);
        }
        else
        {
            EditorGUILayout.LabelField("No node selected", EditorStyles.centeredGreyMiniLabel);
        }
        
        EditorGUILayout.EndVertical();
        GUILayout.EndArea();
    }
    
    private Vector2 CanvasToScreenPos(Vector2 canvasPos, Rect canvasRect)
    {
        return new Vector2(
            canvasPos.x * canvasRect.width * zoom + canvasOffset.x,
            canvasPos.y * canvasRect.height * zoom + canvasOffset.y
        );
    }
    
    private Vector2 ScreenToCanvasPos(Vector2 screenPos, Rect canvasRect)
    {
        return new Vector2(
            (screenPos.x - canvasOffset.x) / (canvasRect.width * zoom),
            (screenPos.y - canvasOffset.y) / (canvasRect.height * zoom)
        );
    }
}

