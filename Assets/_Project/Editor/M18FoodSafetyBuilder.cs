using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Hygiene;

namespace ZeroStarRestaurant.Editor
{
    public static class M18FoodSafetyBuilder
    {
        public const string SettingsPath = "Assets/_Project/ScriptableObjects/FoodSafety.asset";

        private static FoodSafetySettings Settings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<FoodSafetySettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<FoodSafetySettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            settings.CreatePolicy(); return settings;
        }

        // In-memory incremental path for a scene being authored; does not move or rebuild objects.
        public static void ConfigureScene(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var surfaces = roots.SelectMany(root => root.GetComponentsInChildren<CleanableSurface>(true)).ToArray();
            if (surfaces.Length == 0) throw new InvalidOperationException("Install M17 surfaces first.");
            var simulation = roots.SelectMany(root => root.GetComponentsInChildren<FoodSimulation>(true)).Single();
            var settings = Settings();
            foreach (var surface in surfaces)
            {
                var data = new SerializedObject(surface);
                if (data.FindProperty("_foodSafetySettings").objectReferenceValue == null)
                {
                    data.FindProperty("_foodSafetySettings").objectReferenceValue = settings;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                if (surface.GetComponent<SurfaceFoodContact>() != null) continue;
                var contact = surface.gameObject.AddComponent<SurfaceFoodContact>();
                var contactData = new SerializedObject(contact);
                contactData.FindProperty("_surface").objectReferenceValue = surface;
                contactData.FindProperty("_simulation").objectReferenceValue = simulation;
                contactData.FindProperty("_dirtPerContact").floatValue = 0;
                contactData.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.MarkSceneDirty(scene);
        }

        [MenuItem("Zero Star Restaurant/Prototype/Install M18 Food Safety (incremental)")]
        public static void InstallInExistingScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetSceneByPath(M1GreyboxBuilder.ScenePath).IsValid())
                throw new InvalidOperationException("Save edits, stop Play and close PrototypeRestaurant before installing M18.");
            Settings(); AssetDatabase.SaveAssets();
            string text = File.ReadAllText(M1GreyboxBuilder.ScenePath);
            string newline = text.Contains("\r\n") ? "\r\n" : "\n";
            var documents = Regex.Split(text, "(?=^--- !u!)", RegexOptions.Multiline).ToList();
            string GuidOf<T>() where T : UnityEngine.Object
            {
                string path = AssetDatabase.FindAssets("t:MonoScript " + typeof(T).Name)
                    .Select(AssetDatabase.GUIDToAssetPath).Single(item => Path.GetFileNameWithoutExtension(item) == typeof(T).Name);
                return AssetDatabase.AssetPathToGUID(path);
            }
            string surfaceGuid = GuidOf<CleanableSurface>(), contactGuid = GuidOf<SurfaceFoodContact>(), simulationGuid = GuidOf<FoodSimulation>();
            var surfaces = documents.Where(block => block.Contains("guid: " + surfaceGuid + ",")).ToArray();
            if (surfaces.Length == 0) throw new InvalidOperationException("Install M17 surfaces first.");
            string simulation = documents.Single(block => block.Contains("guid: " + simulationGuid + ","));
            long Id(string block) => long.Parse(Regex.Match(block, @"^--- !u!\d+ &(-?\d+)").Groups[1].Value);
            long Owner(string block) => long.Parse(Regex.Match(block, @"m_GameObject: \{fileID: (\d+)\}").Groups[1].Value);
            var usedIds = new System.Collections.Generic.HashSet<long>(documents.Skip(1).Select(Id));
            long nextId = 3000000000;
            string settingsGuid = AssetDatabase.AssetPathToGUID(SettingsPath);
            foreach (string surface in surfaces)
            {
                int index = documents.IndexOf(surface);
                var safety = Regex.Match(surface, @"^  _foodSafetySettings: \{fileID: (\d+).*\}\r?$", RegexOptions.Multiline);
                if (!safety.Success)
                    documents[index] = surface.Replace("  _settings:", "  _foodSafetySettings: {fileID: 11400000, guid: " + settingsGuid + ", type: 2}" + newline + "  _settings:");
                else if (safety.Groups[1].Value == "0")
                    documents[index] = surface.Replace(safety.Value, "  _foodSafetySettings: {fileID: 11400000, guid: " + settingsGuid + ", type: 2}");
                long ownerId = Owner(surface);
                if (documents.Any(block => block.Contains("guid: " + contactGuid + ",") && Owner(block) == ownerId)) continue;
                int ownerIndex = documents.FindIndex(block => block.StartsWith("--- !u!1 &" + ownerId + newline));
                if (ownerIndex < 0) throw new InvalidOperationException("Surface owner not found.");
                while (usedIds.Contains(nextId)) nextId++;
                long contactId = nextId++; usedIds.Add(contactId);
                documents[ownerIndex] = documents[ownerIndex].Replace("  m_Layer:", "  - component: {fileID: " + contactId + "}" + newline + "  m_Layer:");
                documents.Add(string.Join(newline, new[] {
                    "--- !u!114 &" + contactId, "MonoBehaviour:", "  m_ObjectHideFlags: 0",
                    "  m_CorrespondingSourceObject: {fileID: 0}", "  m_PrefabInstance: {fileID: 0}", "  m_PrefabAsset: {fileID: 0}",
                    "  m_GameObject: {fileID: " + ownerId + "}", "  m_Enabled: 1", "  m_EditorHideFlags: 0",
                    "  m_Script: {fileID: 11500000, guid: " + contactGuid + ", type: 3}", "  m_Name:", "  m_EditorClassIdentifier:",
                    "  _surface: {fileID: " + Id(surface) + "}", "  _simulation: {fileID: " + Id(simulation) + "}",
                    "  _dirtPerContact: 0", "  _contactTolerance: 0.035", "  _kind: 0", "  _pollAutomatically: 1", ""
                }));
            }
            string updated = string.Concat(documents);
            if (updated == text) return;
            File.WriteAllText(M1GreyboxBuilder.ScenePath, updated, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(M1GreyboxBuilder.ScenePath);
        }
    }
}
