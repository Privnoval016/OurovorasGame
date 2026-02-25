#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Extensions.UtilityAI.ConsiderationBases;
using UnityEditor;
using UnityEngine;

namespace Extensions.UtilityAI.Editor
{
    /** <summary>Editor window for inspecting and simulating any AIBrainUser in the scene. Open via Window → UtilityAI → Brain Debugger.</summary> */
    public class AIBrainEditorWindow : EditorWindow
    {
        [MenuItem("Window/UtilityAI/Brain Debugger")]
        public static void Open() => GetWindow<AIBrainEditorWindow>("Brain Debugger");

        #region State

        private IAIBrainAccessor _brain;
        private GameObject _selectedGo;
        private ActionDebugInfo? _selectedAction;
        private bool _showConsiderationTree = true;

        // edit-mode simulation
        private readonly Dictionary<string, float> _simulatedContext = new();
        private List<(string actionName, float utility)> _simResults = new();
        private string _simWinner;

        // scroll positions
        private Vector2 _actionListScroll;
        private Vector2 _contextScroll;
        private Vector2 _considerationScroll;
        private Vector2 _simScroll;

        // styles
        private GUIStyle _winnerStyle;
        private GUIStyle _normalStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _monoStyle;
        private GUIStyle _dimStyle;
        private bool _stylesReady;

        #endregion

        #region Colours

        private static readonly Color ColWinner = new Color(0.25f, 0.9f, 0.35f);
        private static readonly Color ColBar = new Color(0.3f, 0.6f, 1f);
        private static readonly Color ColBarWin = new Color(0.2f, 0.85f, 0.3f);
        private static readonly Color ColBarBg = new Color(0.15f, 0.15f, 0.15f);
        private static readonly Color ColHeader = new Color(0.12f, 0.12f, 0.12f);
        private static readonly Color ColSelected = new Color(0.24f, 0.42f, 0.7f);
        private static readonly Color ColRowAlt = new Color(0.2f, 0.2f, 0.2f);
        private static readonly Color ColRowBase = new Color(0.22f, 0.22f, 0.22f);

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            titleContent = new GUIContent("Brain Debugger", EditorGUIUtility.IconContent("d_UnityEditor.ProfilerWindow").image);
            minSize = new Vector2(520, 400);
            _stylesReady = false;
        }

        private void OnSelectionChange()
        {
            _brain = null;
            _selectedGo = null;
            _selectedAction = null;

            if (Selection.activeGameObject != null)
            {
                var acc = Selection.activeGameObject.GetComponent<IAIBrainAccessor>();
                if (acc != null)
                {
                    _brain = acc;
                    _selectedGo = Selection.activeGameObject;
                    RebuildSimContext();
                }
            }
            Repaint();
        }

        private void Update()
        {
            if (Application.isPlaying && _brain != null)
                Repaint();
        }

        #endregion

        #region OnGUI

        private void OnGUI()
        {
            // Re-fetch interface ref after domain reload (_selectedGo survives but interface ref doesn't).
            if (_selectedGo != null && _brain == null)
            {
                var acc = _selectedGo.GetComponent<IAIBrainAccessor>();
                if (acc != null) { _brain = acc; RebuildSimContext(); }
                else _selectedGo = null;
            }

            EnsureStyles();
            if (!_stylesReady) return;

            DrawToolbar();

            if (_brain == null) { DrawEmpty(); return; }

            if (Application.isPlaying)
                DrawRuntimeView();
            else
                DrawSimulateView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Brain Debugger", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (_selectedGo != null && _brain != null)
                GUILayout.Label($"{_selectedGo.name}  [{_brain.KeyTypeName}]", EditorStyles.miniLabel);
            Color prev = GUI.color;
            GUI.color = Application.isPlaying ? Color.green : new Color(1f, 0.85f, 0.3f);
            GUILayout.Label(Application.isPlaying ? "● LIVE" : "◉ SIMULATE", EditorStyles.boldLabel, GUILayout.Width(90));
            GUI.color = prev;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEmpty()
        {
            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Select a GameObject with an AIBrainUser component.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
            }
            GUILayout.FlexibleSpace();
        }

