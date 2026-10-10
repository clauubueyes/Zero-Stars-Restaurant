using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZeroStarRestaurant.Editor
{
    // Repairs existing presentation children. Never rebuilds or edits a functional object.
    public static class PostM19VisualFixBuilder
    {
        public static Component[] ConfigureScene(Scene scene)
        {
            var changes = new HashSet<Component>();
            var transforms = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            Transform Find(string name) => transforms.Single(t => t.name == name);
            Transform Cladding(Transform t) => t.Find("Visual_VP1BC/Cladding");
            void Mesh(Transform t, Mesh mesh)
            {
                var filter = t.GetComponent<MeshFilter>();
                if (filter.sharedMesh == mesh) return;
                filter.sharedMesh = mesh; changes.Add(filter);
            }
            void Position(Transform t, Vector3 position)
            { if ((t.localPosition - position).sqrMagnitude > .00000001f) { t.localPosition = position; changes.Add(t); } }
            void Hide(Transform t)
            {
                var renderer = t.GetComponent<Renderer>();
                if (renderer.enabled) { renderer.enabled = false; changes.Add(renderer); }
            }
            var main = Cladding(Find("Floor")); var annex = Cladding(Find("ProcurementFloor"));
            // The dedicated floor mesh is the receipt for this synchronous migration. Reapplying
            // an installed pass must preserve later manual edits to its visual children too.
            if (annex.GetComponent<MeshFilter>().sharedMesh.name.StartsWith("PostM19_")) return Array.Empty<Component>();
            // Partition the shared floor plane; retain its height and the original collider footprint.
            // UVs keep the main floor's metre grid across the annex seam, without stretched tiles.
            if (!annex.GetComponent<MeshFilter>().sharedMesh.name.StartsWith("PostM19_"))
            {
                Bounds a = main.GetComponent<Renderer>().bounds, b = annex.GetComponent<Renderer>().bounds;
                if (b.max.x > a.min.x && b.min.x < a.min.x)
                {
                    var size = b.size; size.x = a.min.x - b.min.x;
                    Vector3 center = b.center; center.x = (b.min.x + a.min.x) * .5f;
                    Position(annex, annex.parent.InverseTransformPoint(center));
                    var mesh = UnityEngine.Object.Instantiate(VP1BCArtAssets.Box(size)); mesh.name = "PostM19_AnnexFloor";
                    Vector2[] uv = mesh.uv; Vector3[] vertices = mesh.vertices; Vector3[] normals = mesh.normals;
                    for (int i = 0; i < vertices.Length; i++)
                        if (normals[i].y > .9f)
                        { Vector3 world = annex.TransformPoint(vertices[i]); uv[i] = new Vector2(world.x - a.min.x, a.max.z - world.z); }
                    mesh.uv = uv; mesh.RecalculateTangents();
                    string path = VP1BCArtAssets.Folder + "/Meshes/PostM19_AnnexFloor.asset";
                    var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (existing == null) { AssetDatabase.CreateAsset(mesh, path); existing = mesh; }
                    else UnityEngine.Object.DestroyImmediate(mesh);
                    Mesh(annex, existing);
                }
            }
            foreach (var shell in transforms.Where(t => t.name == "Visual_VP1BC"))
            {
                var pieces = shell.Cast<Transform>().ToArray();
                if (shell.parent.name.Contains("Lintel"))
                {
                    // Floor-level skirting/wainscot on an overhead beam was a redundant black band.
                    foreach (var piece in pieces.Where(t => t.name is "PaintWainscot" or "TileWainscot" or "Skirting" or "WainscotTrim")) Hide(piece);
                    continue;
                }
                var backing = shell.Find("Cladding");
                if (backing == null) continue;
                var bounds = backing.GetComponent<MeshFilter>().sharedMesh.bounds;
                int axis = bounds.size.x > bounds.size.z ? 2 : 0;
                foreach (var piece in pieces.Where(t => t.name is "PaintWainscot" or "TileWainscot" or "Skirting" or "WainscotTrim"))
                {
                    // Real cladding thicknesses: tile/enamel 14 mm; metal/wood trims 30 mm.
                    // The exposed faces are separated by 16 mm, rather than coincident 4 mm slabs.
                    Vector3 size = piece.GetComponent<MeshFilter>().sharedMesh.bounds.size;
                    float depth = piece.name.EndsWith("Wainscot") ? .014f : .030f;
                    size[axis] = depth;
                    Vector3 p = piece.localPosition;
                    p[axis] = Mathf.Sign(p[axis]) * (bounds.extents[axis] + depth * .5f);
                    Position(piece, p); Mesh(piece, VP1BCArtAssets.Box(size));
                }
            }
            var counter = Find("ServiceCounterVolume").Find("Visual_VP1BC");
            var body = counter.Find("Cladding"); var top = counter.Find("CounterTop");
            Bounds bodyBounds = body.GetComponent<MeshFilter>().sharedMesh.bounds;
            float bottom = body.localPosition.y + bodyBounds.min.y;
            float join = top.localPosition.y + top.GetComponent<MeshFilter>().sharedMesh.bounds.min.y;
            if (body.localPosition.y + bodyBounds.max.y > join + .0001f)
            {
                Vector3 size = bodyBounds.size; size.y = join - bottom;
                Vector3 p = body.localPosition; p.y = (bottom + join) * .5f;
                Position(body, p); Mesh(body, VP1BCArtAssets.Box(size));
            }
            // The fixed assembly supports already render the same top and sides as their TrayVisual.
            foreach (var tray in transforms.Where(t => t.name == "TrayVisual" && t.parent.name.StartsWith("PrepSupport")))
                if (Cladding(tray) != null) Hide(Cladding(tray));
            if (changes.Count > 0) EditorSceneManager.MarkSceneDirty(scene);
            return changes.ToArray();
        }

        [MenuItem("Zero Star Restaurant/Visual/Repair Post-M19 Visual Overlaps (incremental)")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetSceneByPath(M1GreyboxBuilder.ScenePath).IsValid())
                throw new InvalidOperationException("Save edits, leave Play and close PrototypeRestaurant before the visual repair.");
            string original = File.ReadAllText(M1GreyboxBuilder.ScenePath);
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(M1GreyboxBuilder.ScenePath, OpenSceneMode.Additive);
            string temporary = "Assets/_Project/Scenes/PostM19RepairTemporary.unity";
            try
            {
                Component[] changed = ConfigureScene(scene);
                if (changed.Length == 0) { Debug.Log("Post-M19 repair already installed; nothing changed."); return; }
                var ids = changed.Select(c => (long)GlobalObjectId.GetGlobalObjectIdSlow(c).targetObjectId).ToHashSet();
                AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene, temporary, true);
                // Keep every document byte-for-byte except approved visual position/mesh/enabled fields.
                var serialized = Regex.Split(File.ReadAllText(temporary).Replace("\r\n", "\n"), "(?=^--- !u!)", RegexOptions.Multiline)
                    .Skip(1).ToDictionary(b => long.Parse(Regex.Match(b, @"^--- !u!\d+ &(-?\d+)").Groups[1].Value));
                var blocks = Regex.Split(original.Replace("\r\n", "\n"), "(?=^--- !u!)", RegexOptions.Multiline);
                for (int i = 1; i < blocks.Length; i++)
                {
                    long id = long.Parse(Regex.Match(blocks[i], @"^--- !u!\d+ &(-?\d+)").Groups[1].Value);
                    if (!ids.Contains(id)) continue;
                    string pattern = blocks[i].StartsWith("--- !u!4 &") ? @"(?m)^  m_LocalPosition: .+$" :
                        blocks[i].StartsWith("--- !u!33 &") ? @"(?m)^  m_Mesh: .+$" :
                        blocks[i].StartsWith("--- !u!23 &") ? @"(?m)^  m_Enabled: .+$" : throw new InvalidOperationException("Nonvisual repair document.");
                    blocks[i] = Regex.Replace(blocks[i], pattern, Regex.Match(serialized[id], pattern).Value);
                }
                Directory.CreateDirectory("Build/PostM19");
                File.WriteAllText("Build/PostM19/PrototypeRestaurant.before.unity", original, new UTF8Encoding(false));
                File.WriteAllText(M1GreyboxBuilder.ScenePath, string.Concat(blocks), new UTF8Encoding(false));
                Debug.Log("Post-M19 repaired " + changed.Length + " visual components; functional serialization preserved.");
            }
            finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true); AssetDatabase.DeleteAsset(temporary); }
            AssetDatabase.ImportAsset(M1GreyboxBuilder.ScenePath);
        }
    }
}
