using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Hygiene;
using ZeroStarRestaurant.Player;
using ZeroStarRestaurant.Presentation;
using ZeroStarRestaurant.Restaurant;
using ZeroStarRestaurant.Utilities;

namespace ZeroStarRestaurant.Editor
{
    public static class VisualPolishValidation
    {
        private const string Key = "ZeroStarRestaurant.VisualPolish.Capture";
        private static int _frames;
        [InitializeOnLoadMethod] private static void Resume()
        {
            if (!Application.isBatchMode || !SessionState.GetBool(Key, false)) return;
            EditorApplication.playModeStateChanged -= Changed; EditorApplication.playModeStateChanged += Changed;
        }
        public static void Run()
        {
            if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Dedicated batch capture required.");
            EditorSceneManager.OpenScene(M1GreyboxBuilder.ScenePath); SessionState.SetBool(Key, true); SessionState.SetInt(Key + "Code", 0);
            EditorApplication.playModeStateChanged += Changed; EditorApplication.EnterPlaymode();
        }
        private static void Changed(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) { _frames = 0; EditorApplication.update += Ready; }
            if (state == PlayModeStateChange.EnteredEditMode)
            { EditorApplication.playModeStateChanged -= Changed; SessionState.SetBool(Key, false); EditorApplication.Exit(SessionState.GetInt(Key + "Code", 1)); }
        }
        private static void Ready()
        {
            if (++_frames < 30) return; EditorApplication.update -= Ready;
            try { Views(); } catch (Exception e) { Debug.LogException(e); SessionState.SetInt(Key + "Code", 1); }
            finally { EditorApplication.ExitPlaymode(); }
        }
        private static void Views()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) throw new InvalidOperationException("Graphics required.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            T[] All<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
            foreach (var component in new Component[] { All<RestaurantElectricity>().Single(), All<CustomerServiceLoop>().Single(), All<RestaurantDayController>().Single() })
            { var data = new SerializedObject(component); data.FindProperty("_advanceAutomatically").boolValue = false; data.ApplyModifiedPropertiesWithoutUndo(); }
            var simulation = All<FoodSimulation>().Single(); simulation.enabled = false; All<FirstPersonController>().Single().enabled = false;
            foreach (var contact in All<SurfaceFoodContact>()) contact.enabled = false;
            All<RestaurantElectricity>().Single().PowerOn(); foreach (var fixture in All<PoweredLightFixture>()) fixture.Refresh();
            var station = All<IngredientPurchaseStation>().Single();
            var camera = All<Camera>().Single(); var support = All<CleanableSurface>().Single(s => s.DisplayName == "Assembly 3").Support;
            var cooker = All<GrillHeatSource>().Single(); var appliance = All<ElectricalAppliance>().Single(a => a.ThermalSource == cooker); appliance.SetOn(true);
            var assembly = All<PhysicalDishAssembly>().Single(); var receipt = new System.Text.StringBuilder();
            Directory.CreateDirectory("Build/VisualPolish/Captures");
            DishItem Burger(bool cheese)
            {
                float top = support.bounds.max.y + .001f; var units = new System.Collections.Generic.List<FoodItem>();
                foreach (int product in cheese ? new[] { 0, 1, 2, 0 } : new[] { 0, 1, 0 })
                {
                    if (!station.TryPurchase(product, out var food)) throw new InvalidOperationException("Purchase failed: " + station.LastMessage);
                    var body = food.GetComponent<Rigidbody>(); body.interpolation = RigidbodyInterpolation.None;
                    float half = food.GetComponent<BoxCollider>().bounds.extents.y;
                    if (product == 1)
                    {
                        body.position = cooker.CleanableSurface.Support.bounds.center + Vector3.up * (cooker.CleanableSurface.Support.bounds.extents.y + half + .001f);
                        food.transform.position = body.position; Physics.SyncTransforms();
                        int steps = 0; while (food.State.Cooking.Stage < CookingStage.Cooked && ++steps < 800) simulation.Advance(.25);
                        if (food.State.Cooking.Stage != CookingStage.Cooked) throw new InvalidOperationException("Patty not cooked.");
                    }
                    body.position = new Vector3(support.bounds.center.x, top + half, support.bounds.center.z);
                    food.transform.position = body.position; top += half * 2; units.Add(food); Physics.SyncTransforms();
                }
                if (!assembly.TryFinalize(units.Last(), out var dish)) throw new InvalidOperationException("Physical stack not finalized.");
                if (dish.State.RecognizedDefinition?.Id != (cheese ? "dish.cheeseburger" : "dish.hamburger")) throw new InvalidOperationException("Wrong recipe.");
                dish.GetComponent<Rigidbody>().isKinematic = true;
                foreach (var visual in dish.GetComponentsInChildren<FoodStageVisual>()) visual.Refresh();
                foreach (var visual in dish.GetComponentsInChildren<BurgerIngredientVisual>()) visual.Refresh();
                receipt.AppendLine(dish.State.DisplayName + " " + dish.State.InstanceId + ": " + string.Join(", ", units.Select(f => f.State.InstanceId + "=" + (f.State.Cooking?.Stage.ToString() ?? "not cookable"))));
                return dish;
            }
            void View(string name, Vector3 position, Vector3 target)
            {
                camera.transform.position = position; camera.transform.LookAt(target);
                foreach (var visual in All<BurgerIngredientVisual>()) visual.SetPolishEnabled(true);
                foreach (var view in All<DirtSurfaceView>()) view.Refresh();
                Capture(camera, name + "-Polish");
                foreach (var visual in All<BurgerIngredientVisual>()) visual.SetPolishEnabled(false);
                var dirt = All<DirtSurfaceView>(); var meshes = new System.Collections.Generic.List<(MeshFilter, Mesh)>();
                var materials = new System.Collections.Generic.List<(Renderer, Material, ShadowCastingMode)>();
                Mesh cube = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Food/Bun.prefab").GetComponent<MeshFilter>().sharedMesh;
                Material originalDirt = AssetDatabase.LoadAssetAtPath<Material>(M17HygieneBuilder.MaterialPath);
                try
                {
                    foreach (var view in dirt)
                    {
                        var data = new SerializedObject(view); data.FindProperty("_organicOverlay").boolValue = false;
                        var stains = data.FindProperty("_stains");
                        for (int i = 0; i < stains.arraySize; i++)
                        {
                            var stain = (Transform)stains.GetArrayElementAtIndex(i).objectReferenceValue;
                            var mesh = stain.GetComponent<MeshFilter>(); var renderer = stain.GetComponent<Renderer>();
                            meshes.Add((mesh, mesh.sharedMesh)); materials.Add((renderer, renderer.sharedMaterial, renderer.shadowCastingMode));
                            mesh.sharedMesh = cube; renderer.sharedMaterial = originalDirt; renderer.SetPropertyBlock(null); renderer.shadowCastingMode = ShadowCastingMode.On;
                        }
                        data.ApplyModifiedPropertiesWithoutUndo(); view.Refresh();
                    }
                    Capture(camera, name + "-VP1BC-Before");
                }
                finally
                {
                    foreach (var pair in meshes) pair.Item1.sharedMesh = pair.Item2;
                    foreach (var pair in materials) { pair.Item1.sharedMaterial = pair.Item2; pair.Item1.shadowCastingMode = pair.Item3; }
                    foreach (var view in dirt) { var data = new SerializedObject(view); data.FindProperty("_organicOverlay").boolValue = true; data.ApplyModifiedPropertiesWithoutUndo(); view.Refresh(); }
                    foreach (var visual in All<BurgerIngredientVisual>()) visual.SetPolishEnabled(true);
                }
            }
            foreach (var surface in All<CleanableSurface>()) surface.DevelopmentCleanSurface();
            var hamburger = Burger(false); Vector3 center = support.bounds.center;
            View("01-Hamburger", new Vector3(center.x, 1.65f, center.z - .90f), new Vector3(center.x, support.bounds.max.y + .18f, center.z));
            hamburger.gameObject.SetActive(false); // Retain original units, clear the support only for the second runtime staging.
            var cheeseburger = Burger(true);
            View("02-Cheeseburger", new Vector3(center.x, 1.65f, center.z - .90f), new Vector3(center.x, support.bounds.max.y + .18f, center.z));
            cheeseburger.gameObject.SetActive(false);
            var grill = cooker.CleanableSurface;
            for (int i = 0; i < 4; i++)
            {
                grill.DevelopmentCleanSurface(); grill.AddDirt(new[] { 0, .18, .55, .95 }[i], DirtKind.Grease, "Visual capture");
                var b = grill.Support.bounds;
                View("0" + (i + 3) + "-Grill-" + new[] { "Clean", "Used", "Dirty", "Filthy" }[i], new Vector3(b.center.x, 1.65f, b.center.z - .95f), new Vector3(b.center.x, b.max.y, b.center.z));
                receipt.AppendLine("Grill " + grill.State.Category + " " + grill.State.Amount);
            }
            var prep = All<CleanableSurface>().Single(s => s.DisplayName == "Assembly 2"); prep.DevelopmentCleanSurface(); prep.AddDirt(.55, DirtKind.FoodResidue, "Visual capture");
            var pb = prep.Support.bounds;
            View("07-Prep-Dirty", new Vector3(pb.center.x, 1.65f, pb.center.z - .85f), new Vector3(pb.center.x, pb.max.y, pb.center.z));
            var floor = All<CleanableSurface>().Single(s => s.DisplayName == "Floor near Prep"); floor.DevelopmentCleanSurface(); floor.AddDirt(.55, DirtKind.GeneralDirt, "Visual capture");
            Vector3 fc = floor.Support.transform.TransformPoint(floor.LocalCenter + Vector3.up * floor.LocalSize.y * .5f);
            View("08-Floor-Dirty", new Vector3(fc.x, 1.65f, fc.z - .95f), fc);
            receipt.AppendLine("PlayerCamera in Play, eye 1.65 m, same FOV/poses. D3D: " + SystemInfo.graphicsDeviceName + " / " + SystemInfo.graphicsDeviceType);
            receipt.AppendLine("Actual purchases, FoodSimulation/Grill cooking and PhysicalDishAssembly. Runtime-only staging; no scene/asset save.");
            File.WriteAllText("Build/VisualPolish/Captures/Receipt.txt", receipt.ToString()); Debug.Log("Visual polish captures complete.");
        }
        private static void Capture(Camera camera, string name)
        {
            var target = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGBHalf); var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                target.Create(); var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                if (!RenderPipeline.SupportsRenderRequest(camera, request)) throw new InvalidOperationException("URP capture unavailable.");
                RenderPipeline.SubmitRenderRequest(camera, request); RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target; texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); texture.Apply();
                File.WriteAllBytes("Build/VisualPolish/Captures/" + name + ".png", texture.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
