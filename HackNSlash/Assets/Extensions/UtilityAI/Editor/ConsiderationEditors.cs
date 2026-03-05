#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Extensions.UtilityAI.ConsiderationBases;
using UnityEditor;
using UnityEngine;

namespace Extensions.UtilityAI.Editor
{
    /** <summary>
     * Property drawer for any <c>[SerializeReference] Consideration</c> field.
     * Handles all types including recursive <see cref="CompositeConsideration"/> nesting.
     * </summary>
     */
    [CustomPropertyDrawer(typeof(Consideration), true)]
    public class ConsiderationDrawer : PropertyDrawer
    {
        // ── Static clipboard ──────────────────────────────────────────────────────
        private static string _clipboard;
        private static string _clipboardType;

        // ── Discovered types (excludes Composite — composed inline) ───────────────
        private static List<Type> _leafTypes;
        private static List<Type> _allTypes;

        // ── Height cache — keyed by serialised property path + managed type name ──
        // Avoids repeated FindPropertyRelative + child iteration every repaint.
        private static readonly Dictionary<string, float> _heightCache = new(64);

        private static void EnsureTypes()
        {
            if (_allTypes != null) return;
            _allTypes = new List<Type>();
            _leafTypes = new List<Type>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var t in asm.GetTypes())
                    {
                        if (t.IsAbstract || !typeof(Consideration).IsAssignableFrom(t)) continue;
                        if (t.GetCustomAttribute<SerializableAttribute>() == null) continue;
                        _allTypes.Add(t);
                        if (t != typeof(CompositeConsideration))
                            _leafTypes.Add(t);
                    }
                }
                catch (Exception) { /* skip assemblies that fail reflection */ }
            }
            _allTypes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            _leafTypes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        }

        // ── Colors ────────────────────────────────────────────────────────────────
        private static readonly Color ColHeader     = new(0.18f, 0.18f, 0.18f);
        private static readonly Color ColComposite  = new(0.14f, 0.22f, 0.14f);
        private static readonly Color ColBorder     = new(0.35f, 0.35f, 0.35f);
        private static readonly Color ColLow        = new(0.7f,  0.2f,  0.1f);
        private static readonly Color ColHigh       = new(0.2f,  0.85f, 0.3f);
        private static readonly Color ColBg         = new(0.13f, 0.13f, 0.13f);
        private static readonly Color ColAddBtn     = new(0.2f,  0.35f, 0.2f);

        // ── Height calculation ────────────────────────────────────────────────────

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            EnsureTypes();

            // Only cache leaf-node heights. CompositeConsideration child count changes
            // dynamically so caching it causes stale heights and overlapping draws.
            bool isComposite = property.managedReferenceValue is CompositeConsideration;
            if (!isComposite)
            {
                string cacheKey = property.propertyPath + "|" + (property.managedReferenceFullTypename ?? "null");
                if (_heightCache.TryGetValue(cacheKey, out float cached))
                    return cached;
                float h = ComputePropertyHeight(property);
                _heightCache[cacheKey] = h;
                return h;
            }

            return ComputePropertyHeight(property);
        }

        private float ComputePropertyHeight(SerializedProperty property)
        {
            float h = 26f; // header bar

            if (property.managedReferenceValue == null)
                return h + 20f;

            if (property.managedReferenceValue is CompositeConsideration)
            {
                h += 20f; // AND-gate toggle
                var firstProp = property.FindPropertyRelative("first");
                h += (firstProp != null ? GetPropertyHeight(firstProp, GUIContent.none) : 20f) + 4f;
                var restProp = property.FindPropertyRelative("rest");
                if (restProp != null)
                {
                    for (int i = 0; i < restProp.arraySize; i++)
                    {
                        h += 20f;
                        h += GetPropertyHeight(restProp.GetArrayElementAtIndex(i), GUIContent.none) + 4f;
                    }
                }
                h += 22f;
                return h;
            }

            var iter = property.Copy();
            var end = property.GetEndProperty();
            if (iter.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(iter, end)) break;
                    h += EditorGUI.GetPropertyHeight(iter, true) + 2f;
                    if (iter.propertyType == SerializedPropertyType.AnimationCurve)
                        h += 14f;
                } while (iter.NextVisible(false));
            }
            return h + 2f;
        }

        // ── OnGUI ─────────────────────────────────────────────────────────────────

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EnsureTypes();
            EditorGUI.BeginProperty(position, label, property);
            float y = DrawConsideration(position, property, 0);
            _ = y;
            EditorGUI.EndProperty();
        }

        // Returns the y position after drawing (used for recursive composite children).
        private float DrawConsideration(Rect position, SerializedProperty property, int depth)
        {
            float y = position.y;
            float x = position.x + depth * 8f;
            float w = position.width - depth * 8f;

            bool isComposite = property.managedReferenceValue is CompositeConsideration;

            // ── Header bar ────────────────────────────────────────────────────────
            Rect headerRect = new(x, y, w, 24f);
            EditorGUI.DrawRect(headerRect, isComposite ? ColComposite : ColHeader);

            string shortName = GetShortName(property);
            Rect typeLabelRect = new(x + 4, y + 4, w * 0.35f, 16);
            EditorGUI.LabelField(typeLabelRect, shortName, EditorStyles.boldLabel);

            // Buttons — right-aligned
            float btnX = x + w;
            float btnY = y + 3f;

            // Link
            btnX -= w * 0.10f;
            if (GUI.Button(new Rect(btnX, btnY, w * 0.10f, 18), "↗", EditorStyles.miniButton))
                SetToLinkedType(property);

            // Paste
            btnX -= w * 0.11f;
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_clipboard)))
                if (GUI.Button(new Rect(btnX, btnY, w * 0.11f, 18), "Paste", EditorStyles.miniButton))
                    PasteFromClipboard(property);

            // Copy
            btnX -= w * 0.10f;
            using (new EditorGUI.DisabledScope(property.managedReferenceValue == null))
                if (GUI.Button(new Rect(btnX, btnY, w * 0.10f, 18), "Copy", EditorStyles.miniButton))
                    CopyToClipboard(property);

            // Change Type
            btnX -= w * 0.22f;
            if (GUI.Button(new Rect(btnX, btnY, w * 0.22f, 18), "Change Type", EditorStyles.miniButton))
                ShowTypePicker(property, allowComposite: depth == 0);

            // Compose (add AND/OR wrapper) — only on leaf, only at top level
            if (!isComposite && depth == 0)
            {
                btnX -= w * 0.18f;
                if (GUI.Button(new Rect(btnX, btnY, w * 0.18f, 18), "+ Compose", EditorStyles.miniButton))
                    WrapInComposite(property);
            }

            y += 26f;

            // ── Body ──────────────────────────────────────────────────────────────
            if (property.managedReferenceValue == null)
            {
                EditorGUI.LabelField(new Rect(x + 4, y, w - 8, 18),
                    "None — click Change Type", EditorStyles.centeredGreyMiniLabel);
                return y + 20f;
            }

            if (isComposite)
                return DrawCompositeBody(property, x, y, w, depth);

            return DrawLeafBody(property, x, y, w);
        }

        // ── Composite body ────────────────────────────────────────────────────────

        private float DrawCompositeBody(SerializedProperty property, float x, float y, float w, int depth)
        {
            // AND-gate toggle
            var andProp = property.FindPropertyRelative("allMustBeNonZero");
            if (andProp != null)
            {
                Rect toggleRect = new(x + 4, y, w - 8, 18);
                andProp.boolValue = EditorGUI.ToggleLeft(toggleRect,
                    new GUIContent("All Must Be Non-Zero (short-circuit AND)"), andProp.boolValue);
                y += 20f;
            }

            // Thin separator
            EditorGUI.DrawRect(new Rect(x, y, w, 1), ColBorder);
            y += 2f;

            // First child
            var firstProp = property.FindPropertyRelative("first");
            if (firstProp != null)
            {
                EditorGUI.LabelField(new Rect(x + 4, y, 40, 16), "FIRST", EditorStyles.centeredGreyMiniLabel);
                float childH = GetPropertyHeight(firstProp, GUIContent.none);
                y = DrawConsideration(new Rect(x + 12, y, w - 12, childH), firstProp, depth + 1);
                y += 4f;
            }

            // Rest children
            var restProp = property.FindPropertyRelative("rest");
            var opsProp = property.FindPropertyRelative("operations");
            if (restProp != null && opsProp != null)
            {
                int count = restProp.arraySize;
                // Sync operations array length
                while (opsProp.arraySize < count) opsProp.InsertArrayElementAtIndex(opsProp.arraySize);
                while (opsProp.arraySize > count) opsProp.DeleteArrayElementAtIndex(opsProp.arraySize - 1);

                for (int i = 0; i < count; i++)
                {
                    // Operation dropdown row
                    var opProp = opsProp.GetArrayElementAtIndex(i);
                    Rect opRow = new(x + 4, y, w - 8, 18);
                    Rect opLabel = new(x + 4, y, 30, 18);
                    Rect opDropdown = new(x + 34, y, 90, 18);
                    Rect opDelete = new(x + w - 22, y, 20, 18);

                    EditorGUI.LabelField(opLabel, "OP");
                    EditorGUI.PropertyField(opDropdown, opProp, GUIContent.none);

                    Color prev = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.7f, 0.2f, 0.2f);
                    if (GUI.Button(opDelete, "✕", EditorStyles.miniButton))
                    {
                        restProp.DeleteArrayElementAtIndex(i);
                        opsProp.DeleteArrayElementAtIndex(i);
                        property.serializedObject.ApplyModifiedProperties();
                        GUI.backgroundColor = prev;
                        break;
                    }
                    GUI.backgroundColor = prev;
                    y += 20f;

                    // Child consideration
                    var childProp = restProp.GetArrayElementAtIndex(i);
                    float childH = GetPropertyHeight(childProp, GUIContent.none);
                    y = DrawConsideration(new Rect(x + 12, y, w - 12, childH), childProp, depth + 1);
                    y += 4f;
                }

                // "+ Add Child" button
                Color addPrev = GUI.backgroundColor;
                GUI.backgroundColor = ColAddBtn;
                if (GUI.Button(new Rect(x + 4, y, w - 8, 20), "+ Add Child Consideration", EditorStyles.miniButton))
                {
                    restProp.InsertArrayElementAtIndex(count);
                    restProp.GetArrayElementAtIndex(count).managedReferenceValue = null;
                    opsProp.InsertArrayElementAtIndex(count);
                    property.serializedObject.ApplyModifiedProperties();
                }
                GUI.backgroundColor = addPrev;
                y += 22f;
            }

            return y;
        }

        // ── Leaf body ─────────────────────────────────────────────────────────────

        private float DrawLeafBody(SerializedProperty property, float x, float y, float w)
        {
            var iter = property.Copy();
            var end = property.GetEndProperty();
            if (!iter.NextVisible(true)) return y;

            do
            {
                if (SerializedProperty.EqualContents(iter, end)) break;

                float h = EditorGUI.GetPropertyHeight(iter, true);
                EditorGUI.PropertyField(new Rect(x + 4, y, w - 8, h), iter, true);
                y += h + 2f;

                if (iter.propertyType == SerializedPropertyType.AnimationCurve
                    && iter.animationCurveValue != null)
                {
                    DrawCurveBar(new Rect(x + 4, y, w - 8, 12), iter.animationCurveValue);
                    y += 14f;
                }
            } while (iter.NextVisible(false));

            return y + 2f;
        }

        // ── Wrap in composite ─────────────────────────────────────────────────────

        private static void WrapInComposite(SerializedProperty property)
        {
            // Snapshot current consideration
            var existing = property.managedReferenceValue;
            string json = existing != null ? JsonUtility.ToJson(existing) : null;
            string typeName = existing?.GetType().AssemblyQualifiedName;

            var composite = new CompositeConsideration();
            if (existing != null && json != null && typeName != null)
            {
                Type t = Type.GetType(typeName);
                if (t != null)
                {
                    var clone = (Consideration)Activator.CreateInstance(t);
                    JsonUtility.FromJsonOverwrite(json, clone);
                    composite.first = clone;
                }
            }

            property.managedReferenceValue = composite;
            _heightCache.Clear();
            property.serializedObject.ApplyModifiedProperties();
        }

        // ── Type picker ───────────────────────────────────────────────────────────

        private static void ShowTypePicker(SerializedProperty property, bool allowComposite)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("(none)"), false, () =>
            {
                property.managedReferenceValue = null;
                _heightCache.Clear();
                property.serializedObject.ApplyModifiedProperties();
            });
            menu.AddSeparator("");

            var list = allowComposite ? _allTypes : _leafTypes;
            foreach (var t in list)
            {
                var captured = t;
                string lbl = t.Name.Replace("Consideration", "");
                menu.AddItem(new GUIContent(lbl), false, () =>
                {
                    property.managedReferenceValue = Activator.CreateInstance(captured);
                    _heightCache.Clear();
                    property.serializedObject.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }

        // ── Copy / Paste ──────────────────────────────────────────────────────────

        private static void CopyToClipboard(SerializedProperty property)
        {
            var val = property.managedReferenceValue;
            if (val == null) return;
            _clipboardType = val.GetType().AssemblyQualifiedName;
            _clipboard = JsonUtility.ToJson(val);
        }

        private static void PasteFromClipboard(SerializedProperty property)
        {
            if (string.IsNullOrEmpty(_clipboard) || string.IsNullOrEmpty(_clipboardType)) return;
            Type t = Type.GetType(_clipboardType);
            if (t == null) return;
            var instance = Activator.CreateInstance(t);
            JsonUtility.FromJsonOverwrite(_clipboard, instance);
            property.managedReferenceValue = instance;
            _heightCache.Clear();
            property.serializedObject.ApplyModifiedProperties();
        }

        private static void SetToLinkedType(SerializedProperty property)
        {
            property.managedReferenceValue = new LinkedAssetConsideration();
            _heightCache.Clear();
            property.serializedObject.ApplyModifiedProperties();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string GetShortName(SerializedProperty property)
        {
            string typeName = property.managedReferenceFullTypename;
            if (string.IsNullOrEmpty(typeName)) return "(none)";
            int dot = typeName.LastIndexOf('.');
            return (dot >= 0 ? typeName[(dot + 1)..] : typeName).Replace("Consideration", "");
        }

        private static void DrawCurveBar(Rect r, AnimationCurve curve)
        {
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
    }

    // ── AIActionBase custom editor ─────────────────────────────────────────────────

    /** <summary>
     * Custom inspector for all <see cref="AIActionBase"/> ScriptableObjects.
     * Reads live utility scores from the <see cref="AIBrainUser"/> static registry —
     * no scene scanning of any kind.
     * </summary>
     */
    [CustomEditor(typeof(AIActionBase), true)]
    public class AIActionBaseEditor : UnityEditor.Editor
    {
        private static readonly Color ColBarBg  = new(0.15f, 0.15f, 0.15f);
        private static readonly Color ColBar    = new(0.3f,  0.6f,  1f);
        private static readonly Color ColBarWin = new(0.2f,  0.85f, 0.3f);

        // Brain resolved from the registry — refreshed when the registry changes.
        private IAIBrainAccessor _cachedBrain;
        private bool _registryDirty = true;

        private void OnEnable()
        {
            AIBrainUser.RegistryChanged += OnRegistryChanged;
            _registryDirty = true;
        }

        private void OnDisable()
        {
            AIBrainUser.RegistryChanged -= OnRegistryChanged;
        }

        private void OnRegistryChanged()
        {
            _registryDirty = true;
            _cachedBrain = null;
            Repaint();
        }

        private void RefreshBrainFromRegistry()
        {
            if (!_registryDirty) return;
            _registryDirty = false;
            _cachedBrain = null;
            string targetName = target.name;
            foreach (var user in AIBrainUser.All)
            {
                if (user == null) continue;
                foreach (var info in user.GetActionDebugInfos())
                {
                    if (info.ActionName == targetName)
                    {
                        _cachedBrain = user;
                        return;
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField(target.GetType().Name, EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            var iter = serializedObject.GetIterator();
            iter.NextVisible(true);
            while (iter.NextVisible(false))
            {
                if (iter.name == "consideration") continue;
                EditorGUILayout.PropertyField(iter, true);
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Consideration", EditorStyles.boldLabel);
            var conProp = serializedObject.FindProperty("consideration");
            EditorGUILayout.PropertyField(conProp, GUIContent.none, true);

            if (Application.isPlaying)
            {
                RefreshBrainFromRegistry();
                EditorGUILayout.Space(4);
                DrawLiveScoreBar();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawLiveScoreBar()
        {
            if (_cachedBrain == null)
            {
                EditorGUILayout.LabelField("No active brain found with this action.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            float score = -1f;
            string brainName = (_cachedBrain as Component)?.name ?? "?";
            foreach (var info in _cachedBrain.GetActionDebugInfos())
            {
                if (info.ActionName == target.name) { score = info.LastUtility; break; }
            }

            if (score < 0) return;
            EditorGUILayout.LabelField($"Brain: {brainName}  Utility: {score:F4}", EditorStyles.miniLabel);
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(16));
            EditorGUI.DrawRect(r, ColBarBg);
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width * Mathf.Clamp01(score), r.height),
                score > 0.5f ? ColBarWin : ColBar);
        }
    }

    // ── ConsiderationAsset editor ──────────────────────────────────────────────────

    /** <summary>Custom inspector for <see cref="ConsiderationAsset"/> — the linkable SO wrapper.</summary> */
    [CustomEditor(typeof(ConsiderationAsset))]
    public class ConsiderationAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Linked Consideration Asset", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Drag this asset into any action's '↗' slot to share it across multiple actions. " +
                "Editing here updates all linked actions automatically.",
                MessageType.Info);
            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("consideration"), GUIContent.none, true);
            serializedObject.ApplyModifiedProperties();
        }
    }

    internal static class ConsiderationEditorUtils
    {
        internal static void DrawKeyField(SerializedProperty keyProp, string label, string tooltip)
        {
            EditorGUILayout.PropertyField(keyProp, new GUIContent(label, tooltip));
            var idProp = keyProp.FindPropertyRelative("internalId");
            if (idProp == null || string.IsNullOrEmpty(idProp.stringValue))
                EditorGUILayout.HelpBox("⚠  Key is not set — will return 0 at runtime.", MessageType.Warning);
        }
    }
}
#endif



