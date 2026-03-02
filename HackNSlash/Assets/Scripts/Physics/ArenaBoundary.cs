using System.Collections.Generic;
using UnityEngine;

/** <summary>
 * Defines a closed polygon boundary for a gameplay arena.
 * Invisible wall colliders are generated at runtime from the polygon points,
 * and a proximity shader effect is triggered on nearby renderers when a
 * tracked transform enters the fade zone.
 * </summary>
 * <remarks>
 * Edit the polygon in Scene View using the custom editor gizmo handles.
 * Each pair of adjacent points produces one thin <see cref="BoxCollider"/>
 * wall segment.  The shader property name set in <see cref="shaderDistanceProperty"/>
 * must exist on the boundary-wall material for the proximity fade to work.
 *
 * Tracked objects (player, enemies) are registered via <see cref="RegisterTracked"/>
 * and automatically update the shader each frame.
 * </remarks>
 */
public class ArenaBoundary : MonoBehaviour
{
    #region Inspector

    [Tooltip("Shared physics tuning parameters (used for wall height, thickness and shader distance).")]
    public PhysicsConfig config;

    [Tooltip("World-space XZ polygon points that define the boundary outline. " +
             "The polygon is automatically closed between the last and first point.")]
    public List<Vector3> points = new List<Vector3>();

    [Tooltip("Material applied to the invisible wall colliders. " +
             "Must expose the property named in shaderDistanceProperty.")]
    public Material wallMaterial;

    [Tooltip("Name of the float shader property driven by proximity to the nearest tracked transform.")]
    public string shaderDistanceProperty = "_ProximityDistance";

    [Tooltip("Additional GameObjects (player, enemies) whose proximity drives the wall shader. " +
             "Populated automatically via RegisterTracked but can also be pre-assigned here.")]
    public List<Transform> trackedTransforms = new List<Transform>();

    #endregion

    // Cached wall segments and renderers.
    private readonly List<GameObject> _walls = new();
    private readonly List<(Renderer rend, MaterialPropertyBlock block)> _wallRenderers = new();

    #region MonoBehaviour Callbacks

    private void Awake()
    {
        BuildWalls();
    }

    private void Update()
    {
        if (_wallRenderers.Count == 0 || trackedTransforms.Count == 0) return;
        if (config == null) return;

        UpdateProximityShader();
    }

    private void OnDestroy()
    {
        foreach (var wall in _walls)
        {
            if (wall != null) Destroy(wall);
        }
        _walls.Clear();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        DrawGizmos(false);
    }

    private void OnDrawGizmosSelected()
    {
        DrawGizmos(true);
    }
#endif

    #endregion

    #region Public API

    /** <summary>
     * Registers a transform so that its proximity to the boundary wall
     * continuously drives the proximity shader effect.
     * </summary>
     */
    public void RegisterTracked(Transform t)
    {
        if (t != null && !trackedTransforms.Contains(t))
            trackedTransforms.Add(t);
    }

    /** <summary>
     * Removes a previously registered transform from proximity tracking.
     * </summary>
     */
    public void UnregisterTracked(Transform t)
    {
        trackedTransforms.Remove(t);
    }

    /** <summary>
     * Rebuilds wall colliders from the current <see cref="points"/> list.
     * Call this in Editor if you modify points at runtime or via script.
     * </summary>
     */
    public void BuildWalls()
    {
        foreach (var wall in _walls)
        {
            if (wall != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(wall);
                else
#endif
                    Destroy(wall);
            }
        }

        _walls.Clear();
        _wallRenderers.Clear();

        if (points == null || points.Count < 2) return;
        if (config == null) return;

        int n = points.Count;
        for (int i = 0; i < n; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[(i + 1) % n];

            CreateWallSegment(a, b);
        }
    }

    #endregion

    #region Wall Construction

