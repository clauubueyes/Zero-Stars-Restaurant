using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Presentation;

namespace ZeroStarRestaurant.Editor
{
    public static class FoodVisualPlacementBuilder
    {
        // Art data belongs to the prefab. Runtime support placement has no food/recipe exceptions.
        public static void ConfigurePresentation(BurgerIngredientVisual visual)
        {
            var data = new SerializedObject(visual);
            var food = (ZeroStarRestaurant.Food.FoodItem)data.FindProperty("_food").objectReferenceValue;
            float height = food.Definition.Id == "food.bun" ? .45f : food.Definition.Id == "food.cheese" ? .07f : .28f;
            data.FindProperty("_heightScale").floatValue = height;
            data.FindProperty("_supportingHeightScale").floatValue = food.Definition.Id == "food.bun" ? .20f : height;
            data.FindProperty("_diameterScale").floatValue = .95f; data.ApplyModifiedPropertiesWithoutUndo();
        }
        [MenuItem("Zero Star Restaurant/Visual/Install Food Visual Placement Fix (incremental)")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetSceneByPath(M1GreyboxBuilder.ScenePath).IsValid())
                throw new InvalidOperationException("Save edits, leave Play and close PrototypeRestaurant before installing.");
            foreach (string path in new[] { "Assets/_Project/Prefabs/Food/Bun.prefab", "Assets/_Project/Prefabs/Food/RawBeefPatty.prefab", "Assets/_Project/Prefabs/Food/Cheese.prefab" })
            {
                string original = File.ReadAllText(path); if (original.Contains("  _heightScale:")) continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    ConfigurePresentation(root.transform.Find("Visual_VP1BC").GetComponent<BurgerIngredientVisual>());
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    string merged;
                    try { merged = Merge(original, File.ReadAllText(path)); }
                    catch { File.WriteAllText(path, original, new UTF8Encoding(false)); throw; }
                    File.WriteAllText(path, merged, new UTF8Encoding(false));
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
                AssetDatabase.ImportAsset(path);
            }
            if (string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var previous = SceneManager.GetActiveScene(); string before = File.ReadAllText(M1GreyboxBuilder.ScenePath);
            var scene = EditorSceneManager.OpenScene(M1GreyboxBuilder.ScenePath, OpenSceneMode.Additive);
            string temporary = "Assets/_Project/Scenes/__Placement_" + Guid.NewGuid().ToString("N") + ".unity";
            try
            {
                T[] All<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
                if (before.Contains("ZeroStarRestaurant.Presentation.FoodVisualSupportSurface") && Regex.Matches(before, @"(?m)^  _heightScale:").Count == All<BurgerIngredientVisual>().Length) return;
                var oldBlocks = Blocks(before);
                foreach (var visual in All<BurgerIngredientVisual>())
                {
                    long id = unchecked((long)GlobalObjectId.GetGlobalObjectIdSlow(visual).targetObjectId);
                    if (!oldBlocks[id].Contains("  _heightScale:")) ConfigurePresentation(visual);
                }
                if (All<FoodVisualSupportSurface>().Length == 0)
                {
                    var marker = All<Transform>().Single(t => t.name == "OutputMarker").Find("Visual_VP1BC/Cladding").GetComponent<Renderer>();
                    Physics.SyncTransforms();
                    if (!Physics.Raycast(marker.bounds.center + Vector3.up * .2f, Vector3.down, out var hit, .4f, ~0, QueryTriggerInteraction.Ignore) || hit.collider.gameObject.scene != scene)
                        throw new InvalidOperationException("Cannot identify current physical support under the collect overlay.");
                    var component = hit.collider.gameObject.AddComponent<FoodVisualSupportSurface>(); var data = new SerializedObject(component);
                    var overlays = data.FindProperty("_overlays"); overlays.arraySize = 1; overlays.GetArrayElementAtIndex(0).objectReferenceValue = marker;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                SceneManager.SetActiveScene(scene); EditorSceneManager.SaveScene(scene, temporary, true);
                string merged = Merge(before, File.ReadAllText(temporary));
                Directory.CreateDirectory("Build/FoodVisualPlacement");
                if (!File.Exists("Build/FoodVisualPlacement/PrototypeRestaurant.before.unity")) File.WriteAllText("Build/FoodVisualPlacement/PrototypeRestaurant.before.unity", before, new UTF8Encoding(false));
                File.WriteAllText(M1GreyboxBuilder.ScenePath, merged, new UTF8Encoding(false)); AssetDatabase.ImportAsset(M1GreyboxBuilder.ScenePath);
            }
            finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true); AssetDatabase.DeleteAsset(temporary); }
        }
        private static System.Collections.Generic.Dictionary<long, string> Blocks(string text) => Regex.Split(text.Replace("\r\n", "\n"), "(?=^--- !u!)", RegexOptions.Multiline)
            .Skip(1).ToDictionary(b => long.Parse(Regex.Match(b, @"^--- !u!\d+ &(-?\d+)").Groups[1].Value));
        public static string Merge(string original, string serialized)
        {
            var after = Blocks(serialized); var ids = Blocks(original).Keys.ToHashSet();
            var before = Regex.Split(original.Replace("\r\n", "\n"), "(?=^--- !u!)", RegexOptions.Multiline);
            for (int i = 1; i < before.Length; i++)
            {
                var id = long.Parse(Regex.Match(before[i], @"^--- !u!\d+ &(-?\d+)").Groups[1].Value);
                if (!after.TryGetValue(id, out var next)) throw new InvalidOperationException("Removed original document.");
                if (before[i].Contains("ZeroStarRestaurant.Presentation.BurgerIngredientVisual") && !before[i].Contains("  _heightScale:"))
                    foreach (string field in new[] { "_heightScale", "_supportingHeightScale", "_diameterScale" })
                        before[i] = before[i].TrimEnd('\n') + "\n" + Regex.Match(next, @"(?m)^  " + field + ":.*$").Value + "\n";
                if (before[i].StartsWith("--- !u!1 &"))
                {
                    string pattern = @"(?m)^  m_Component:\n(?:  - component: \{fileID: -?\d+\}\n)*";
                    string oldList = Regex.Match(before[i], pattern).Value, newList = Regex.Match(next, pattern).Value;
                    var added = Regex.Matches(newList, @"  - component: \{fileID: (-?\d+)\}").Cast<Match>().Where(m => !oldList.Contains(m.Value)).ToArray();
                    foreach (var m in added) if (!after[long.Parse(m.Groups[1].Value)].Contains("ZeroStarRestaurant.Presentation.FoodVisualSupportSurface")) throw new InvalidOperationException("Unexpected component.");
                    if (added.Length > 0) before[i] = Regex.Replace(before[i], pattern, oldList + string.Join("\n", added.Select(m => m.Value)) + "\n");
                }
            }
            var additions = after.Where(p => !ids.Contains(p.Key)).ToArray();
            if (additions.Any(p => !p.Value.Contains("ZeroStarRestaurant.Presentation.FoodVisualSupportSurface"))) throw new InvalidOperationException("Unexpected new document.");
            return (string.Concat(before) + string.Concat(additions.Select(p => Regex.Replace(p.Value, @"[ \t]+(?=\n|$)", "")))).Replace("\n", original.Contains("\r\n") ? "\r\n" : "\n");
        }
    }
}