        #endregion

        #region Runtime View

        private void DrawRuntimeView()
        {
            var infos = _brain.GetActionDebugInfos();
            float h = position.height - 22f;
            float lw = Mathf.Max(200, position.width * 0.4f);
            float rw = position.width - lw - 6;

            EditorGUILayout.BeginHorizontal();

            // Left column: actions + context
            EditorGUILayout.BeginVertical(GUILayout.Width(lw));
            Header("Actions");
            _actionListScroll = EditorGUILayout.BeginScrollView(_actionListScroll, GUILayout.Height(h * 0.55f));
            for (int i = 0; i < infos.Count; i++)
                DrawActionRow(infos[i], lw - 20, i);
            EditorGUILayout.EndScrollView();
            GUILayout.Space(4);
            Header("Live Context");
            _contextScroll = EditorGUILayout.BeginScrollView(_contextScroll, GUILayout.Height(h * 0.35f));
            DrawContextTable(_brain.GetContextSnapshot());
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            GUILayout.Space(3);

            // Right column: consideration tree
            EditorGUILayout.BeginVertical(GUILayout.Width(rw));
            Header(_selectedAction.HasValue ? $"Considerations — {_selectedAction.Value.ActionName}" : "Considerations");
            if (_selectedAction.HasValue)
            {
                _showConsiderationTree = EditorGUILayout.Foldout(_showConsiderationTree, "Tree", true);
                if (_showConsiderationTree)
                {
                    _considerationScroll = EditorGUILayout.BeginScrollView(_considerationScroll);
                    EditorGUI.indentLevel = 0;
                    DrawConsiderationNode(_selectedAction.Value.Consideration, 0, true);
                    EditorGUILayout.EndScrollView();
                }
            }
            else
                EditorGUILayout.HelpBox("Click an action on the left to inspect its consideration tree.", MessageType.Info);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawActionRow(ActionDebugInfo info, float w, int rowIndex)
        {
            bool selected = _selectedAction.HasValue && _selectedAction.Value.ActionName == info.ActionName;
            bool winner = info.IsChosen;

            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(24));
            Color bg = selected ? ColSelected : (winner ? new Color(0.14f, 0.28f, 0.14f) : (rowIndex % 2 == 0 ? ColRowBase : ColRowAlt));
            EditorGUI.DrawRect(r, bg);

            // utility bar
            float barW = w * 0.3f;
            Rect bgBar = new Rect(r.xMax - barW - 4, r.y + 4, barW, r.height - 8);
            float filled = info.LastUtility >= 0 ? bgBar.width * Mathf.Clamp01(info.LastUtility) : 0;
            EditorGUI.DrawRect(bgBar, ColBarBg);
            if (filled > 0) EditorGUI.DrawRect(new Rect(bgBar.x, bgBar.y, filled, bgBar.height), winner ? ColBarWin : ColBar);

            // label + score
            EditorGUI.LabelField(new Rect(r.x + 6, r.y + 4, bgBar.x - r.x - 10, r.height - 8),
                (winner ? "★ " : "  ") + info.ActionName, winner ? _winnerStyle : _normalStyle);
            string scoreStr = info.LastUtility >= 0 ? info.LastUtility.ToString("F3") : "--";
            EditorGUI.LabelField(new Rect(bgBar.x, bgBar.y, bgBar.width, bgBar.height),
                new GUIContent(scoreStr), EditorStyles.centeredGreyMiniLabel);

            // click to select
            if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
            {
                _selectedAction = info;
                Event.current.Use();
                Repaint();
            }
        }