    /** <summary>
     * Creates a single invisible wall <see cref="BoxCollider"/> and optional
     * renderer between two boundary points.
     * </summary>
     */
    private void CreateWallSegment(Vector3 a, Vector3 b)
    {
        Vector3 mid = (a + b) * 0.5f;
        mid.y = transform.position.y + config.boundaryWallHeight * 0.5f;

        Vector3 dir = (b - a);
        dir.y = 0f;
        float length = dir.magnitude;
        if (length < 0.001f) return;

        Quaternion rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

        var wallGO = new GameObject("BoundaryWall")
        {
            layer = gameObject.layer
        };
        wallGO.transform.SetParent(transform, false);
        wallGO.transform.position = mid;
        wallGO.transform.rotation = rotation;

        var col = wallGO.AddComponent<BoxCollider>();
        col.size = new Vector3(config.boundaryWallThickness, config.boundaryWallHeight, length);
        col.isTrigger = false;

        if (wallMaterial != null)
        {
            var rend = wallGO.AddComponent<MeshRenderer>();
            var filter = wallGO.AddComponent<MeshFilter>();
            filter.sharedMesh = CreateWallMesh(length);
            rend.sharedMaterial = wallMaterial;

            var block = new MaterialPropertyBlock();
            _wallRenderers.Add((rend, block));
        }

        _walls.Add(wallGO);
    }

    /** <summary>
     * Creates a simple quad mesh for the wall face so the shader effect is visible.
     * </summary>
     */
    private static Mesh CreateWallMesh(float length)
    {
        var mesh = new Mesh { name = "BoundaryWallMesh" };
        // A simple quad facing outward (–Z in local space).
        float h = 1f; // Unit height; scaled via transform.
        mesh.vertices = new[]
        {
            new Vector3(-length * 0.5f, 0f, 0f),
            new Vector3( length * 0.5f, 0f, 0f),
            new Vector3( length * 0.5f, h,  0f),
            new Vector3(-length * 0.5f, h,  0f),
        };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateNormals();
        return mesh;
    }

    #endregion

    #region Proximity Shader

    /** <summary>
     * Each frame finds the minimum distance from any tracked transform to any
     * wall renderer and pushes that value into the wall material's shader property
     * via a <see cref="MaterialPropertyBlock"/>.
     * </summary>
     */
    private void UpdateProximityShader()
    {
        int propID = Shader.PropertyToID(shaderDistanceProperty);

        foreach (var (rend, block) in _wallRenderers)
        {
            Vector3 wallPos = rend.transform.position;
            float minDist = float.MaxValue;

            foreach (var t in trackedTransforms)
            {
                if (t == null) continue;
                float d = Vector3.Distance(new Vector3(t.position.x, wallPos.y, t.position.z), wallPos);
                if (d < minDist) minDist = d;
            }

            // Normalise distance so 0 = at the wall, 1 = at or beyond the fade start.
            float normalised = Mathf.Clamp01(minDist / config.boundaryProximityShaderDistance);
            block.SetFloat(propID, normalised);
            rend.SetPropertyBlock(block);
        }
    }

    #endregion

    #region Gizmo Helpers

#if UNITY_EDITOR
    private void DrawGizmos(bool selected)
    {
        if (points == null || points.Count < 2) return;

        Gizmos.color = selected ? new Color(1f, 0.5f, 0f, 0.9f) : new Color(1f, 0.5f, 0f, 0.35f);

        int n = points.Count;
        float wallH = config != null ? config.boundaryWallHeight : 4f;

        for (int i = 0; i < n; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[(i + 1) % n];

            // Ground line.
            Gizmos.DrawLine(a, b);

            // Vertical edges at each point.
            Gizmos.DrawLine(a, a + Vector3.up * wallH);
            Gizmos.DrawLine(b, b + Vector3.up * wallH);

            // Top line.
            Gizmos.DrawLine(a + Vector3.up * wallH, b + Vector3.up * wallH);
        }

        // Draw handles for each point.
        Gizmos.color = selected ? Color.yellow : new Color(1f, 1f, 0f, 0.4f);
        foreach (var p in points)
        {
            Gizmos.DrawSphere(p, 0.12f);
        }
    }
#endif

    #endregion
}


