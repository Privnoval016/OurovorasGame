using System;
using System.Collections.Generic;
using UnityEngine.Splines;
using ProceduralGrammarGeneration.GrammarParsing;

namespace ProceduralGrammarGeneration.Editor
{
    /// <summary>
    /// Unity-specific implementation of ISpatialDataConverter.
    /// Converts Unity Splines, Terrains, and other spatial data to grammar-native types.
    /// This class uses conditional compilation to only include Unity code when in Unity.
    /// </summary>
    public class UnitySpatialConverter : ISpatialDataConverter
    {
        public string ConverterName => "Unity";

        /// <summary>
        /// Converts Unity Spline to grammar-native SplineCurve.
        /// Supports both Unity.Splines and legacy AnimationCurve.
        /// </summary>
        public SplineCurve ConvertSpline(object unitySpline)
        {
            if (unitySpline == null)
                throw new ArgumentNullException(nameof(unitySpline));

#if UNITY_EDITOR || UNITY_STANDALONE
            // Unity 2022+ Spline system
            if (unitySpline is Spline spline)
            {
                var controlPoints = new List<Vector3D>();
                
                // Sample the spline at regular intervals
                int sampleCount = Math.Max(spline.Count * 4, 20); // 4 samples per knot minimum
                for (int i = 0; i < sampleCount; i++)
                {
                    float t = i / (float)(sampleCount - 1);
                    var pos = spline.EvaluatePosition(t);
                    controlPoints.Add(new Vector3D(pos.x, pos.y, pos.z));
                }

                return new SplineCurve(controlPoints, spline.Closed);
            }
            // Legacy AnimationCurve (1D)
            else if (unitySpline is UnityEngine.AnimationCurve curve)
            {
                var controlPoints = new List<Vector3D>();
                
                if (curve.keys.Length > 0)
                {
                    float minTime = curve.keys[0].time;
                    float maxTime = curve.keys[curve.keys.Length - 1].time;
                    int sampleCount = Math.Max(curve.keys.Length * 4, 20);

                    for (int i = 0; i < sampleCount; i++)
                    {
                        float t = UnityEngine.Mathf.Lerp(minTime, maxTime, i / (float)(sampleCount - 1));
                        float value = curve.Evaluate(t);
                        controlPoints.Add(new Vector3D(t, value, 0));
                    }
                }

                return new SplineCurve(controlPoints, false);
            }
            // Unity Transform array (manual path definition)
            else if (unitySpline is UnityEngine.Transform[] transforms)
            {
                var controlPoints = new List<Vector3D>();
                foreach (var transform in transforms)
                {
                    var pos = transform.position;
                    controlPoints.Add(new Vector3D(pos.x, pos.y, pos.z));
                }
                return new SplineCurve(controlPoints, false);
            }
            else
            {
                throw new ArgumentException($"Unsupported Unity spline type: {unitySpline.GetType().Name}");
            }
#else
            throw new NotSupportedException("UnitySpatialConverter requires Unity runtime");
#endif
        }

        /// <summary>
        /// Converts Unity Terrain or Texture2D to grammar-native Heightmap.
        /// </summary>
        public Heightmap ConvertHeightmap(object unityHeightData, int width, int height, Vector3D worldSize)
        {
            if (unityHeightData == null)
                throw new ArgumentNullException(nameof(unityHeightData));

#if UNITY_EDITOR || UNITY_STANDALONE
            // Unity Terrain heightmap
            if (unityHeightData is UnityEngine.Terrain terrain)
            {
                var terrainData = terrain.terrainData;
                int heightmapWidth = terrainData.heightmapResolution;
                int heightmapHeight = terrainData.heightmapResolution;
                float[,] heights = terrainData.GetHeights(0, 0, heightmapWidth, heightmapHeight);

                // Resample if requested size differs
                if (width != heightmapWidth || height != heightmapHeight)
                {
                    heights = ResampleHeights(heights, heightmapWidth, heightmapHeight, width, height);
                }

                // Flatten 2D array to 1D
                var values = new float[width * height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        values[y * width + x] = heights[y, x] * terrainData.size.y; // Scale by terrain height
                    }
                }

                var size = new Vector3D(terrainData.size.x, terrainData.size.y, terrainData.size.z);
                return new Heightmap(width, height, values, size);
            }
            // Unity Texture2D (grayscale heightmap)
            else if (unityHeightData is UnityEngine.Texture2D texture)
            {
                var values = new float[width * height];
                
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float u = x / (float)(width - 1);
                        float v = y / (float)(height - 1);
                        
                        var color = texture.GetPixelBilinear(u, v);
                        values[y * width + x] = color.grayscale * worldSize.Y; // Use grayscale value as height
                    }
                }

