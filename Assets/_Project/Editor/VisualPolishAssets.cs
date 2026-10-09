using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZeroStarRestaurant.Editor
{
    public static class VisualPolishAssets
    {
        public const string Folder = "Assets/_Project/Art/VisualPolish";
        public static void Prepare()
        {
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            Atlas(); BottomBun();
            foreach (string style in new[] { "Grill", "Prep", "Floor" })
            {
                OverlayMaterial(style);
                for (int i = 0; i < 6; i++) OverlayMesh(style, i);
            }
            AssetDatabase.SaveAssets();
        }
        private static float Noise(int key)
        {
            uint h = unchecked((uint)key); h = (h ^ (h >> 13)) * 1274126177; h ^= h >> 16;
            return (h & 65535) / 65535f;
        }
        private static void Atlas()
        {
            string path = Folder + "/OrganicDirtAtlas.png"; if (File.Exists(path)) return;
            const int n = 512; var texture = new Texture2D(n, n, TextureFormat.RGBA32, false, true);
            var pixels = new Color[n * n];
            float Blob(float x, float z, float seed, float stretch = 1)
            {
                float angle = Mathf.Atan2(z * stretch, x), r = Mathf.Sqrt(x * x + z * z * stretch * stretch);
                float edge = .69f + .10f * Mathf.Sin(angle * 5 + seed) + .065f * Mathf.Sin(angle * 9 - seed * 2);
                return 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(edge - .15f, edge, r));
            }
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                int tile = (x / 256) + (y / 256) * 2;
                float u = (x % 256) / 255f * 2 - 1, v = (y % 256) / 255f * 2 - 1;
                float grain = Noise(x * 374761393 ^ y * 668265263);
                float mottling = .63f + .13f * Mathf.Sin(u * 19 + Mathf.Sin(v * 13)) + .13f * Mathf.Cos(v * 23 + u * 3);
                float alpha = Blob(u, v, tile * 1.7f) * mottling * (.83f + .17f * grain);
                float value = .55f + .20f * grain;
                if (tile == 1) { alpha = Blob(u, v, 2.7f, 2.4f) * mottling; value = .7f + .15f * grain; }
                if (tile == 2) { alpha = Blob(u, v, 5.1f) * (.82f + .18f * grain); value = .18f + .35f * grain; }
                if (tile == 3)
                {
                    alpha = Blob(u * 2, v * 2, 6.1f) * .8f;
                    for (int drop = 0; drop < 7; drop++)
                    {
                        float dx = u - (Noise(drop * 721 + 91) - .5f) * 1.35f;
                        float dz = v - (Noise(drop * 557 + 83) - .5f) * 1.35f;
                        float radius = .055f + Noise(drop * 431 + 97) * .09f;
                        alpha = Mathf.Max(alpha, (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(radius * .6f, radius, Mathf.Sqrt(dx * dx + dz * dz)))) * .77f);
                    }
                }
                pixels[y * n + x] = new Color(value, value * .88f, value * .69f, alpha);
            }
            texture.SetPixels(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.sRGBTexture = true;
            importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp; importer.mipmapEnabled = true;
            importer.maxTextureSize = 512; importer.anisoLevel = 4; importer.SaveAndReimport();
        }
        public static Material OverlayMaterial(string style)
        {
            string path = Folder + "/" + style + "DirtOverlay.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path); if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = style + "DirtOverlay", enableInstancing = true };
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/OrganicDirtAtlas.png"));
            material.SetColor("_BaseColor", style == "Grill" ? new Color(.57f, .41f, .25f, .93f) :
                style == "Prep" ? new Color(.63f, .48f, .30f, .88f) : new Color(.43f, .41f, .34f, .92f));
            material.SetFloat("_Metallic", 0); material.SetFloat("_Smoothness", style == "Grill" ? .45f : style == "Prep" ? .20f : .08f);
            material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0);
            material.SetFloat("_BlendModePreserveSpecular", 0); material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0); material.SetFloat("_Cull", (float)CullMode.Off);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.SetOverrideTag("RenderType", "Transparent"); material.renderQueue = 3000;
            AssetDatabase.CreateAsset(material, path); return material;
        }
        public static Mesh OverlayMesh(string style, int variant)
        {
            string path = Folder + "/" + style + "Residue" + variant + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (mesh != null) return mesh;
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            int styleKey = style == "Grill" ? 173 : style == "Prep" ? 347 : 563;
            int seed = variant * 74131 + styleKey;
            for (int i = 0; i < 26; i++)
            {
                int key = seed + i * 7349;
                float x = (Noise(key) + Noise(key + 19) - 1) * .31f;
                float z = (Noise(key + 71) + Noise(key + 101) - 1) * .31f;
                int tile = i < 4 ? (style == "Floor" ? 1 : 0) : i < 8 ? 1 : i < 14 ? 3 : 2;
                float width = tile == 2 ? .018f + Noise(key + 59) * .06f : .13f + Noise(key + 59) * .22f;
                float depth = tile == 2 ? width * (.4f + Noise(key + 83) * .7f) : width * (.45f + Noise(key + 83) * .7f);
                float angle = Noise(key + 137) * Mathf.PI * 2;
                Vector3 right = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * width * .5f;
                Vector3 forward = new Vector3(-Mathf.Sin(angle), 0, Mathf.Cos(angle)) * depth * .5f;
                Vector3 center = new Vector3(x, i * .000015f, z); int start = vertices.Count;
                vertices.Add(center - right - forward); vertices.Add(center - right + forward);
                vertices.Add(center + right + forward); vertices.Add(center + right - forward);
                float a = (tile % 2) * .5f, b = (tile / 2) * .5f;
                uv.Add(new Vector2(a + .003f, b + .003f)); uv.Add(new Vector2(a + .003f, b + .497f));
                uv.Add(new Vector2(a + .497f, b + .497f)); uv.Add(new Vector2(a + .497f, b + .003f));
                triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
            mesh = new Mesh { name = style + "Residue" + variant };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
        public static Mesh BottomBun()
        {
            string path = Folder + "/BottomBun.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (mesh != null) return mesh;
            Vector2[] profile = { new Vector2(0, -.5f), new Vector2(.43f, -.5f), new Vector2(.49f, -.24f),
                new Vector2(.5f, .15f), new Vector2(.47f, .48f), new Vector2(0, .5f) };
            const int segments = 48; var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            for (int row = 0; row < profile.Length; row++) for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2 / segments; var p = new Vector3(Mathf.Cos(a) * profile[row].x, profile[row].y, Mathf.Sin(a) * profile[row].x);
                vertices.Add(p); uv.Add(new Vector2(p.x + .5f, p.z + .5f));
                if (row == 0 || i == 0) continue;
                int c = row * (segments + 1) + i;
                triangles.AddRange(new[] { c, c - segments - 2, c - 1, c, c - segments - 1, c - segments - 2 });
            }
            mesh = new Mesh { name = "BottomBun" }; mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
    }
}
