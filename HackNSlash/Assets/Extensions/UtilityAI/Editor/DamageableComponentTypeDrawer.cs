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
     * Property drawer for fields marked with <see cref="DamageableComponentTypeAttribute"/>.
     * Renders as a dropdown populated with all concrete <see cref="IDamageableComponent"/>
     * types discovered in the loaded assemblies, showing short names in the UI while
     * storing the assembly-qualified name for robust runtime resolution.
     * </summary>
     */
    [CustomPropertyDrawer(typeof(DamageableComponentTypeAttribute))]
    public sealed class DamageableComponentTypeDrawer : PropertyDrawer
    {
        // Cache discovered types so reflection only happens once per editor session.
        private static List<Type> _types;
        private static string[] _displayNames;
        private static string[] _assemblyQualifiedNames;

        private static void EnsureCache()
        {
            if (_types != null) return;
            _types = new List<Type>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var t in asm.GetTypes())
                    {
                        if (t.IsAbstract || t.IsInterface) continue;
                        if (!typeof(IDamageableComponent).IsAssignableFrom(t)) continue;
                        _types.Add(t);
                    }
                }
                catch { /* skip assemblies that cannot be reflected */ }
            }
            _types.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

            _displayNames          = new string[_types.Count + 1];
            _assemblyQualifiedNames = new string[_types.Count + 1];
            _displayNames[0]          = "(None)";
            _assemblyQualifiedNames[0] = string.Empty;
            for (int i = 0; i < _types.Count; i++)
            {
                _displayNames[i + 1]           = _types[i].Name;
                _assemblyQualifiedNames[i + 1] = _types[i].AssemblyQualifiedName;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.HelpBox(position, "[DamageableComponentType] requires a string field.", MessageType.Error);
                return;
            }

            EnsureCache();

            string current = property.stringValue;
            int selectedIndex = 0;
            for (int i = 1; i < _assemblyQualifiedNames.Length; i++)
            {
                if (_assemblyQualifiedNames[i] == current) { selectedIndex = i; break; }
            }

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(position, label.text, selectedIndex, _displayNames);
            if (EditorGUI.EndChangeCheck())
            {
                property.stringValue = newIndex == 0 ? string.Empty : _assemblyQualifiedNames[newIndex];
            }
        }

        /** <summary>Clears the cache so it rebuilds on next domain reload.</summary> */
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void ClearCache() { _types = null; _displayNames = null; _assemblyQualifiedNames = null; }
    }
}
#endif

