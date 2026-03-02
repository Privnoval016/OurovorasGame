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
    /** <summary>
     * Editor window for authoring considerations across all <see cref="AIActionBase"/> assets.
     * Open via <c>Window → UtilityAI → Consideration Editor</c>.
     *
     * Left panel   — searchable list of every AIActionBase asset in the project.
     * Centre panel — full consideration editor for the selected action, with type picker,
     *                copy/paste, and link support.
     * Right panel  — clipboard / linked-asset management; drag ConsiderationAssets here.
     * </summary>
     */
    public class ConsiderationEditorWindow : EditorWindow
    {
        [MenuItem("Window/UtilityAI/Consideration Editor")]
        public static void Open() => GetWindow<ConsiderationEditorWindow>("Consideration Editor");

        #region State

        private List<AIActionBase> _actions = new();
        private AIActionBase _selected;
        private SerializedObject _selectedSo;

        private string _search = "";
        private Vector2 _listScroll;
        private Vector2 _editorScroll;

        // Static clipboard shared with inline ConsiderationDrawer
        private static string _cbJson;
        private static string _cbType;

        private static List<Type> _allTypes;
        private bool _stylesReady;

        #endregion

        #region Colours

        private static readonly Color ColSideBg   = new(0.16f, 0.16f, 0.16f);
        private static readonly Color ColSelected  = new(0.24f, 0.42f, 0.7f);
        private static readonly Color ColHeader    = new(0.12f, 0.12f, 0.12f);
        private static readonly Color ColSeparator = new(0.3f,  0.3f,  0.3f);
        private static readonly Color ColLow       = new(0.7f,  0.2f,  0.1f);
        private static readonly Color ColHigh      = new(0.2f,  0.85f, 0.3f);
        private static readonly Color ColBg        = new(0.13f, 0.13f, 0.13f);

        #endregion

        #region Styles

        private GUIStyle _listItemStyle;
        private GUIStyle _listItemSelectedStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _sectionLabelStyle;

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            if (EditorStyles.label == null) return;

            _listItemStyle = new GUIStyle(EditorStyles.label)
            {
                padding = new RectOffset(8, 4, 4, 4),
                fontSize = 11
            };
            _listItemSelectedStyle = new GUIStyle(_listItemStyle)
            {
                normal = { textColor = Color.white }
            };
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = Color.white }
            };
            _sectionLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };

            _stylesReady = true;
        }

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            titleContent = new GUIContent("Consideration Editor",
                EditorGUIUtility.IconContent("d_ScriptableObject Icon").image);
            minSize = new Vector2(680, 420);
            RefreshActionList();
            EnsureTypes();
        }

        private void OnFocus() => RefreshActionList();

        #endregion

        #region OnGUI

        private void OnGUI()
        {
            EnsureStyles();
            if (!_stylesReady) return;

            // Re-validate selected SO (survives domain reload if asset still exists)
            if (_selected != null && _selectedSo == null)
                _selectedSo = new SerializedObject(_selected);

            float w = position.width;
            float h = position.height;
            float listW = Mathf.Clamp(w * 0.26f, 160, 260);
            float editorW = w - listW;

            // ── Toolbar ───────────────────────────────────────────────────────────
            DrawToolbar();
            float bodyY = 22f;

            // ── Left: action list ─────────────────────────────────────────────────
            Rect listRect = new(0, bodyY, listW, h - bodyY);
            DrawActionList(listRect);

            // ── Separator ─────────────────────────────────────────────────────────
            EditorGUI.DrawRect(new Rect(listW, bodyY, 1, h - bodyY), ColSeparator);

            // ── Right: consideration editor ───────────────────────────────────────
            Rect editorRect = new(listW + 1, bodyY, editorW - 1, h - bodyY);
            if (_selected != null && _selectedSo != null)
                DrawConsiderationEditor(editorRect);
            else
                DrawEmptyState(editorRect);
        }

        #endregion

        #region Toolbar

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Consideration Editor", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                RefreshActionList();
            if (GUILayout.Button("Brain Debugger", EditorStyles.toolbarButton, GUILayout.Width(100)))
                EditorApplication.ExecuteMenuItem("Window/UtilityAI/Brain Debugger");

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Action List

        private void DrawActionList(Rect rect)
        {
            // Background
            EditorGUI.DrawRect(rect, ColSideBg);

            GUILayout.BeginArea(rect);

            // Search bar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20)))
                _search = "";
            EditorGUILayout.EndHorizontal();

            _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUIStyle.none, GUI.skin.verticalScrollbar);

            string filter = _search.ToLowerInvariant();
            string lastType = null;

            foreach (var action in _actions)
            {
                if (action == null) continue;
                string actionName = action.name;
                string typeName = action.GetType().Name;

                if (!string.IsNullOrEmpty(filter) &&
                    !actionName.ToLowerInvariant().Contains(filter) &&
                    !typeName.ToLowerInvariant().Contains(filter))
                    continue;

                // Type group header
                if (typeName != lastType)
                {
                    EditorGUILayout.LabelField(typeName, _sectionLabelStyle ?? EditorStyles.miniLabel,
                        GUILayout.Height(16));
                    lastType = typeName;
                }

                bool isSelected = _selected == action;
                Rect rowRect = EditorGUILayout.GetControlRect(GUILayout.Height(22));

                if (isSelected)
                    EditorGUI.DrawRect(rowRect, ColSelected);

                // Consideration type badge
                string badge = "(none)";
                if (action.consideration != null)
                    badge = action.consideration.DisplayName;

                Rect nameRect = new(rowRect.x + 8, rowRect.y + 3, rowRect.width * 0.6f - 8, 16);
                Rect badgeRect = new(rowRect.x + rowRect.width * 0.6f, rowRect.y + 3, rowRect.width * 0.4f - 4, 16);

                EditorGUI.LabelField(nameRect, actionName,
                    isSelected ? (_listItemSelectedStyle ?? EditorStyles.label) : (_listItemStyle ?? EditorStyles.label));
                EditorGUI.LabelField(badgeRect, badge, EditorStyles.centeredGreyMiniLabel);

                if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
                {
                    SelectAction(action);
                    Event.current.Use();
                    Repaint();
                }
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        #endregion

        #region Consideration Editor

        private void DrawConsiderationEditor(Rect rect)
        {
            GUILayout.BeginArea(rect);
            _editorScroll = EditorGUILayout.BeginScrollView(_editorScroll);

            _selectedSo.Update();

            // ── Header ────────────────────────────────────────────────────────────
            Rect headerRect = EditorGUILayout.GetControlRect(GUILayout.Height(32));
            EditorGUI.DrawRect(headerRect, ColHeader);
            EditorGUI.LabelField(new Rect(headerRect.x + 8, headerRect.y + 7, headerRect.width - 100, 18),
                _selected.name, _headerStyle ?? EditorStyles.boldLabel);

            // Ping button
            Rect pingRect = new(headerRect.xMax - 88, headerRect.y + 6, 84, 20);
            if (GUI.Button(pingRect, "Ping Asset", EditorStyles.miniButton))
                EditorGUIUtility.PingObject(_selected);

            EditorGUILayout.Space(4);

            // ── Action fields (everything except consideration) ───────────────────
            EditorGUILayout.LabelField("Action Settings", EditorStyles.boldLabel);
            var iter = _selectedSo.GetIterator();
            iter.NextVisible(true);
            while (iter.NextVisible(false))
            {
                if (iter.name == "consideration") continue;
                EditorGUILayout.PropertyField(iter, true);
            }

            EditorGUILayout.Space(8);

            // ── Consideration section header ──────────────────────────────────────
            DrawSectionHeader("Consideration");

            // Clipboard toolbar
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Change Type", GUILayout.Height(22)))
                ShowTypePickerMenu();

            using (new EditorGUI.DisabledScope(_selectedSo.FindProperty("consideration").managedReferenceValue == null))
            {
                if (GUILayout.Button("Copy", GUILayout.Height(22)))
                    CopyConsideration();
            }

            bool canPaste = !string.IsNullOrEmpty(_cbJson);
            using (new EditorGUI.DisabledScope(!canPaste))
            {
                if (GUILayout.Button("Paste (copy)", GUILayout.Height(22)))
                    PasteConsideration(linked: false);
                if (GUILayout.Button("Paste (link)", GUILayout.Height(22)))
                    PasteConsideration(linked: true);
            }

            if (GUILayout.Button("↗ Link Asset", GUILayout.Height(22)))
                SwitchToLinkedAsset();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // ── Inline consideration fields ───────────────────────────────────────
            var conProp = _selectedSo.FindProperty("consideration");
            DrawConsiderationBody(conProp);

            _selectedSo.ApplyModifiedProperties();

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawConsiderationBody(SerializedProperty conProp)
        {
            var val = conProp.managedReferenceValue;
            if (val == null)
            {
                EditorGUILayout.HelpBox("No consideration set. Click 'Change Type' to add one.", MessageType.Info);
                return;
            }

            // Type label
            string typeName = val.GetType().Name.Replace("Consideration", "");
            EditorGUILayout.LabelField($"Type: {typeName}", EditorStyles.miniLabel);
            EditorGUILayout.Space(2);

            // Draw all child properties
            var iter = conProp.Copy();
            var end = conProp.GetEndProperty();
            bool entered = iter.NextVisible(true);
            if (!entered) return;

            do
            {
                if (SerializedProperty.EqualContents(iter, end)) break;
                EditorGUILayout.PropertyField(iter, true);

                // Curve preview bar
                if (iter.propertyType == SerializedPropertyType.AnimationCurve
                    && iter.animationCurveValue != null)
                {
                    DrawCurveBar(iter.animationCurveValue);
                }
            }
            while (iter.NextVisible(false));
        }

        private void DrawEmptyState(Rect rect)
        {
            GUILayout.BeginArea(rect);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Select an action from the list.",
                EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndArea();
        }

        #endregion

        #region Type Picker

        private void ShowTypePickerMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("(none)"), false, () =>
            {
                _selectedSo.FindProperty("consideration").managedReferenceValue = null;
                _selectedSo.ApplyModifiedProperties();
            });
            menu.AddSeparator("");
            foreach (var t in _allTypes)
            {
                var captured = t;
                menu.AddItem(new GUIContent(t.Name.Replace("Consideration", "")), false, () =>
                {
                    _selectedSo.FindProperty("consideration").managedReferenceValue =
                        Activator.CreateInstance(captured);
                    _selectedSo.ApplyModifiedProperties();
                    Repaint();
                });
            }
            menu.ShowAsContext();
        }

        #endregion

        #region Copy / Paste / Link

        private void CopyConsideration()
        {
            var val = _selectedSo.FindProperty("consideration").managedReferenceValue;
            if (val == null) return;
            _cbType = val.GetType().AssemblyQualifiedName;
            _cbJson = JsonUtility.ToJson(val);
        }

        private void PasteConsideration(bool linked)
        {
            if (string.IsNullOrEmpty(_cbJson)) return;
            var prop = _selectedSo.FindProperty("consideration");

            if (linked)
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "Save Linked Consideration Asset",
                    "LinkedConsideration", "asset",
                    "Choose where to save the shared ConsiderationAsset.");
                if (string.IsNullOrEmpty(path)) return;

                var asset = CreateInstance<ConsiderationAsset>();
                Type t = Type.GetType(_cbType);
                if (t != null)
                {
                    var instance = Activator.CreateInstance(t);
                    JsonUtility.FromJsonOverwrite(_cbJson, instance);
                    asset.consideration = (Consideration)instance;
                }
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();

                prop.managedReferenceValue = new LinkedAssetConsideration { asset = asset };
            }
            else
            {
                Type t = Type.GetType(_cbType);
                if (t == null) return;
                var instance = Activator.CreateInstance(t);
                JsonUtility.FromJsonOverwrite(_cbJson, instance);
                prop.managedReferenceValue = instance;
            }

            _selectedSo.ApplyModifiedProperties();
            Repaint();
        }

        private void SwitchToLinkedAsset()
        {
            _selectedSo.FindProperty("consideration").managedReferenceValue =
                new LinkedAssetConsideration();
            _selectedSo.ApplyModifiedProperties();
            Repaint();
        }

        #endregion

        #region Helpers

        private void SelectAction(AIActionBase action)
        {
            _selected = action;
            _selectedSo = action != null ? new SerializedObject(action) : null;
        }

        private void RefreshActionList()
        {
            _actions.Clear();
            var guids = AssetDatabase.FindAssets($"t:{nameof(AIActionBase)}");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<AIActionBase>(path);
                if (asset != null) _actions.Add(asset);
            }
            _actions = _actions
                .OrderBy(a => a.GetType().Name)
                .ThenBy(a => a.name)
                .ToList();
        }

        private static void EnsureTypes()
        {
            if (_allTypes != null) return;
            _allTypes = new List<Type>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var t in asm.GetTypes())
                    {
                        if (t.IsAbstract) continue;
                        if (!typeof(Consideration).IsAssignableFrom(t)) continue;
                        if (t.GetCustomAttribute<SerializableAttribute>() == null) continue;
                        _allTypes.Add(t);
                    }
                }
                catch (Exception) { /* skip assemblies that fail reflection */ }
            }
            _allTypes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        }

        private void DrawSectionHeader(string label)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(20));
            EditorGUI.DrawRect(r, ColHeader);
            EditorGUI.LabelField(new Rect(r.x + 6, r.y + 2, r.width, 16), label,
                EditorStyles.boldLabel);
        }

        private void DrawCurveBar(AnimationCurve curve)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(14));
            EditorGUI.DrawRect(r, ColBg);
            int steps = Mathf.RoundToInt(r.width);
            if (steps < 2) return;
            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)(steps - 1);
                float v = Mathf.Clamp01(curve.Evaluate(t));
                EditorGUI.DrawRect(new Rect(r.x + i, r.yMax - r.height * v, 1, r.height * v),
                    Color.Lerp(ColLow, ColHigh, v));
            }
        }

        #endregion
    }
}
#endif











