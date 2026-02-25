#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.UtilityAI.ConsiderationBases;
using UnityEditor;
using UnityEngine;

namespace Extensions.UtilityAI.Editor
{
    /**
     * <summary>
     * Custom <see cref="PropertyDrawer"/> for <see cref="EnumContextKey"/>.
     *
     * Replaces the raw <c>enumTypeName</c>/<c>enumValue</c> fields with a compact two-column
     * inline control:
     * <list type="bullet">
     *   <item>Left popup — selects the enum type from all enums discovered in the project's
     *         loaded assemblies (filtered to assemblies that reference the game's code).</item>
     *   <item>Right popup — selects the member within the chosen enum type.</item>
     * </list>
     *
     * The discovered enum list is cached and rebuilt only when the domain reloads.
     * </summary>
     */
    [CustomPropertyDrawer(typeof(EnumContextKey))]
    public class EnumContextKeyDrawer : PropertyDrawer
    {
        // ── Type discovery cache (rebuilt on domain reload) ────────────────────────
        private static List<Type> _cachedEnumTypes;
        private static string[]   _cachedTypeNames;   // display names
        private static string[]   _cachedTypeFullNames;

        [InitializeOnLoadMethod]
        private static void RebuildCache()
        {
            _cachedEnumTypes     = null;
            _cachedTypeNames     = null;
            _cachedTypeFullNames = null;
        }

        private static void EnsureCache()
        {
            if (_cachedEnumTypes != null) return;

            var enums = new List<Type>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Skip assemblies that can never contain user-attributed enums.
                string asmName = asm.GetName().Name;
                if (asmName.StartsWith("Unity", StringComparison.Ordinal)
                    || asmName.StartsWith("System", StringComparison.Ordinal)
                    || asmName.StartsWith("mscorlib", StringComparison.Ordinal)
                    || asmName.StartsWith("netstandard", StringComparison.Ordinal)
                    || asmName.StartsWith("Mono.", StringComparison.Ordinal)
                    || asmName.StartsWith("Microsoft.", StringComparison.Ordinal))
                    continue;

                try
                {
                    foreach (var t in asm.GetTypes())
                    {
                        if (t.IsEnum && t.IsPublic
                            && t.IsDefined(typeof(Extensions.UtilityAI.AIContextKeyAttribute), false))
                            enums.Add(t);
                    }
                }
                catch { /* skip assemblies that fail reflection */ }
            }

            enums.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

            _cachedEnumTypes     = enums;
            _cachedTypeFullNames = enums.Select(t => t.FullName).ToArray();
            _cachedTypeNames     = enums.Select(t => t.Name).ToArray();
        }

        // ── PropertyDrawer ─────────────────────────────────────────────────────────

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EnsureCache();

            SerializedProperty typeProp  = property.FindPropertyRelative("enumTypeName");
            SerializedProperty valueProp = property.FindPropertyRelative("enumValue");

            EditorGUI.BeginProperty(position, label, property);

            // Label
            Rect labelRect   = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            Rect controlRect = new Rect(position.x + EditorGUIUtility.labelWidth,
                                        position.y,
                                        position.width - EditorGUIUtility.labelWidth,
                                        position.height);

            EditorGUI.LabelField(labelRect, label);

            float halfW = controlRect.width * 0.45f;
            float gap   = controlRect.width * 0.02f;

            Rect typeRect  = new Rect(controlRect.x,                  controlRect.y, halfW, controlRect.height);
            Rect valueRect = new Rect(controlRect.x + halfW + gap,     controlRect.y, controlRect.width - halfW - gap, controlRect.height);

            // ── Type popup ──────────────────────────────────────────────────────────
            string currentTypeName = typeProp.stringValue;
            int currentTypeIdx = Array.IndexOf(_cachedTypeFullNames, currentTypeName);

            string[] displayNames = PrependNone(_cachedTypeNames);
            // currentTypeIdx is -1 when not set → show (none) at popup index 0.
            // Otherwise offset by 1 to account for the prepended (none) entry.
            int popupIdx = currentTypeIdx < 0 ? 0 : currentTypeIdx + 1;

            EditorGUI.BeginChangeCheck();
            int newPopupIdx = EditorGUI.Popup(typeRect, popupIdx, displayNames);
            if (EditorGUI.EndChangeCheck())
            {
                if (newPopupIdx == 0)
                {
                    typeProp.stringValue = string.Empty;
                    valueProp.intValue = 0;
                }
                else
                {
                    typeProp.stringValue = _cachedTypeFullNames[newPopupIdx - 1];
                    valueProp.intValue = 0;
                }
            }

            // ── Value popup (only if type is selected) ──────────────────────────────
            string resolvedTypeName = typeProp.stringValue;
            Type enumType = null;
            if (!string.IsNullOrEmpty(resolvedTypeName))
                enumType = _cachedEnumTypes.Find(t => t.FullName == resolvedTypeName);

            if (enumType != null)
            {
                string[] memberNames  = Enum.GetNames(enumType);
                Array    memberValues = Enum.GetValues(enumType);

                // Find current index
                int curValIdx = 0;
                int curVal = valueProp.intValue;
                for (int i = 0; i < memberValues.Length; i++)
                {
                    if ((int)memberValues.GetValue(i) == curVal) { curValIdx = i; break; }
                }

                EditorGUI.BeginChangeCheck();
                int newValIdx = EditorGUI.Popup(valueRect, curValIdx, memberNames);
                if (EditorGUI.EndChangeCheck())
                    valueProp.intValue = (int)memberValues.GetValue(newValIdx);
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUI.Popup(valueRect, 0, new[] { "— pick type first —" });
            }

            EditorGUI.EndProperty();
        }

        private static string[] PrependNone(string[] names)
        {
            var result = new string[names.Length + 1];
            result[0] = "(none)";
            names.CopyTo(result, 1);
            return result;
        }
    }
}
#endif