                return new Heightmap(width, height, values, worldSize);
            }
            else
            {
                throw new ArgumentException($"Unsupported Unity height data type: {unityHeightData.GetType().Name}");
            }
#else
            throw new NotSupportedException("UnitySpatialConverter requires Unity runtime");
#endif
        }

        /// <summary>
        /// Converts Unity point data (Transforms, ParticleSystem, etc.) to grammar-native PointField.
        /// </summary>
        public PointField ConvertPointField(object unityPointData)
        {
            if (unityPointData == null)
                throw new ArgumentNullException(nameof(unityPointData));

#if UNITY_EDITOR || UNITY_STANDALONE
            // Transform array
            if (unityPointData is UnityEngine.Transform[] transforms)
            {
                var points = new List<Vector3D>();
                foreach (var transform in transforms)
                {
                    var pos = transform.position;
                    points.Add(new Vector3D(pos.x, pos.y, pos.z));
                }
                return new PointField(points);
            }
            // GameObject array
            else if (unityPointData is UnityEngine.GameObject[] gameObjects)
            {
                var points = new List<Vector3D>();
                foreach (var go in gameObjects)
                {
                    var pos = go.transform.position;
                    points.Add(new Vector3D(pos.x, pos.y, pos.z));
                }
                return new PointField(points);
            }
            // Vector3 array
            else if (unityPointData is UnityEngine.Vector3[] vectors)
            {
                var points = new List<Vector3D>();
                foreach (var v in vectors)
                {
                    points.Add(new Vector3D(v.x, v.y, v.z));
                }
                return new PointField(points);
            }
            // ParticleSystem
            else if (unityPointData is UnityEngine.ParticleSystem particleSystem)
            {
                var particles = new UnityEngine.ParticleSystem.Particle[particleSystem.particleCount];
                particleSystem.GetParticles(particles);

                var points = new List<Vector3D>();
                foreach (var particle in particles)
                {
                    var pos = particle.position;
                    points.Add(new Vector3D(pos.x, pos.y, pos.z));
                }
                return new PointField(points);
            }
            else
            {
                throw new ArgumentException($"Unsupported Unity point data type: {unityPointData.GetType().Name}");
            }
#else
            throw new NotSupportedException("UnitySpatialConverter requires Unity runtime");
#endif
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        private float[,] ResampleHeights(float[,] source, int srcWidth, int srcHeight, int dstWidth, int dstHeight)
        {
            var result = new float[dstHeight, dstWidth];

            for (int y = 0; y < dstHeight; y++)
            {
                for (int x = 0; x < dstWidth; x++)
                {
                    float u = x / (float)(dstWidth - 1);
                    float v = y / (float)(dstHeight - 1);

                    float srcX = u * (srcWidth - 1);
                    float srcY = v * (srcHeight - 1);

                    int x0 = (int)srcX;
                    int y0 = (int)srcY;
                    int x1 = UnityEngine.Mathf.Min(x0 + 1, srcWidth - 1);
                    int y1 = UnityEngine.Mathf.Min(y0 + 1, srcHeight - 1);

                    float fx = srcX - x0;
                    float fy = srcY - y0;

                    float h00 = source[y0, x0];
                    float h10 = source[y0, x1];
                    float h01 = source[y1, x0];
                    float h11 = source[y1, x1];

                    float h0 = UnityEngine.Mathf.Lerp(h00, h10, fx);
                    float h1 = UnityEngine.Mathf.Lerp(h01, h11, fx);

                    result[y, x] = UnityEngine.Mathf.Lerp(h0, h1, fy);
                }
            }

            return result;
        }
#endif
    }
}
