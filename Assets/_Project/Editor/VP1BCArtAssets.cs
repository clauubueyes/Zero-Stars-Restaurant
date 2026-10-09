using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ZeroStarRestaurant.Editor
{
    // Deterministic, locally authored textures and modest mesh placeholders; no downloaded assets.
    public static class VP1BCArtAssets
    {
        public const string Folder = "Assets/_Project/Art/Restaurant";
        public static void Prepare()
        {
            Directory.CreateDirectory(Folder + "/Materials"); Directory.CreateDirectory(Folder + "/Textures");
            Directory.CreateDirectory(Folder + "/Meshes"); AssetDatabase.Refresh();
        }
        public static Material Material(string name, Color color, string pattern, float metallic, float smoothness)
        {
            string path = Folder + "/Materials/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path); if (existing != null) return existing;
            string prefix = Folder + "/Textures/" + pattern;
            if (!File.Exists(prefix + "_Albedo.png")) Textures(prefix, pattern);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, enableInstancing = true };
            material.SetColor("_BaseColor", color); material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", smoothness);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(prefix + "_Albedo.png"));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(prefix + "_Normal.png"));
            material.SetFloat("_BumpScale", pattern == "Steel" ? .08f : .12f); material.EnableKeyword("_NORMALMAP");
            // Alpha is a spatial smoothness multiplier. Metallic remains the material's measured scalar.
            material.SetTexture("_OcclusionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(prefix + "_Surface.png"));
            material.SetFloat("_OcclusionStrength", .15f); material.EnableKeyword("_OCCLUSIONMAP");
            string packedPath = Folder + "/Textures/" + name + "_MetalSmooth.png";
            if (!File.Exists(packedPath))
            {
                var packed = new Texture2D(512, 512, TextureFormat.RGBA32, false, true);
                packed.LoadImage(File.ReadAllBytes(prefix + "_Surface.png")); var pixels = packed.GetPixels();
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(metallic, 0, 0, pixels[i].a);
                UnityEngine.Object.DestroyImmediate(packed); Write(packedPath, pixels, false, true);
            }
            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(packedPath));
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            AssetDatabase.CreateAsset(material, path); return material;
        }
        private static void Textures(string prefix, string pattern)
        {
            const int n = 512;
            var colors = new Color[n * n]; var heights = new float[n * n]; var surface = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float u = x / (float)n, v = y / (float)n;
                // Periodic value noise avoids seams and regular stripes; hash adds a small surface grain.
                float coarse = PeriodicNoise(u, v, 5, 37) * .65f + PeriodicNoise(u, v, 13, 91) * .35f;
                uint hash = unchecked((uint)(x * 374761393 + y * 668265263)); hash = (hash ^ (hash >> 13)) * 1274126177; hash ^= hash >> 16;
                float grain = (hash & 65535) / 65535f;
                float value = .84f + .05f * coarse + .055f * grain, height = .5f + .015f * grain;
                if (pattern == "Floor" || pattern == "Tile" || pattern == "Ceiling")
                {
                    int count = pattern == "Floor" ? 4 : pattern == "Tile" ? 5 : 2;
                    float a = Mathf.Repeat(u * count, 1), b = Mathf.Repeat(v * count, 1);
                    float seam = Mathf.Min(a, 1 - a, b, 1 - b);
                    float grout = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.007f, .022f, seam));
                    int ix = Mathf.FloorToInt(u * count), iy = Mathf.FloorToInt(v * count);
                    float variation = Mathf.Sin(ix * 39.37f + iy * 17.63f) * .055f;
                    value += variation; value = Mathf.Lerp(value, pattern == "Ceiling" ? .45f : .47f, grout);
                    height -= grout * .055f;
                    if (pattern == "Ceiling" && grain > .985f) { value *= .65f; height -= .08f; }
                }
                else if (pattern == "Steel")
                { value = .83f + .035f * Mathf.Sin(v * Mathf.PI * 230) + .05f * grain + .025f * coarse; height += Mathf.Sin(v * Mathf.PI * 230) * .008f; }
                else if (pattern == "Wood")
                { value = .78f + .065f * Mathf.Sin(u * Mathf.PI * 24 + Mathf.Sin(v * Mathf.PI * 2) * 2) + .035f * grain; height += (value - .8f) * .15f; }
                else if (pattern == "Meat")
                { value = .72f + .12f * coarse + .17f * grain; height += .065f * grain; }
                else if (pattern == "Cooked")
                {
                    float sear = Mathf.Pow(Mathf.Max(0, Mathf.Cos(u * Mathf.PI * 16)), 18);
                    value = .73f + .12f * coarse + .14f * grain - sear * .27f; height += grain * .065f;
                }
                else if (pattern == "Bread")
                { value = .8f + .09f * coarse + .10f * grain; if (grain > .996f) value = .56f; height += .035f * grain; }
                else if (pattern == "Griddle")
                { value = .78f + .05f * coarse + .11f * grain; height += .025f * grain; }
                colors[y * n + x] = new Color(value, value, value, 1); heights[y * n + x] = height;
                surface[y * n + x] = new Color(1, .86f + .12f * grain, 1, .7f + .2f * grain);
            }
            var normals = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float dx = heights[y * n + (x + 1) % n] - heights[y * n + (x + n - 1) % n];
                float dy = heights[((y + 1) % n) * n + x] - heights[((y + n - 1) % n) * n + x];
                Vector3 normal = new Vector3(-dx * 3, -dy * 3, 1).normalized;
                normals[y * n + x] = new Color(normal.x * .5f + .5f, normal.y * .5f + .5f, normal.z * .5f + .5f, 1);
            }
            Write(prefix + "_Albedo.png", colors, false, false); Write(prefix + "_Normal.png", normals, true, false);
            Write(prefix + "_Surface.png", surface, false, true);
        }
        private static float PeriodicNoise(float u, float v, int cells, uint salt)
        {
            float x = u * cells, y = v * cells; int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float tx = Mathf.SmoothStep(0, 1, x - ix), ty = Mathf.SmoothStep(0, 1, y - iy);
            float Value(int px, int py)
            {
                uint h = unchecked((uint)((px % cells) * 374761393 + (py % cells) * 668265263)) ^ salt;
                h = (h ^ (h >> 13)) * 1274126177; h ^= h >> 16;
                return (h & 65535) / 32767.5f - 1;
            }
            return Mathf.Lerp(Mathf.Lerp(Value(ix, iy), Value(ix + 1, iy), tx), Mathf.Lerp(Value(ix, iy + 1), Value(ix + 1, iy + 1), tx), ty);
        }
        private static void Write(string path, Color[] pixels, bool normal, bool linear)
        {
            var texture = new Texture2D(512, 512, TextureFormat.RGBA32, false, true); texture.SetPixels(pixels); texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !linear && !normal; importer.wrapMode = TextureWrapMode.Repeat; importer.mipmapEnabled = true;
            importer.anisoLevel = 4; importer.maxTextureSize = 512; importer.SaveAndReimport();
        }
        public static Mesh Box(Vector3 size)
        {
            string name = "Box_" + size.x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "_" +
                size.y.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "_" + size.z.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
            string path = Folder + "/Meshes/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (existing != null) return existing;
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            void Face(Vector3 normal, Vector3 right, Vector3 up, float width, float height, float depth)
            {
                int start = vertices.Count; Vector3 center = normal * depth * .5f;
                vertices.Add(center - right * width * .5f - up * height * .5f); vertices.Add(center + right * width * .5f - up * height * .5f);
                vertices.Add(center + right * width * .5f + up * height * .5f); vertices.Add(center - right * width * .5f + up * height * .5f);
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(width, 0)); uv.Add(new Vector2(width, height)); uv.Add(new Vector2(0, height));
                triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
            Face(Vector3.forward, Vector3.right, Vector3.up, size.x, size.y, size.z);
            Face(Vector3.back, Vector3.left, Vector3.up, size.x, size.y, size.z);
            Face(Vector3.right, Vector3.back, Vector3.up, size.z, size.y, size.x);
            Face(Vector3.left, Vector3.forward, Vector3.up, size.z, size.y, size.x);
            Face(Vector3.up, Vector3.right, Vector3.back, size.x, size.z, size.y);
            Face(Vector3.down, Vector3.right, Vector3.forward, size.x, size.z, size.y);
            var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
        public static Mesh FoodMesh(string kind)
        {
            string path = Folder + "/Meshes/" + kind + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (existing != null) return existing;
            Vector2[] profile = kind == "Bun" ? new[] { new Vector2(0, -.5f), new Vector2(.42f, -.5f), new Vector2(.49f, -.35f),
                new Vector2(.5f, -.05f), new Vector2(.46f, .18f), new Vector2(.34f, .38f), new Vector2(.18f, .48f), new Vector2(0, .5f) } :
                kind == "Plate" ? new[] { new Vector2(0, -.5f), new Vector2(.33f, -.5f), new Vector2(.48f, .1f), new Vector2(.5f, .42f),
                    new Vector2(.48f, .5f), new Vector2(.34f, -.05f), new Vector2(0, -.05f) } :
                    new[] { new Vector2(0, -.5f), new Vector2(.40f, -.5f), new Vector2(.485f, -.32f), new Vector2(.50f, .1f),
                        new Vector2(.46f, .38f), new Vector2(.35f, .47f), new Vector2(0, .5f) };
            const int segments = 48; var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            for (int row = 0; row < profile.Length; row++) for (int i = 0; i <= segments; i++)
            {
                float angle = i * 2 * Mathf.PI / segments;
                float radius = profile[row].x * (kind == "Patty" ? 1 - .015f * (1 + Mathf.Sin(angle * 7 + row)) : 1);
                var point = new Vector3(Mathf.Cos(angle) * radius, profile[row].y, Mathf.Sin(angle) * radius);
                vertices.Add(point); uv.Add(new Vector2(point.x + .5f, point.z + .5f));
                if (row == 0 || i == 0) continue;
                int current = row * (segments + 1) + i;
                triangles.AddRange(new[] { current, current - segments - 2, current - 1, current, current - segments - 1, current - segments - 2 });
            }
            var mesh = new Mesh { name = kind }; mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
    }
}
