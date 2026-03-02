#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/** <summary>
 * Scene-view editor for <see cref="ArenaBoundary"/> that provides draggable
 * handles for each polygon point and toolbar buttons to insert or remove points.
 * </summary>
 * <remarks>
 * Interaction:
 * <list type="bullet">
 *   <item>Drag a yellow sphere handle to reposition a boundary point.</item>
 *   <item>Click "Add Point" to append a new point after the last one.</item>
 *   <item>Click a point's "✕" label in the Inspector list to remove it.</item>
 *   <item>Click "Rebuild Walls" after editing to regenerate wall colliders in Play mode.</item>
 * </list>
 * All edits are Undo-registered.
 * </remarks>
 */
[CustomEditor(typeof(ArenaBoundary))]
public class ArenaBoundaryEditor : Editor
{
    private ArenaBoundary _boundary;
    private int _selectedIndex = -1;

    private void OnEnable()
    {
        _boundary = (ArenaBoundary) target;
    }

    #region Inspector GUI

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw standard fields except points.
        EditorGUILayout.PropertyField(serializedObject.FindProperty("config"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("wallMaterial"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("shaderDistanceProperty"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("trackedTransforms"), true);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Boundary Points", EditorStyles.boldLabel);

        List<Vector3> pts = _boundary.points;

        for (int i = 0; i < pts.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            // Highlight selected.
            if (i == _selectedIndex)
                GUI.backgroundColor = new Color(1f, 0.85f, 0.3f);

            pts[i] = EditorGUILayout.Vector3Field($"  [{i}]", pts[i]);

            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("✕", GUILayout.Width(24)))
            {
                Undo.RecordObject(_boundary, "Remove Boundary Point");
                pts.RemoveAt(i);
                if (_selectedIndex >= pts.Count) _selectedIndex = pts.Count - 1;
                EditorUtility.SetDirty(_boundary);
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Add Point"))
        {
            Undo.RecordObject(_boundary, "Add Boundary Point");
            Vector3 last = pts.Count > 0 ? pts[pts.Count - 1] + Vector3.right * 2f : Vector3.zero;
            pts.Add(last);
            _selectedIndex = pts.Count - 1;
            EditorUtility.SetDirty(_boundary);
        }

        if (Application.isPlaying && GUILayout.Button("Rebuild Walls"))
        {
            _boundary.BuildWalls();
        }

        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();

        // Repaint scene view while inspector is open.
        SceneView.RepaintAll();
    }

    #endregion

    #region Scene GUI

    private void OnSceneGUI()
    {
        if (_boundary == null) return;

        List<Vector3> pts = _boundary.points;
        if (pts == null || pts.Count == 0) return;

        // Draw lines between points.
        Handles.color = new Color(1f, 0.55f, 0f, 0.8f);
        int n = pts.Count;
        for (int i = 0; i < n; i++)
        {
            Handles.DrawLine(pts[i], pts[(i + 1) % n], 2f);
        }

        // Draggable sphere handles.
        for (int i = 0; i < pts.Count; i++)
        {
            Handles.color = (i == _selectedIndex) ? Color.yellow : new Color(1f, 0.9f, 0.2f, 0.9f);

            float handleSize = HandleUtility.GetHandleSize(pts[i]) * 0.12f;

            // Clicking a handle selects it.
            if (Handles.Button(pts[i], Quaternion.identity, handleSize, handleSize * 1.5f, Handles.SphereHandleCap))
            {
                _selectedIndex = i;
                Repaint();
            }

            if (i == _selectedIndex)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newPos = Handles.PositionHandle(pts[i], Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_boundary, "Move Boundary Point");
                    pts[i] = newPos;
                    EditorUtility.SetDirty(_boundary);
                }
            }

            // Point index label.
            Handles.Label(pts[i] + Vector3.up * 0.35f, $"{i}", EditorStyles.boldLabel);
        }
    }

    #endregion
}
#endif

