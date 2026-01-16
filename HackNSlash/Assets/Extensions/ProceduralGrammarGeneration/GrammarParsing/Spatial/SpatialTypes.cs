using System;
using System.Collections.Generic;
using System.Linq;

namespace ProceduralGrammarGeneration.GrammarParsing
{
    /// <summary>
    /// Base interface for all spatial data types used in grammar generation.
    /// All spatial types are Unity-independent and can be serialized/deserialized.
    /// </summary>
    public interface ISpatialData
    {
        string TypeName { get; }
    }

    /// <summary>
    /// Represents a 3D point in space.
    /// </summary>
    public struct Vector3D
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector3D(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3D Zero => new Vector3D(0, 0, 0);
        public static Vector3D One => new Vector3D(1, 1, 1);

        public float Magnitude => (float)Math.Sqrt(X * X + Y * Y + Z * Z);

        public Vector3D Normalized
        {
            get
            {
                float mag = Magnitude;
                return mag > 0 ? new Vector3D(X / mag, Y / mag, Z / mag) : Zero;
            }
        }

        public static Vector3D operator +(Vector3D a, Vector3D b) => new Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3D operator -(Vector3D a, Vector3D b) => new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3D operator *(Vector3D a, float scalar) => new Vector3D(a.X * scalar, a.Y * scalar, a.Z * scalar);
        public static Vector3D operator /(Vector3D a, float scalar) => new Vector3D(a.X / scalar, a.Y / scalar, a.Z / scalar);

        public static float Dot(Vector3D a, Vector3D b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        public static Vector3D Lerp(Vector3D a, Vector3D b, float t) => a + (b - a) * t;

        public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2})";
    }

    /// <summary>
    /// Represents a 3D spline curve defined by control points.
    /// Used for paths, building traces, tree trunks, etc.
    /// </summary>
    public class SplineCurve : ISpatialData
    {
        public string TypeName => "SplineCurve";

        /// <summary>
        /// Control points defining the spline.
        /// </summary>
        public List<Vector3D> ControlPoints { get; set; }

        /// <summary>
        /// Whether the spline forms a closed loop.
        /// </summary>
        public bool IsClosed { get; set; }

        /// <summary>
        /// Total length of the spline (approximated).
        /// </summary>
        public float Length { get; private set; }

        public SplineCurve(List<Vector3D> controlPoints, bool isClosed = false)
        {
            ControlPoints = controlPoints ?? throw new ArgumentNullException(nameof(controlPoints));
            IsClosed = isClosed;
            CalculateLength();
        }

        /// <summary>
        /// Evaluates the spline at normalized parameter t (0 to 1).
        /// Uses Catmull-Rom interpolation for smooth curves.
        /// </summary>
        public Vector3D Evaluate(float t)
        {
            if (ControlPoints.Count == 0)
                return Vector3D.Zero;
            if (ControlPoints.Count == 1)
                return ControlPoints[0];

            t = Math.Clamp(t, 0f, 1f);

            float scaledT = t * (ControlPoints.Count - 1);
            int segment = Math.Min((int)scaledT, ControlPoints.Count - 2);
            float localT = scaledT - segment;

            return CatmullRom(
                GetPoint(segment - 1),
                GetPoint(segment),
                GetPoint(segment + 1),
                GetPoint(segment + 2),
                localT
            );
        }

        /// <summary>
        /// Gets the tangent (direction) at normalized parameter t.
        /// </summary>
        public Vector3D GetTangent(float t)
        {
            float delta = 0.001f;
            Vector3D p1 = Evaluate(Math.Max(0, t - delta));
            Vector3D p2 = Evaluate(Math.Min(1, t + delta));
            return (p2 - p1).Normalized;
        }

        /// <summary>
        /// Gets a point at a specific distance along the spline.
        /// </summary>
        public Vector3D EvaluateByDistance(float distance)
        {
            if (Length <= 0) return ControlPoints.FirstOrDefault();
            return Evaluate(distance / Length);
        }

        private Vector3D GetPoint(int index)
        {
            if (ControlPoints.Count == 0) return Vector3D.Zero;

            if (IsClosed)
            {
                index = ((index % ControlPoints.Count) + ControlPoints.Count) % ControlPoints.Count;
            }
            else
            {
                index = Math.Clamp(index, 0, ControlPoints.Count - 1);
            }

            return ControlPoints[index];
        }

        private static Vector3D CatmullRom(Vector3D p0, Vector3D p1, Vector3D p2, Vector3D p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return new Vector3D(
                0.5f * ((2f * p1.X) +
                       (-p0.X + p2.X) * t +
                       (2f * p0.X - 5f * p1.X + 4f * p2.X - p3.X) * t2 +
                       (-p0.X + 3f * p1.X - 3f * p2.X + p3.X) * t3),
                0.5f * ((2f * p1.Y) +
                       (-p0.Y + p2.Y) * t +
                       (2f * p0.Y - 5f * p1.Y + 4f * p2.Y - p3.Y) * t2 +
                       (-p0.Y + 3f * p1.Y - 3f * p2.Y + p3.Y) * t3),
                0.5f * ((2f * p1.Z) +
                       (-p0.Z + p2.Z) * t +
                       (2f * p0.Z - 5f * p1.Z + 4f * p2.Z - p3.Z) * t2 +
                       (-p0.Z + 3f * p1.Z - 3f * p2.Z + p3.Z) * t3)
            );
        }

        private void CalculateLength()
        {
            Length = 0f;
            if (ControlPoints.Count < 2) return;

            const int samples = 100;
            Vector3D prev = Evaluate(0);

            for (int i = 1; i <= samples; i++)
            {
                float t = i / (float)samples;
                Vector3D current = Evaluate(t);
                Length += (current - prev).Magnitude;
                prev = current;
            }
        }
    }

    /// <summary>
    /// Represents a 2D heightmap for height variation along surfaces.
    /// Used for building height profiles, terrain elevation, etc.
    /// </summary>
    public class Heightmap : ISpatialData
    {
        public string TypeName => "Heightmap";

        /// <summary>
        /// Width of the heightmap (number of columns).
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// Height of the heightmap (number of rows).
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// Height values stored in row-major order.
        /// </summary>
        private float[] _values;

        /// <summary>
        /// World-space size of the heightmap.
        /// </summary>
        public Vector3D WorldSize { get; set; }

        public Heightmap(int width, int height, Vector3D worldSize)
        {
            Width = width;
            Height = height;
            WorldSize = worldSize;
            _values = new float[width * height];
        }

        public Heightmap(int width, int height, float[] values, Vector3D worldSize)
        {
            Width = width;
            Height = height;
            WorldSize = worldSize;
            _values = values ?? throw new ArgumentNullException(nameof(values));

            if (_values.Length != width * height)
                throw new ArgumentException($"Values array length ({_values.Length}) must match width*height ({width * height})");
        }

        /// <summary>
        /// Samples the heightmap at normalized UV coordinates (0-1, 0-1).
        /// Uses bilinear interpolation for smooth results.
        /// </summary>
        public float Sample(float u, float v)
        {
            u = Math.Clamp(u, 0f, 1f);
            v = Math.Clamp(v, 0f, 1f);

            float x = u * (Width - 1);
            float y = v * (Height - 1);

            int x0 = (int)Math.Floor(x);
            int y0 = (int)Math.Floor(y);
            int x1 = Math.Min(x0 + 1, Width - 1);
            int y1 = Math.Min(y0 + 1, Height - 1);

            float fx = x - x0;
            float fy = y - y0;

            float h00 = GetValue(x0, y0);
            float h10 = GetValue(x1, y0);
            float h01 = GetValue(x0, y1);
            float h11 = GetValue(x1, y1);

            float h0 = h00 * (1 - fx) + h10 * fx;
            float h1 = h01 * (1 - fx) + h11 * fx;

            return h0 * (1 - fy) + h1 * fy;
        }

        /// <summary>
        /// Samples at world-space position.
        /// </summary>
        public float SampleWorld(float x, float z)
        {
            float u = x / WorldSize.X;
            float v = z / WorldSize.Z;
            return Sample(u, v);
        }

        public void SetValue(int x, int y, float value)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                throw new ArgumentOutOfRangeException($"Position ({x},{y}) out of bounds");

            _values[y * Width + x] = value;
        }

        public float GetValue(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return 0f;

            return _values[y * Width + x];
        }

        public float[] GetValues() => (float[])_values.Clone();
    }

    /// <summary>
    /// Represents a scattered collection of points in 3D space.
    /// Used for placement points, vegetation distribution, etc.
    /// </summary>
    public class PointField : ISpatialData
    {
        public string TypeName => "PointField";

        /// <summary>
        /// Collection of points in the field.
        /// </summary>
        public List<Vector3D> Points { get; set; }

        /// <summary>
        /// Optional metadata for each point (e.g., scale, rotation, type).
        /// </summary>
        public List<Dictionary<string, object>> Metadata { get; set; }

        public PointField(List<Vector3D> points)
        {
            Points = points ?? throw new ArgumentNullException(nameof(points));
            Metadata = new List<Dictionary<string, object>>();
            for (int i = 0; i < points.Count; i++)
                Metadata.Add(new Dictionary<string, object>());
        }

        /// <summary>
        /// Finds the nearest point to a given position.
        /// </summary>
        public int FindNearest(Vector3D position)
        {
            if (Points.Count == 0) return -1;

            int nearest = 0;
            float minDist = (Points[0] - position).Magnitude;

            for (int i = 1; i < Points.Count; i++)
            {
                float dist = (Points[i] - position).Magnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = i;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Gets all points within a radius of a position.
        /// </summary>
        public List<int> FindInRadius(Vector3D position, float radius)
        {
            var result = new List<int>();
            float radiusSq = radius * radius;

            for (int i = 0; i < Points.Count; i++)
            {
                Vector3D delta = Points[i] - position;
                float distSq = delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z;
                if (distSq <= radiusSq)
                    result.Add(i);
            }

            return result;
        }
    }

    /// <summary>
    /// Represents a constant spatial value (for parameters that don't vary).
    /// </summary>
    public class SpatialConstant : ISpatialData
    {
        public string TypeName => "SpatialConstant";

        public object Value { get; set; }

        public SpatialConstant(object value)
        {
            Value = value;
        }

        public T GetValue<T>() => (T)Value;
    }
}