        private void DrawContextTable(IReadOnlyList<(string key, string value)> snapshot)
        {
            if (snapshot == null || snapshot.Count == 0)
            {
                EditorGUILayout.LabelField("(empty)", EditorStyles.centeredGreyMiniLabel);
                return;
            }
            foreach (var (k, v) in snapshot)
            {
                Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(16));
                EditorGUI.LabelField(new Rect(r.x, r.y, 160, r.height), k, _monoStyle);
                EditorGUI.LabelField(new Rect(r.x + 164, r.y, r.width - 164, r.height), v, _monoStyle);
            }
        }

        #endregion

        #region Consideration Tree

        // Draws one consideration node with live or simulated score bar, recursing into composites.
        private void DrawConsiderationNode(Consideration c, int depth, bool liveMode, IContextBase simCtx = null)
        {
            if (c == null)
            {
                EditorGUI.indentLevel = depth;
                EditorGUILayout.LabelField("(none)", _dimStyle);
                return;
            }

            float score = -1f;
            if (liveMode)
            {
                try { score = Mathf.Clamp01(c.Evaluate(new SnapshotContext(_brain.GetContextSnapshot(), _brain))); }
                catch (Exception) { score = -1f; }
            }
            else if (simCtx != null)
            {
                try { score = Mathf.Clamp01(c.Evaluate(simCtx)); }
                catch (Exception) { score = -1f; }
            }

            EditorGUI.indentLevel = depth;
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(20));

            // score bar behind the row
            if (score >= 0)
            {
                EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, r.height), ColBarBg);
                float bw = r.width * score;
                EditorGUI.DrawRect(new Rect(r.x, r.y, bw, r.height), score > 0.66f ? ColBarWin : (score > 0.33f ? ColBar : new Color(0.7f, 0.35f, 0.15f)));
            }

            float indent = depth * 16f;
            string label = $"{c.name}  ({c.GetType().Name})" + (score >= 0 ? $"  →  {score:F3}" : "");
            EditorGUI.LabelField(new Rect(r.x + indent, r.y, r.width - indent - 60, r.height), label, score >= 0 ? _winnerStyle : _normalStyle);

            // ping button
            if (GUI.Button(new Rect(r.xMax - 56, r.y + 2, 54, r.height - 4), "Select", EditorStyles.miniButton))
                Selection.activeObject = c;

            // recurse composites
            if (c is CompositeConsideration comp)
            {
                DrawConsiderationNode(comp.firstConsideration, depth + 1, liveMode, simCtx);
                foreach (var op in comp.considerations)
                {
                    EditorGUI.indentLevel = depth + 1;
                    EditorGUILayout.LabelField($"  {op.operation}", _dimStyle);
                    DrawConsiderationNode(op.consideration, depth + 2, liveMode, simCtx);
                }
            }
        }

        #endregion

        #region Simulate View

        private void DrawSimulateView()
        {
            // Lazily build sim context if empty (e.g. window opened after object already selected).
            if (_simulatedContext.Count == 0 && _brain != null)
                RebuildSimContext();

            float h = position.height - 22f;
            float lw = Mathf.Max(240, position.width * 0.42f);
            float rw = position.width - lw - 6;

            EditorGUILayout.BeginHorizontal();

            // Left: context inputs + simulate button + results
            EditorGUILayout.BeginVertical(GUILayout.Width(lw));
            Header("Simulated Context Values");
            _simScroll = EditorGUILayout.BeginScrollView(_simScroll, GUILayout.Height(h * 0.5f));
            DrawSimContextInputs();
            EditorGUILayout.EndScrollView();

            GUILayout.Space(6);
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.3f, 0.6f, 0.3f);
            if (GUILayout.Button("▶  Run Simulation", GUILayout.Height(30)))
                RunSimulation();
            GUI.backgroundColor = prev;

            if (_simResults.Count > 0)
            {
                GUILayout.Space(6);
                Header("Results");
                for (int i = 0; i < _simResults.Count; i++)
                {
                    var (aName, u) = _simResults[i];
                    bool win = aName == _simWinner;
                    Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(22));
                    EditorGUI.DrawRect(r, win ? new Color(0.14f, 0.28f, 0.14f) : (i % 2 == 0 ? ColRowBase : ColRowAlt));
                    float bw = (r.width - 60) * Mathf.Clamp01(u);
                    EditorGUI.DrawRect(new Rect(r.x, r.y, bw, r.height), win ? new Color(0.2f, 0.6f, 0.2f, 0.4f) : new Color(0.3f, 0.5f, 0.8f, 0.3f));
                    EditorGUI.LabelField(new Rect(r.x + 4, r.y + 3, r.width - 64, r.height - 6),
                        (win ? "★ " : "  ") + aName, win ? _winnerStyle : _normalStyle);
                    EditorGUI.LabelField(new Rect(r.xMax - 58, r.y + 3, 56, r.height - 6),
                        u.ToString("F4"), EditorStyles.centeredGreyMiniLabel);
                }
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(3);

            // Right: consideration tree for selected/winning action
            EditorGUILayout.BeginVertical(GUILayout.Width(rw));
            Header(_selectedAction.HasValue ? $"Tree — {_selectedAction.Value.ActionName}" : "Consideration Tree");
            if (_selectedAction.HasValue)
            {
                _considerationScroll = EditorGUILayout.BeginScrollView(_considerationScroll);
                EditorGUI.indentLevel = 0;
                DrawConsiderationNode(_selectedAction.Value.Consideration, 0, false, MakeSimCtx());
                EditorGUILayout.EndScrollView();
            }
            else
                EditorGUILayout.HelpBox("Run Simulation to populate results. The winning action's tree will appear here.", MessageType.Info);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSimContextInputs()
        {
            if (_simulatedContext.Count == 0)
            {
                EditorGUILayout.LabelField("No context keys found.", _dimStyle);
                return;
            }

            foreach (var key in _simulatedContext.Keys.ToList())
            {
                Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(18));
                EditorGUI.LabelField(new Rect(r.x, r.y, Mathf.Max(r.width * 0.6f, 160), r.height), key, _monoStyle);
                EditorGUI.BeginChangeCheck();
                float newVal = EditorGUI.FloatField(new Rect(r.x + Mathf.Max(r.width * 0.6f, 160) + 4, r.y, r.width - Mathf.Max(r.width * 0.6f, 160) - 4, r.height), _simulatedContext[key]);
                if (EditorGUI.EndChangeCheck())
                    _simulatedContext[key] = Mathf.Clamp01(newVal);
            }
        }

        private void RunSimulation()
        {
            _simResults.Clear();
            _simWinner = null;
            _selectedAction = null;

            var infos = _brain.GetActionDebugInfos();
            var ctx = MakeSimCtx();
            float best = float.MinValue;

            foreach (var info in infos)
            {
                float u = 1f;
                if (info.Consideration != null)
                {
                    try { u = Mathf.Clamp01(info.Consideration.Evaluate(ctx)); }
                    catch (Exception) { u = 0f; }
                }
                _simResults.Add((info.ActionName, u));
                if (u > best) { best = u; _simWinner = info.ActionName; }
            }

            _simResults.Sort((a, b) => b.utility.CompareTo(a.utility));

            var winner = infos.FirstOrDefault(i => i.ActionName == _simWinner);
            if (winner.ActionName != null) _selectedAction = winner;
            Repaint();
        }

        #endregion

        #region Helpers

        private void RebuildSimContext()
        {
            _simulatedContext.Clear();
            _simResults.Clear();
            _simWinner = null;
            if (_brain == null) return;

            // Seed every member of the TKey enum so the user can always tweak all keys,
            // regardless of whether the consideration assets have their keys configured.
            Type keyType = FindEnumType(_brain.KeyTypeName);
            if (keyType != null)
            {
                foreach (var name in Enum.GetNames(keyType))
                    _simulatedContext.TryAdd(name, 0.5f);
            }

            // Also seed from the live context snapshot when in play mode.
            if (Application.isPlaying)
            {
                foreach (var (k, _) in _brain.GetContextSnapshot())
                    _simulatedContext.TryAdd(k, 0.5f);
            }

            // Walk consideration fields as a supplementary pass (handles non-TKey enum keys on composites etc.)
            foreach (var info in _brain.GetActionDebugInfos())
                HarvestKeys(info.Consideration);
        }

        // Finds an enum Type by its short name, only among enums tagged [AIContextKey].
        private static Type FindEnumType(string shortName)
        {
            if (string.IsNullOrEmpty(shortName)) return null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                string asmName = asm.GetName().Name;
                if (asmName.StartsWith("Unity") || asmName.StartsWith("System") ||
                    asmName.StartsWith("mscorlib") || asmName.StartsWith("netstandard") ||
                    asmName.StartsWith("Mono.") || asmName.StartsWith("Microsoft."))
                    continue;
                try
                {
                    var t = asm.GetTypes().FirstOrDefault(x =>
                        x.IsEnum && x.IsPublic &&
                        x.IsDefined(typeof(Extensions.UtilityAI.AIContextKeyAttribute), false) &&
                        (x.Name == shortName || x.FullName == shortName));
                    if (t != null) return t;
                }
                catch { /* skip */ }
            }
            return null;
        }

        private void HarvestKeys(Consideration c)
        {
            if (c == null) return;
            foreach (var field in c.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (field.FieldType != typeof(EnumContextKey)) continue;
                var eck = (EnumContextKey)field.GetValue(c);
                Type t = eck.ResolveType();
                if (t == null) continue;
                // Use the short member name as key, matching what Context<TKey>.GetSnapshot() returns.
                string memberName = Enum.GetName(t, eck.enumValue);
                if (!string.IsNullOrEmpty(memberName))
                    _simulatedContext.TryAdd(memberName, 0.5f);
            }
            if (c is CompositeConsideration comp)
            {
                HarvestKeys(comp.firstConsideration);
                foreach (var op in comp.considerations)
                    HarvestKeys(op.consideration);
            }
        }

        // DictContext looks up by the raw enum member name, matching what Context<TKey> serialises.
        private IContextBase MakeSimCtx() => new DictContext(_simulatedContext, _brain);

        private void Header(string text)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(18));
            EditorGUI.DrawRect(r, ColHeader);
            EditorGUI.LabelField(r, "  " + text, _headerStyle);
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            if (EditorStyles.label == null) return;
            _winnerStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = ColWinner }, fontStyle = FontStyle.Bold, fontSize = 11 };
            _normalStyle = new GUIStyle(EditorStyles.label) { fontSize = 11 };
            _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11, normal = { textColor = Color.white } };
            _monoStyle = new GUIStyle(EditorStyles.label) { fontSize = 10 };
            _dimStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.55f, 0.55f, 0.55f) } };
            _stylesReady = true;
        }

        #endregion

        #region Lightweight Context Implementations

        // Backed by a snapshot of the live context. Enum keys map to their short member name.
        private class SnapshotContext : IContextBase
        {
            private readonly Dictionary<string, float> _data = new();
            private readonly IAIBrainAccessor _acc;

            public SnapshotContext(IReadOnlyList<(string key, string value)> snapshot, IAIBrainAccessor acc)
            {
                _acc = acc;
                foreach (var (k, v) in snapshot)
                    if (float.TryParse(v, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float f))
                        _data[k] = f;
            }

            public TValue GetData<TValue>(object key)
            {
                if (key == null) return default;
                string k = key is Enum e ? e.ToString() : key.ToString();
                if (typeof(TValue) == typeof(float) && _data.TryGetValue(k, out float f))
                    return (TValue)(object)f;
                return default;
            }

            public bool SetData<TValue>(object key, TValue value) => false;
            public Transform GetSensorTarget(object key) => null;
            public Transform GetBrainTransform() => (_acc as Component)?.transform;
        }

        // Backed by the edit-mode float dictionary. Keys are short enum member names.
        private class DictContext : IContextBase
        {
            private readonly Dictionary<string, float> _data;
            private readonly IAIBrainAccessor _acc;

            public DictContext(Dictionary<string, float> data, IAIBrainAccessor acc) { _data = data; _acc = acc; }

            public TValue GetData<TValue>(object key)
            {
                if (key == null) return default;
                // Accept both the short name ("SelfHealth") and the full enum.ToString() ("SelfHealth")
                string k = key is Enum e ? e.ToString() : key.ToString();
                if (typeof(TValue) == typeof(float) && _data.TryGetValue(k, out float f))
                    return (TValue)(object)f;
                return default;
            }

            public bool SetData<TValue>(object key, TValue value) => false;
            public Transform GetSensorTarget(object key) => null;
            public Transform GetBrainTransform() => (_acc as Component)?.transform;
        }

        #endregion
    }
}
#endif


