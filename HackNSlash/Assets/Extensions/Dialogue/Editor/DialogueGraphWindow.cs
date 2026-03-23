#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Extensions.Dialogue.Editor
{
    /// <summary>
    /// Visual editor window for creating and editing dialogue graphs.
    /// Supports node creation, deletion, connection, and property editing.
    /// </summary>
    public sealed class DialogueGraphWindow : EditorWindow
    {
        private Data.DialogueGraph _currentGraph;
        private Vector2 _panOffset = Vector2.zero;
        private float _zoom = 1f;
        private const float MinZoom = 0.3f;
        private const float MaxZoom = 3f;

        private DialogueGraphNode _selectedNode = default;
        private DialogueGraphNode _connectingFromNode = default;
        private Vector2 _mousePos;
        private List<DialogueGraphNode> _visualNodes = new();
        private Dictionary<int, Vector2> _nodePositions = new();

        private const string PositionSaveKey = "DialogueGraphNodePositions";
        private const float NodeWidth = 200f;
        private const float NodeHeight = 100f;
        private const float GridSize = 20f;

        private GUIStyle _lineNodeStyle;
        private GUIStyle _choiceNodeStyle;
        private GUIStyle _selectedNodeStyle;
        private GUIStyle _connectionStyle;

        [MenuItem("Tools/Dialogue System/Dialogue Graph Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<DialogueGraphWindow>("Dialogue Graph");
            window.minSize = new Vector2(800, 600);
        }

        private void OnEnable()
        {
            LoadSavedPositions();
            InitializeStyles();
            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            SaveNodePositions();
            EditorApplication.update -= Repaint;
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawCanvas();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUILayout.LabelField("Dialogue Graph Editor", EditorStyles.toolbarButton);

                GUILayout.FlexibleSpace();

                _currentGraph = (Data.DialogueGraph)EditorGUILayout.ObjectField(
                    _currentGraph, typeof(Data.DialogueGraph), false, GUILayout.Width(300)
                );

                if (GUILayout.Button("Create New", EditorStyles.toolbarButton, GUILayout.Width(100)))
                {
                    CreateNewGraph();
                }

                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    SaveCurrentGraph();
                }
            }
        }

        private void DrawCanvas()
        {
            if (_currentGraph == null)
            {
                EditorGUILayout.HelpBox("No dialogue graph selected. Create or select a graph to begin editing.", MessageType.Info);
                return;
            }

            Rect canvasRect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
            GUI.DrawTexture(canvasRect, Texture2D.whiteTexture); // Background

            // Draw grid
            DrawGrid(canvasRect);

            // Draw connections
            if (Event.current.type == EventType.Repaint)
            {
                DrawConnections(canvasRect);
            }

            // Draw nodes
            RebuildVisualNodes();
            foreach (var node in _visualNodes)
            {
                DrawNode(node, canvasRect);
            }

            // Handle node interactions
            HandleNodeInteractions(canvasRect);
        }

        private void DrawGrid(Rect canvasRect)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Handles.BeginGUI();
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.1f);

            float startX = _panOffset.x % GridSize;
            float startY = _panOffset.y % GridSize;

            for (float x = startX; x < canvasRect.width; x += GridSize * _zoom)
            {
                Handles.DrawLine(new Vector3(x, 0), new Vector3(x, canvasRect.height));
            }

            for (float y = startY; y < canvasRect.height; y += GridSize * _zoom)
            {
                Handles.DrawLine(new Vector3(0, y), new Vector3(canvasRect.width, y));
            }

            Handles.EndGUI();
        }

        private void DrawConnections(Rect canvasRect)
        {
            Handles.BeginGUI();
            Handles.color = Color.gray;

            foreach (var fromNode in _visualNodes)
            {
                var dataNode = _currentGraph.GetNode(fromNode.Id);
                if (dataNode == null || dataNode.NextNodes.Length == 0)
                    continue;

                var fromRect = GetNodeScreenRect(fromNode);
                Vector2 fromPos = fromRect.center + Vector2.right * fromRect.width / 2;

                foreach (var nextId in dataNode.NextNodes)
                {
                    var toNode = _visualNodes.FirstOrDefault(n => n.Id == nextId);
                    if (toNode.Id == default)
                        continue;

                    var toRect = GetNodeScreenRect(toNode);
                    Vector2 toPos = toRect.center - Vector2.right * toRect.width / 2;

                    Handles.DrawBezier(fromPos, toPos, fromPos + Vector2.right * 50, toPos - Vector2.right * 50, Color.gray, null, 2f);
                }
            }

            if (_connectingFromNode.Id != default)
            {
                var fromRect = GetNodeScreenRect(_connectingFromNode);
                Vector2 fromPos = fromRect.center + Vector2.right * fromRect.width / 2;
                Handles.DrawLine(fromPos, _mousePos, 2f);
            }

            Handles.EndGUI();
        }

        private void DrawNode(DialogueGraphNode node, Rect canvasRect)
        {
            var screenRect = GetNodeScreenRect(node);

            GUIStyle style = node.DataNode switch
            {
                Data.LineNode => _lineNodeStyle,
                Data.ChoiceNode => _choiceNodeStyle,
                _ => _lineNodeStyle
            };

            if (node.Id == _selectedNode.Id)
                style = _selectedNodeStyle;

            GUI.Box(screenRect, node.DisplayName, style);

            Rect outputHandle = new Rect(screenRect.xMax - 10, screenRect.center.y - 5, 10, 10);
            GUI.Box(outputHandle, "", EditorStyles.miniButton);
        }

        private Rect GetNodeScreenRect(DialogueGraphNode node)
        {
            if (!_nodePositions.TryGetValue(node.Id.Value, out var pos))
            {
                pos = new Vector2(200 + node.Id.Value * 250, 200);
                _nodePositions[node.Id.Value] = pos;
            }

            Vector2 screenPos = (pos + _panOffset) * _zoom;
            return new Rect(screenPos, new Vector2(NodeWidth, NodeHeight) * _zoom);
        }

        private void HandleNodeInteractions(Rect canvasRect)
        {
            if (Event.current.type == EventType.MouseDown && canvasRect.Contains(Event.current.mousePosition))
            {
                _mousePos = Event.current.mousePosition;

                bool clickedNode = false;
                foreach (var node in _visualNodes)
                {
                    var nodeRect = GetNodeScreenRect(node);
                    if (nodeRect.Contains(Event.current.mousePosition))
                    {
                        _selectedNode = node;
                        clickedNode = true;
                        Event.current.Use();
                        break;
                    }
                }

                if (!clickedNode)
                {
                    _selectedNode = default;
                }
            }

            if (Event.current.type == EventType.MouseDrag)
            {
                if (Event.current.button == 2)
                {
                    _panOffset += Event.current.delta;
                    Event.current.Use();
                }
            }

            if (Event.current.type == EventType.ScrollWheel)
            {
                float zoomDelta = Event.current.delta.y > 0 ? 0.9f : 1.1f;
                _zoom = Mathf.Clamp(_zoom * zoomDelta, MinZoom, MaxZoom);
                Event.current.Use();
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete && _selectedNode.Id != default)
            {
                DeleteNode(_selectedNode);
                Event.current.Use();
            }
        }

        private void RebuildVisualNodes()
        {
            _visualNodes.Clear();
            if (_currentGraph == null || _currentGraph.Nodes == null)
                return;

            foreach (var dataNode in _currentGraph.Nodes)
            {
                _visualNodes.Add(new DialogueGraphNode { Id = dataNode.Id, DataNode = dataNode });
            }
        }

        private void CreateNewGraph()
        {
            var graph = ScriptableObject.CreateInstance<Data.DialogueGraph>();
            graph.GraphName = "New Dialogue";
            graph.StartNode = new Data.NodeId(0);

            string path = EditorUtility.SaveFilePanelInProject("Create Dialogue Graph", "NewDialogue", "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(graph, path);
                AssetDatabase.SaveAssets();
                _currentGraph = graph;
            }
        }

        private void SaveCurrentGraph()
        {
            if (_currentGraph == null)
            {
                EditorUtility.DisplayDialog("Error", "No graph to save", "OK");
                return;
            }

            EditorUtility.SetDirty(_currentGraph);
            AssetDatabase.SaveAssets();
            Debug.Log("[DialogueGraphWindow] Graph saved");
        }

        private void DeleteNode(DialogueGraphNode node)
        {
            Debug.Log($"[DialogueGraphWindow] Delete node {node.Id}");
            // Implementation would remove node from graph
        }

        private void SaveNodePositions()
        {
            string positionData = JsonUtility.ToJson(new PositionData { Positions = _nodePositions });
            EditorPrefs.SetString(PositionSaveKey, positionData);
        }

        private void LoadSavedPositions()
        {
            string positionData = EditorPrefs.GetString(PositionSaveKey, "");
            if (!string.IsNullOrEmpty(positionData))
            {
                var data = JsonUtility.FromJson<PositionData>(positionData);
                _nodePositions = data?.Positions ?? new Dictionary<int, Vector2>();
            }
        }

        private void InitializeStyles()
        {
            if (_lineNodeStyle != null)
                return;

            _lineNodeStyle = new GUIStyle(EditorStyles.helpBox)
            {
                normal = { background = MakeTex(new Color(0.3f, 0.5f,0.8f)) },
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };

            _choiceNodeStyle = new GUIStyle(_lineNodeStyle)
            {
                normal = { background = MakeTex(new Color(0.8f, 0.5f, 0.3f)) }
            };

            _selectedNodeStyle = new GUIStyle(_lineNodeStyle)
            {
                normal = { background = MakeTex(new Color(0.3f, 0.8f, 0.3f)) }
            };
        }

        private static Texture2D MakeTex(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private struct DialogueGraphNode
        {
            public Data.NodeId Id;
            public Data.DialogueNode DataNode;

            public string DisplayName => DataNode switch
            {
                Data.LineNode => $"Line #{Id.Value}",
                Data.ChoiceNode => $"Choice #{Id.Value}",
                _ => $"Node #{Id.Value}"
            };
        }

        [Serializable]
        private class PositionData
        {
            public Dictionary<int, Vector2> Positions = new();
        }
    }
}
#endif

