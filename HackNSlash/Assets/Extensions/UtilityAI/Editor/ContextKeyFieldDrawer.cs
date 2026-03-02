#if UNITY_EDITOR
using System.Collections.Generic;
using Extensions.UtilityAI;
using UnityEditor;
using UnityEngine;

namespace Extensions.UtilityAI.Editor
{
    /** <summary>
     * Property drawer for <see cref="ContextKeyField"/>.
     * Renders a grouped dropdown populated by all <see cref="ContextKey"/> static fields
     * on <c>[AIContextKey]</c>-tagged types. No raw strings ever appear to the user.
     * </summary>
     */
    [CustomPropertyDrawer(typeof(ContextKeyField))]
    public class ContextKeyFieldDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var idProp = property.FindPropertyRelative("internalId");
            var displayProp = property.FindPropertyRelative("displayName");

            var allKeys = ContextKeyField.GetAllKeys();
            if (allKeys.Count == 0)
            {
                EditorGUI.LabelField(position, label.text, "(no [AIContextKey] classes found)");
                EditorGUI.EndProperty();
                return;
            }

            string currentId = idProp.stringValue;
            var options = new List<GUIContent> { new GUIContent("(none)") };
            var ids = new List<string> { string.Empty };
            var displays = new List<string> { "(none)" };
            int currentIndex = 0;

            foreach (var (key, display, category) in allKeys)
            {
                string prefix = string.IsNullOrEmpty(category) ? string.Empty : category + "/";
                options.Add(new GUIContent(prefix + display));
                ids.Add(key.InternalId);
                displays.Add(display);
                if (key.InternalId == currentId)
                    currentIndex = ids.Count - 1;
            }

            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            Rect popupRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y,
                position.width - EditorGUIUtility.labelWidth, position.height);

            EditorGUI.LabelField(labelRect, label);
            int newIndex = EditorGUI.Popup(popupRect, currentIndex, options.ToArray());

            if (newIndex != currentIndex)
            {
                idProp.stringValue = ids[newIndex];
                displayProp.stringValue = displays[newIndex];
            }

            EditorGUI.EndProperty();
        }
    }
}
#endif

