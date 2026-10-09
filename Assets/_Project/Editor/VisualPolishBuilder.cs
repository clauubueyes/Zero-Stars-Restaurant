using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Hygiene;
using ZeroStarRestaurant.Presentation;

namespace ZeroStarRestaurant.Editor
{
    public static class VisualPolishBuilder
    {
        private static void Food(GameObject item)
        {
            var shell = item.transform.Find("Visual_VP1BC");
            if (shell == null || shell.GetComponent<BurgerIngredientVisual>() != null) return;
            var component = shell.gameObject.AddComponent<BurgerIngredientVisual>(); var data = new SerializedObject(component);
            data.FindProperty("_food").objectReferenceValue = item.GetComponent<FoodItem>();
            data.FindProperty("_bun").objectReferenceValue = shell.Find("Bun")?.GetComponent<MeshFilter>();
            data.FindProperty("_bottomBun").objectReferenceValue = VisualPolishAssets.BottomBun();
            data.FindProperty("_crumb").objectReferenceValue = shell.Find("CutCrumb");
            data.FindProperty("_sesame").objectReferenceValue = shell.Find("Sesame")?.GetComponent<Renderer>();
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        public static void ConfigureScene(Scene scene)
        {
            T[] All<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
            foreach (var food in All<FoodItem>()) Food(food.gameObject);
            foreach (var view in All<DirtSurfaceView>())
            {
                var data = new SerializedObject(view); if (data.FindProperty("_organicOverlay").boolValue) continue;
                var surface = view.GetComponent<CleanableSurface>();
                var contact = view.GetComponent<SurfaceFoodContact>();
                string style = All<GrillHeatSource>().Any(g => g.CleanableSurface == surface) ? "Grill" :
                    contact != null && new SerializedObject(contact).FindProperty("_kind").enumValueIndex == (int)DirtKind.GeneralDirt ? "Floor" : "Prep";
                int seed = 17; foreach (char c in surface.DisplayName) seed = unchecked(seed * 31 + c);
                data.FindProperty("_visualSeed").intValue = seed;
                data.FindProperty("_organicOverlay").boolValue = true;
                var stains = data.FindProperty("_stains");
                for (int i = 0; i < stains.arraySize; i++)
                {
                    var stain = (Transform)stains.GetArrayElementAtIndex(i).objectReferenceValue;
                    stain.GetComponent<MeshFilter>().sharedMesh = VisualPolishAssets.OverlayMesh(style, i);
                    var renderer = stain.GetComponent<Renderer>(); renderer.sharedMaterial = VisualPolishAssets.OverlayMaterial(style);
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = true;
                }
                data.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        [MenuItem("Zero Star Restaurant/Visual/Install Burger and Dirt Polish (incremental)")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetSceneByPath(M1GreyboxBuilder.ScenePath).IsValid())
                throw new InvalidOperationException("Save edits, leave Play and close PrototypeRestaurant before installing polish.");
            VisualPolishAssets.Prepare();
            foreach (string path in new[] { "Assets/_Project/Prefabs/Food/Bun.prefab", "Assets/_Project/Prefabs/Food/RawBeefPatty.prefab", "Assets/_Project/Prefabs/Food/Cheese.prefab" })
            {
                string original = File.ReadAllText(path); var contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (contents.transform.Find("Visual_VP1BC").GetComponent<BurgerIngredientVisual>() != null) continue;
                    Food(contents); PrefabUtility.SaveAsPrefabAsset(contents, path);
                    string merged;
                    try { merged = Merge(original, File.ReadAllText(path)); }
                    catch { File.WriteAllText(path, original, new UTF8Encoding(false)); throw; }
                    File.WriteAllText(path, merged, new UTF8Encoding(false));
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
                AssetDatabase.ImportAsset(path);
            }
            if (string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var previous = SceneManager.GetActiveScene(); string originalScene = File.ReadAllText(M1GreyboxBuilder.ScenePath);
            var scene = EditorSceneManager.OpenScene(M1GreyboxBuilder.ScenePath, OpenSceneMode.Additive);
            string temporary = "Assets/_Project/Scenes/__VisualPolish_" + Guid.NewGuid().ToString("N") + ".unity";
            try
            {
                bool complete = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<FoodItem>(true))
                    .All(f => f.transform.Find("Visual_VP1BC").GetComponent<BurgerIngredientVisual>() != null) &&
                    scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<DirtSurfaceView>(true)).All(v => new SerializedObject(v).FindProperty("_organicOverlay").boolValue);
                if (complete) return;
                SceneManager.SetActiveScene(scene); ConfigureScene(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene, temporary, true);
                string merged = Merge(originalScene, File.ReadAllText(temporary));
                Directory.CreateDirectory("Build/VisualPolish");
                if (!File.Exists("Build/VisualPolish/PrototypeRestaurant.before.unity")) File.WriteAllText("Build/VisualPolish/PrototypeRestaurant.before.unity", originalScene, new UTF8Encoding(false));
                File.WriteAllText(M1GreyboxBuilder.ScenePath, merged, new UTF8Encoding(false)); AssetDatabase.ImportAsset(M1GreyboxBuilder.ScenePath);
            }
            finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true); AssetDatabase.DeleteAsset(temporary); }
        }
        // Original serialized transforms and gameplay remain byte-for-byte intact. Only explicitly visual fields merge.
        public static string Merge(string original, string serialized)
        {
            string[] before = Regex.Split(original.Replace("\r\n", "\n"), "(?=^--- !u!)", RegexOptions.Multiline);
            long Id(string b) => long.Parse(Regex.Match(b, @"^--- !u!\d+ &(-?\d+)").Groups[1].Value);
            var after = Regex.Split(serialized.Replace("\r\n", "\n"), "(?=^--- !u!)", RegexOptions.Multiline).Skip(1).ToDictionary(Id);
            var originals = before.Skip(1).Select(Id).ToHashSet();
            var stainObjects = before.Where(b => b.StartsWith("--- !u!1 &") && Regex.IsMatch(b, @"(?m)^  m_Name: Residue \d+$")).Select(Id).ToHashSet();
            for (int i = 1; i < before.Length; i++)
            {
                string b = before[i]; if (!after.TryGetValue(Id(b), out var next)) throw new InvalidOperationException("Removed original document.");
                if (b.StartsWith("--- !u!1 &"))
                {
                    string pattern = @"(?m)^  m_Component:\n(?:  - component: \{fileID: -?\d+\}\n)*";
                    string oldList = Regex.Match(b, pattern).Value, newList = Regex.Match(next, pattern).Value;
                    var additions = Regex.Matches(newList, @"  - component: \{fileID: (-?\d+)\}").Cast<Match>().Where(m => !oldList.Contains(m.Value)).Select(m => m.Value).ToArray();
                    if (additions.Length > 0) b = Regex.Replace(b, pattern, oldList + string.Join("\n", additions) + "\n");
                }
                if (b.StartsWith("--- !u!114 &") && b.Contains("ZeroStarRestaurant.Hygiene.DirtSurfaceView"))
                    b = b.TrimEnd('\n') + "\n" + Regex.Match(next, @"(?m)^  _organicOverlay:.*$").Value + "\n" + Regex.Match(next, @"(?m)^  _visualSeed:.*$").Value + "\n";
                if (Regex.IsMatch(b, @"^--- !u!(23|33) &") && stainObjects.Contains(long.Parse(Regex.Match(b, @"m_GameObject: \{fileID: (-?\d+)\}").Groups[1].Value)))
                {
                    foreach (string pattern in new[] { @"(?m)^  m_Mesh:.*$", @"(?m)^  m_CastShadows:.*$", @"(?m)^  m_ReceiveShadows:.*$", @"(?m)^  m_Materials:\n(?:  - \{fileID: [^\r\n]*\}\n)*" })
                        if (Regex.IsMatch(b, pattern)) b = Regex.Replace(b, pattern, Regex.Match(next, pattern).Value);
                }
                before[i] = b;
            }
            string result = string.Concat(before) + string.Concat(after.Where(p => !originals.Contains(p.Key)).Select(p => Regex.Replace(p.Value, @"[ \t]+(?=\n|$)", "")));
            return result.Replace("\n", original.Contains("\r\n") ? "\r\n" : "\n");
        }
    }
}
