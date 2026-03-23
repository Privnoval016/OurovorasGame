#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Extensions.Dialogue.Editor
{
    public sealed class DialogueGraphWindow : EditorWindow
    {
        private Data.DialogueGraph _currentGraph;
        private Vector2 _panOffset = Vector2.zero;
        private float _zoom = 1f;
        private Dictionary<int, Vector2> _nodePositions = new();
        private int _selectedNodeId = -1;
        private int _connectingFromNodeId = -1;
        private Vector2 _propertiesScrollPosition;

        private GUIStyle _lineNodeStyle;
        private GUIStyle _choiceNodeStyle;
        private GUIStyle _selectedNodeStyle;
        private bool _stylesInitialized;

        private const float NodeWidth = 150f;
        private const float NodeHeight = 80f;
        private const float GridSize = 20f;
        private const float MinZoom = 0.3f;
        private const float MaxZoom = 3f;
        private const float PropertiesPanelWidth = 350f;

        [MenuItem("Tools/Dialogue System/Open Graph Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<DialogueGraphWindow>("Dialogue Graph Editor");
            window.minSize = new Vector2(1200, 700);
        }

        private void OnEnable()
        {
            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        private void OnGUI()
        {
            InitializeStyles();
            DrawUI();
        }

        private void DrawUI()
        {
            // Draw toolbar at top
            DrawToolbar();

            // Draw main layout below toolbar
            float toolbarHeight = 25;
            Rect canvasArea = new Rect(0, toolbarHeight, position.width - PropertiesPanelWidth, position.height - toolbarHeight);
            Rect propertiesArea = new Rect(canvasArea.xMax, toolbarHeight, PropertiesPanelWidth, position.height - toolbarHeight);

            // Draw canvas
            DrawCanvas(canvasArea);
            HandleCanvasInput(canvasArea);

            // Draw divider
            GUI.Box(new Rect(canvasArea.xMax - 1, toolbarHeight, 1, position.height - toolbarHeight), "");

            // Draw properties panel
            DrawPropertiesPanel(propertiesArea);
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUILayout.LabelField("Dialogue Editor", GUILayout.Width(120));

                var newGraph = (Data.DialogueGraph)EditorGUILayout.ObjectField(_currentGraph, typeof(Data.DialogueGraph), false, GUILayout.Width(200));
                if (newGraph != _currentGraph)
                {
                    _currentGraph = newGraph;
                    _selectedNodeId = -1;
                    _nodePositions.Clear();
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(50)))
                    CreateNewGraph();

                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50)))
                    SaveGraph();

                if (GUILayout.Button("+ Line", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    if (_currentGraph) AddLineNode();

                if (GUILayout.Button("+ Choice", EditorStyles.toolbarButton, GUILayout.Width(70)))
                    if (_currentGraph) AddChoiceNode();

                if (GUILayout.Button("Delete", EditorStyles.toolbarButton, GUILayout.Width(50)))
                    if (_currentGraph && _selectedNodeId >= 0) DeleteNode();
            }
        }

        private void DrawCanvas(Rect area)
        {
            if (_currentGraph == null)
            {
                GUI.Box(area, "");
                GUI.Label(area, "No graph selected. Create or select a graph.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            GUI.Box(area, "");
            GUI.BeginClip(area);
            Handles.BeginGUI();

            DrawGrid(area);
            DrawConnections();
            DrawNodes();

            Handles.EndGUI();
            GUI.EndClip();
        }

        private void DrawGrid(Rect area)
        {
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);
            float gridSpacing = GridSize * _zoom;
            float offsetX = _panOffset.x % gridSpacing;
            float offsetY = _panOffset.y % gridSpacing;

            for (float x = -offsetX; x < area.width; x += gridSpacing)
                Handles.DrawLine(new Vector3(x, 0, 0), new Vector3(x, area.height, 0));

            for (float y = -offsetY; y < area.height; y += gridSpacing)
                Handles.DrawLine(new Vector3(0, y, 0), new Vector3(area.width, y, 0));
        }

        private void DrawConnections()
        {
            if (_currentGraph?.Nodes == null) return;
            Handles.color = Color.gray;

            foreach (var node in _currentGraph.Nodes)
            {
                if (node.NextNodes == null || node.NextNodes.Length == 0) continue;

                Rect fromRect = GetNodeScreenRect(node.Id.Value);
                Vector2 fromPos = fromRect.center + Vector2.right * fromRect.width / 2;

                foreach (var nextId in node.NextNodes)
                {
                    var nextNode = _currentGraph.GetNode(nextId);
                    if (nextNode == null) continue;

                    Rect toRect = GetNodeScreenRect(nextId.Value);
                    Vector2 toPos = toRect.center - Vector2.right * toRect.width / 2;

                    Handles.DrawBezier(fromPos, toPos, fromPos + Vector2.right * 50, toPos - Vector2.right * 50, Color.gray, null, 2f);
                }
            }

            if (_connectingFromNodeId >= 0)
            {
                Rect fromRect = GetNodeScreenRect(_connectingFromNodeId);
                Vector2 fromPos = fromRect.center + Vector2.right * fromRect.width / 2;
                Handles.DrawLine(fromPos, Event.current.mousePosition, 2f);
            }
        }

        private void DrawNodes()
        {
            if (_currentGraph?.Nodes == null) return;

            foreach (var node in _currentGraph.Nodes)
            {
                Rect nodeRect = GetNodeScreenRect(node.Id.Value);
                bool isSelected = node.Id.Value == _selectedNodeId;
                GUIStyle style = isSelected ? _selectedNodeStyle : (node is Data.LineNode ? _lineNodeStyle : _choiceNodeStyle);

                GUI.Box(nodeRect, node.GetDisplayName(node.Id), style);

                Rect btnRect = new Rect(nodeRect.xMax - 12, nodeRect.center.y - 6, 12, 12);
                if (GUI.Button(btnRect, "→", EditorStyles.miniButton))
                    _connectingFromNodeId = (_connectingFromNodeId == node.Id.Value) ? -1 : node.Id.Value;
            }
        }

        private void HandleCanvasInput(Rect area)
        {
            Event evt = Event.current;
            if (!area.Contains(evt.mousePosition)) return;

            Vector2 canvasPos = evt.mousePosition - area.position;

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                _selectedNodeId = -1;
                if (_currentGraph?.Nodes != null)
                {
                    for (int i = _currentGraph.Nodes.Length - 1; i >= 0; i--)
                    {
                        Rect nodeRect = GetNodeScreenRect(_currentGraph.Nodes[i].Id.Value);
                        if (nodeRect.Contains(canvasPos))
                        {
                            _selectedNodeId = _currentGraph.Nodes[i].Id.Value;
                            evt.Use();
                            break;
                        }
                    }
                }
            }
            else if (evt.type == EventType.MouseDrag && (evt.button == 2 || (evt.button == 0 && evt.modifiers == EventModifiers.Alt)))
            {
                _panOffset += evt.delta;
                evt.Use();
            }
            else if (evt.type == EventType.ScrollWheel)
            {
                _zoom *= (evt.delta.y > 0) ? 0.9f : 1.1f;
                _zoom = Mathf.Clamp(_zoom, MinZoom, MaxZoom);
                evt.Use();
            }
            else if (evt.type == EventType.MouseUp && _connectingFromNodeId >= 0)
            {
                if (_currentGraph?.Nodes != null)
                {
                    foreach (var node in _currentGraph.Nodes)
                    {
                        Rect nodeRect = GetNodeScreenRect(node.Id.Value);
                        if (nodeRect.Contains(canvasPos) && node.Id.Value != _connectingFromNodeId)
                        {
                            ConnectNodes(_connectingFromNodeId, node.Id.Value);
                            _connectingFromNodeId = -1;
                            evt.Use();
                            break;
                        }
                    }
                }
                _connectingFromNodeId = -1;
            }
            else if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Delete && _selectedNodeId >= 0)
            {
                DeleteNode();
                evt.Use();
            }
        }

        private void DrawPropertiesPanel(Rect area)
        {
            GUI.Box(area, "");

            float margin = 5;
            Rect scrollArea = new Rect(area.x + margin, area.y + margin, area.width - 2 * margin, area.height - 2 * margin);
            Rect contentArea = new Rect(0, 0, scrollArea.width - 20, 2000);

            _propertiesScrollPosition = GUI.BeginScrollView(scrollArea, _propertiesScrollPosition, contentArea);

            float y = 10;

            if (_currentGraph == null)
            {
                GUI.Label(new Rect(5, y, contentArea.width - 10, 30), "No graph selected", EditorStyles.wordWrappedLabel);
            }
            else
            {
                // Graph properties
                GUI.Label(new Rect(5, y, contentArea.width - 10, 20), "GRAPH", EditorStyles.boldLabel);
                y += 25;

                GUI.Label(new Rect(5, y, 60, 20), "Name:");
                _currentGraph.GraphName = GUI.TextField(new Rect(70, y, contentArea.width - 75, 20), _currentGraph.GraphName);
                y += 25;

                GUI.Label(new Rect(5, y, 60, 20), "Start:");
                string startStr = GUI.TextField(new Rect(70, y, contentArea.width - 75, 20), _currentGraph.StartNode.Value.ToString());
                if (int.TryParse(startStr, out int startId))
                    _currentGraph.StartNode = new Data.NodeId(startId);
                y += 30;

                // Selected node properties
                if (_selectedNodeId >= 0)
                {
                    var node = _currentGraph.GetNode(new Data.NodeId(_selectedNodeId));
                    if (node != null)
                    {
                        GUI.Label(new Rect(5, y, contentArea.width - 10, 20), "NODE", EditorStyles.boldLabel);
                        y += 25;

                        GUI.Label(new Rect(5, y, contentArea.width - 10, 20), "ID: " + _selectedNodeId);
                        y += 25;

                        GUI.Label(new Rect(5, y, contentArea.width - 10, 20), "Type: " + node.GetType().Name);
                        y += 30;

                        if (node is Data.LineNode lineNode)
                        {
                            GUI.Label(new Rect(5, y, contentArea.width - 10, 20), "LINE NODE", EditorStyles.boldLabel);
                            y += 25;

                            GUI.Label(new Rect(5, y, 60, 20), "Text Key:");
                            string textStr = GUI.TextField(new Rect(70, y, contentArea.width - 75, 20), lineNode.TextKey.Value.ToString());
                            if (int.TryParse(textStr, out int textId))
                                lineNode.TextKey = new Data.TextKey(textId);
                            y += 25;

                            GUI.Label(new Rect(5, y, 60, 20), "Speaker:");
                            string speakerStr = GUI.TextField(new Rect(70, y, contentArea.width - 75, 20), lineNode.Speaker.Value.ToString());
                            if (int.TryParse(speakerStr, out int speakerId))
                                lineNode.Speaker = new Data.SpeakerId(speakerId);
                            y += 25;

                            GUI.Label(new Rect(5, y, 60, 20), "Style:");
                            string styleStr = GUI.TextField(new Rect(70, y, contentArea.width - 75, 20), lineNode.Style.Value.ToString());
                            if (int.TryParse(styleStr, out int styleId))
                                lineNode.Style = new Data.StyleId(styleId);
                            y += 30;

                            EditorUtility.SetDirty(_currentGraph);
                        }
                        else if (node is Data.ChoiceNode choiceNode)
                        {
                            GUI.Label(new Rect(5, y, contentArea.width - 10, 20), "CHOICE NODE", EditorStyles.boldLabel);
                            y += 25;

                            GUI.Label(new Rect(5, y, contentArea.width - 10, 20), "Options: " + choiceNode.Options.Length);
                            y += 25;

                            if (GUI.Button(new Rect(5, y, contentArea.width - 10, 20), "Add Option"))
                            {
                                var opts = new List<Data.ChoiceOption>(choiceNode.Options);
                                opts.Add(new Data.ChoiceOption
                                {
                                    TextKey = new Data.TextKey(0),
                                    NextNode = new Data.NodeId(-1),
                                    ConditionSerializedReferences = new string[0]
                                });
                                choiceNode.Options = opts.ToArray();
                                EditorUtility.SetDirty(_currentGraph);
                            }
                            y += 25;

                            EditorUtility.SetDirty(_currentGraph);
                        }

                        y += 10;
                        GUI.Label(new Rect(5, y, contentArea.width - 10, 20), "CONNECTIONS", EditorStyles.boldLabel);
                        y += 25;
                        GUI.Label(new Rect(5, y, contentArea.width - 10, 20), "Next Nodes: " + node.NextNodes.Length);
                        y += 25;
                    }
                }
                else
                {
                    GUI.Label(new Rect(5, y, contentArea.width - 10, 60), "Click a node on the canvas to select it and edit its properties.", EditorStyles.wordWrappedLabel);
                }
            }

            GUI.EndScrollView();
        }

        private Rect GetNodeScreenRect(int nodeId)
        {
            if (!_nodePositions.TryGetValue(nodeId, out var worldPos))
                worldPos = _nodePositions[nodeId] = new Vector2(200 + nodeId * 250, 200);

            Vector2 screenPos = (worldPos + _panOffset) * _zoom;
            return new Rect(screenPos, new Vector2(NodeWidth, NodeHeight) * _zoom);
        }

        private void ConnectNodes(int fromId, int toId)
        {
            var fromNode = _currentGraph.GetNode(new Data.NodeId(fromId));
            if (fromNode == null) return;

            var nextNodes = new List<Data.NodeId>(fromNode.NextNodes);
            if (!nextNodes.Any(n => n.Value == toId))
            {
                nextNodes.Add(new Data.NodeId(toId));
                fromNode.NextNodes = nextNodes.ToArray();
                EditorUtility.SetDirty(_currentGraph);
            }
        }

        private void AddLineNode()
        {
            if (_currentGraph.Nodes == null) _currentGraph.Nodes = new Data.DialogueNode[0];

            int id = _currentGraph.Nodes.Length;
            var node = new Data.LineNode
            {
                Id = new Data.NodeId(id),
                TextKey = new Data.TextKey(0),
                Speaker = Data.SpeakerId.None,
                Style = Data.StyleId.Default,
                NextNodes = new Data.NodeId[0],
                Commands = new Data.CommandData[0],
                ConditionSerializedReferences = new string[0]
            };

            var nodes = new Data.DialogueNode[_currentGraph.Nodes.Length + 1];
            System.Array.Copy(_currentGraph.Nodes, nodes, _currentGraph.Nodes.Length);
            nodes[nodes.Length - 1] = node;
            _currentGraph.Nodes = nodes;

            _nodePositions[id] = new Vector2(200 + id * 250, 200);
            EditorUtility.SetDirty(_currentGraph);
        }

        private void AddChoiceNode()
        {
            if (_currentGraph.Nodes == null) _currentGraph.Nodes = new Data.DialogueNode[0];

            int id = _currentGraph.Nodes.Length;
            var node = new Data.ChoiceNode
            {
                Id = new Data.NodeId(id),
                Options = new Data.ChoiceOption[0],
                NextNodes = new Data.NodeId[0]
            };

            var nodes = new Data.DialogueNode[_currentGraph.Nodes.Length + 1];
            System.Array.Copy(_currentGraph.Nodes, nodes, _currentGraph.Nodes.Length);
            nodes[nodes.Length - 1] = node;
            _currentGraph.Nodes = nodes;

            _nodePositions[id] = new Vector2(200 + id * 250, 200);
            EditorUtility.SetDirty(_currentGraph);
        }

        private void DeleteNode()
        {
            if (_selectedNodeId < 0 || _currentGraph.Nodes == null) return;

            _currentGraph.Nodes = _currentGraph.Nodes.Where(n => n.Id.Value != _selectedNodeId).ToArray();
            _nodePositions.Remove(_selectedNodeId);

            foreach (var node in _currentGraph.Nodes)
                node.NextNodes = node.NextNodes.Where(n => n.Value != _selectedNodeId).ToArray();

            _selectedNodeId = -1;
            EditorUtility.SetDirty(_currentGraph);
        }

        private void CreateNewGraph()
        {
            var graph = ScriptableObject.CreateInstance<Data.DialogueGraph>();
            string path = EditorUtility.SaveFilePanelInProject("Save Graph", "NewDialogue", "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                graph.GraphName = "New Dialogue";
                graph.StartNode = new Data.NodeId(0);
                graph.Nodes = new Data.DialogueNode[0];
                AssetDatabase.CreateAsset(graph, path);
                AssetDatabase.SaveAssets();
                _currentGraph = graph;
                _nodePositions.Clear();
            }
        }

        private void SaveGraph()
        {
            if (_currentGraph == null) return;
            EditorUtility.SetDirty(_currentGraph);
            AssetDatabase.SaveAssets();
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _lineNodeStyle = new GUIStyle(EditorStyles.helpBox)
            {
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(5, 5, 5, 5)
            };
            _lineNodeStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.4f, 0.6f));

            _choiceNodeStyle = new GUIStyle(_lineNodeStyle);
            _choiceNodeStyle.normal.background = MakeTex(2, 2, new Color(0.6f, 0.4f, 0.2f));

            _selectedNodeStyle = new GUIStyle(_lineNodeStyle);
            _selectedNodeStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.6f, 0.2f));

            _stylesInitialized = true;
        }

        private static Texture2D MakeTex(int width, int height, Color color)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            var tex = new Texture2D(width, height);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
#endif

