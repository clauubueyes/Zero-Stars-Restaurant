using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Hygiene;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Editor
{
    public static class M17HygieneBuilder
    {
        public const string SettingsPath = "Assets/_Project/ScriptableObjects/Hygiene.asset";
        public const string MaterialPath = "Assets/_Project/Materials/HygieneResidue.mat";
        public const string ToolMaterialPath = "Assets/_Project/Materials/CleaningTool.mat";
        [MenuItem("Zero Star Restaurant/Prototype/Install M17 Hygiene (incremental)")]
        public static void InstallInExistingScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetSceneByPath(M1GreyboxBuilder.ScenePath).IsValid())
                throw new InvalidOperationException("Save edits, stop Play and close PrototypeRestaurant before installing M17.");
            if (Application.isBatchMode && string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(M1GreyboxBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene); ConfigureScene(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save incremental hygiene components.");
                AssetDatabase.SaveAssets();
            }
            finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true); }
        }
        public static void ConfigureScene(Scene scene)
        {
            T[] Components<T>() where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
            var installed = Components<CleanableSurface>();
            if (installed.Length != 0) { foreach (var surface in installed) surface.Validate(); return; }
            var supports = Components<BoxCollider>();
            BoxCollider Support(string name) => supports.Single(item => item.name == name);
            var simulation = Components<FoodSimulation>().Single();
            var player = Components<PlayerInteraction>().Single();
            var settings = AssetDatabase.LoadAssetAtPath<HygieneSettings>(SettingsPath);
            if (settings == null) { settings = ScriptableObject.CreateInstance<HygieneSettings>(); AssetDatabase.CreateAsset(settings, SettingsPath); }
            Material Material(string path, Color color)
            {
                var existing = AssetDatabase.LoadAssetAtPath<Material>(path); if (existing != null) return existing;
                var created = new Material(Shader.Find("Universal Render Pipeline/Lit")); created.SetColor("_BaseColor", color);
                AssetDatabase.CreateAsset(created, path); return created;
            }
            var residue = Material(MaterialPath, new Color(.23f, .11f, .025f));
            var toolMaterial = Material(ToolMaterialPath, new Color(.08f, .7f, .8f));
            GameObject root = new GameObject("Hygiene"); SceneManager.MoveGameObjectToScene(root, scene);
            CleanableSurface AddSurface(string label, BoxCollider support, bool contacts, DirtKind kind = DirtKind.FoodResidue, Vector2? center = null, Vector2? size = null)
            {
                var owner = new GameObject(label); owner.transform.SetParent(root.transform, false);
                var surface = owner.AddComponent<CleanableSurface>();
                var data = new SerializedObject(surface);
                data.FindProperty("_settings").objectReferenceValue = settings; data.FindProperty("_support").objectReferenceValue = support;
                data.FindProperty("_displayName").stringValue = label;
                if (center.HasValue) data.FindProperty("_regionCenter").vector2Value = center.Value;
                if (size.HasValue) data.FindProperty("_regionSize").vector2Value = size.Value;
                data.ApplyModifiedPropertiesWithoutUndo(); surface.Validate();
                if (contacts)
                {
                    var contact = owner.AddComponent<SurfaceFoodContact>(); var contactData = new SerializedObject(contact);
                    contactData.FindProperty("_surface").objectReferenceValue = surface;
                    contactData.FindProperty("_simulation").objectReferenceValue = simulation;
                    contactData.FindProperty("_kind").enumValueIndex = (int)kind; contactData.ApplyModifiedPropertiesWithoutUndo();
                }
                var view = owner.AddComponent<DirtSurfaceView>(); var viewData = new SerializedObject(view);
                viewData.FindProperty("_surface").objectReferenceValue = surface;
                var stains = viewData.FindProperty("_stains"); stains.arraySize = 6;
                for (int i = 0; i < stains.arraySize; i++)
                {
                    GameObject stain = GameObject.CreatePrimitive(PrimitiveType.Cube); stain.name = "Residue " + (i + 1);
                    UnityEngine.Object.DestroyImmediate(stain.GetComponent<Collider>());
                    stain.GetComponent<Renderer>().sharedMaterial = residue;
                    stain.transform.SetParent(root.transform, false); stain.transform.localScale = Vector3.zero;
                    stains.GetArrayElementAtIndex(i).objectReferenceValue = stain.transform;
                }
                viewData.ApplyModifiedPropertiesWithoutUndo(); return surface;
            }
            var grill = AddSurface("Grill", Support("GrillHotSurface"), false);
            var sourceData = new SerializedObject(Components<GrillHeatSource>().Single());
            sourceData.FindProperty("_cleanableSurface").objectReferenceValue = grill; sourceData.ApplyModifiedPropertiesWithoutUndo();
            AddSurface("Prep / Assembly worktop", Support("AssemblyWorkbench"), true);
            for (int i = 1; i <= 3; i++) AddSurface("Assembly " + i, Support("PrepSupport" + i), true);
            BoxCollider floor = Support("Floor");
            Vector2 FloorCenter(Vector3 world)
            { Vector3 local = floor.transform.InverseTransformPoint(world) - floor.center; return new Vector2(local.x, local.z); }
            Vector2 FloorSize(Vector2 world) => new Vector2(world.x / Mathf.Abs(floor.transform.lossyScale.x), world.y / Mathf.Abs(floor.transform.lossyScale.z));
            AddSurface("Floor near Grill", floor, true, DirtKind.GeneralDirt, FloorCenter(Support("GrillHotSurface").bounds.center - Vector3.forward), FloorSize(new Vector2(2.4f, 1.2f)));
            AddSurface("Floor near Prep", floor, true, DirtKind.GeneralDirt, FloorCenter(Support("AssemblyWorkbench").bounds.center - Vector3.forward), FloorSize(new Vector2(3.4f, 1.2f)));
            var cleaning = player.gameObject.AddComponent<CleaningInteraction>(); var cleaningData = new SerializedObject(cleaning);
            cleaningData.FindProperty("_carry").objectReferenceValue = player.GetComponent<PhysicalCarry>();
            cleaningData.FindProperty("_detector").objectReferenceValue = player.GetComponent<InteractionDetector>();
            var surfaces = cleaningData.FindProperty("_surfaces"); var createdSurfaces = Components<CleanableSurface>(); surfaces.arraySize = createdSurfaces.Length;
            for (int i = 0; i < surfaces.arraySize; i++) surfaces.GetArrayElementAtIndex(i).objectReferenceValue = createdSurfaces[i];
            cleaningData.ApplyModifiedPropertiesWithoutUndo();
            foreach (Component adapter in new Component[] { player.GetComponent<InteractionInput>(), player.GetComponent<InteractionFeedback>() })
            { var data = new SerializedObject(adapter); data.FindProperty("_cleaning").objectReferenceValue = cleaning; data.ApplyModifiedPropertiesWithoutUndo(); }
            GameObject tool = GameObject.CreatePrimitive(PrimitiveType.Cube); tool.name = "CleaningTool"; SceneManager.MoveGameObjectToScene(tool, scene);
            tool.transform.localScale = new Vector3(.4f, .08f, .22f);
            Bounds bench = Support("AssemblyWorkbench").bounds;
            tool.transform.position = new Vector3(bench.min.x + .3f, bench.max.y + .05f, bench.min.z + .2f);
            tool.GetComponent<Renderer>().sharedMaterial = toolMaterial;
            var body = tool.AddComponent<Rigidbody>(); body.mass = .3f; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.solverIterations = 12;
            var pickup = tool.AddComponent<Pickup>(); var pickupData = new SerializedObject(pickup);
            pickupData.FindProperty("_displayName").stringValue = "Cleaning Tool"; pickupData.ApplyModifiedPropertiesWithoutUndo();
            tool.AddComponent<CleaningTool>(); EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
