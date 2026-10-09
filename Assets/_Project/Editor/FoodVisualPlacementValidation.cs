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
using ZeroStarRestaurant.Interaction;
using ZeroStarRestaurant.Player;
using ZeroStarRestaurant.Presentation;
using ZeroStarRestaurant.Restaurant;
using ZeroStarRestaurant.Utilities;

namespace ZeroStarRestaurant.Editor
{
    // Dedicated Play capture: real purchase, carry, assisted placement, physics and finalization APIs.
    public static class FoodVisualPlacementValidation
    {
        private const string Key = "ZeroStarRestaurant.PlacementCapture";
        private static int _frames;
        [InitializeOnLoadMethod] private static void Resume()
        {
            if (!Application.isBatchMode || !SessionState.GetBool(Key, false)) return;
            EditorApplication.playModeStateChanged -= Changed; EditorApplication.playModeStateChanged += Changed;
        }
        public static void Run()
        {
            if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Dedicated graphics batch required.");
            SessionState.SetString(Key + "Phase", Environment.GetEnvironmentVariable("ZSR_PLACEMENT_PHASE") ?? "After");
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
            if (++_frames < 25) return; EditorApplication.update -= Ready;
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
            All<FoodSimulation>().Single().enabled = false; All<FirstPersonController>().Single().enabled = false;
            All<RestaurantElectricity>().Single().PowerOn(); foreach (var fixture in All<PoweredLightFixture>()) fixture.Refresh();
            All<CustomerServiceLoop>().Single().Ledger.TryRecord(Guid.NewGuid(), Guid.NewGuid(), 10000, true); // Capture-only budget, no saved economy change.
            var camera = All<Camera>().Single(); var actor = All<FirstPersonController>().Single().transform;
            var carry = All<PhysicalCarry>().Single(); var assembly = All<PhysicalDishAssembly>().Single(); var station = All<IngredientPurchaseStation>().Single();
            var output = (Transform)new SerializedObject(station).FindProperty("_output").objectReferenceValue;
            var marker = All<Transform>().Single(t => t.name == "OutputMarker").Find("Visual_VP1BC/Cladding").GetComponent<Renderer>();
            string phase = SessionState.GetString(Key + "Phase", "After"), folder = "Build/FoodVisualPlacement/Captures/" + phase;
            Directory.CreateDirectory(folder); var receipt = new System.Text.StringBuilder("Real PlayerCamera, Play, original light/art. Temporary staging only.\n");
            var mode = Physics.simulationMode; Physics.simulationMode = SimulationMode.Script;
            void Refresh() { foreach (var v in All<FoodStageVisual>()) v.Refresh(); foreach (var v in All<BurgerIngredientVisual>()) v.Refresh(); }
            void SetEye(Vector3 eye, Vector3 target) { actor.position += eye - camera.transform.position; camera.transform.LookAt(target); Physics.SyncTransforms(); }
            void Settle() { for (int i = 0; i < 45; i++) { Physics.SyncTransforms(); Physics.Simulate(.02f); } Refresh(); }
            Bounds Visual(FoodItem f) { var rs = f.transform.Find("Visual_VP1BC").GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b; }
            void Shot(string name, Vector3 center, float targetHeight = .12f, bool procurement = false)
            {
                SetEye(new Vector3(center.x, 1.65f, center.z + (procurement ? .9f : -.9f)), center + Vector3.up * targetHeight); Refresh();
                Capture(camera, folder + "/" + name + ".png");
            }
            FoodItem Buy(int product) { if (!station.TryPurchase(product, out var f)) throw new InvalidOperationException(station.LastMessage); return f; }
            void Place(FoodItem food, Collider destination)
            {
                Vector3 p = food.GetComponent<BoxCollider>().bounds.center; SetEye(new Vector3(p.x, 1.65f, p.z + .9f), p);
                if (!carry.TryPickUp(food.GetComponent<Pickup>())) throw new InvalidOperationException("Pickup failed: " + food.name);
                Vector3 center = destination.bounds.center; food.GetComponent<Rigidbody>().position = new Vector3(center.x, destination.bounds.max.y + .5f, center.z);
                food.transform.position = food.GetComponent<Rigidbody>().position; Physics.SyncTransforms();
                if (!destination.Raycast(new Ray(new Vector3(center.x, destination.bounds.max.y + 1, center.z), Vector3.down), out var hit, 2) ||
                    !assembly.TryPlaceHeld(carry, hit, 1)) throw new InvalidOperationException("Assisted placement failed.");
                Settle();
            }
            try
            {
                foreach (int product in new[] { 2, 0, 1 })
                {
                    var f = Buy(product); Refresh(); string label = new[] { "Bun", "Patty", "Cheese" }[product];
                    Shot("Collect-" + label + "-Immediate", marker.bounds.center, procurement: true);
                    Settle(); Shot("Collect-" + label + "-Settled", marker.bounds.center, procurement: true);
                    Bounds b = Visual(f); receipt.AppendLine(label + " collector visual min=" + b.min.y + " marker top=" + marker.bounds.max.y + " proxy bottom=" + f.GetComponent<BoxCollider>().bounds.min.y + " state=" + f.State.InstanceId);
                    File.WriteAllText(folder + "/Receipt.txt", receipt.ToString());
                    SetEye(new Vector3(output.position.x, 1.65f, output.position.z + .9f), f.transform.position);
                    if (!carry.TryPickUp(f.GetComponent<Pickup>())) throw new InvalidOperationException("Purchased unit not pickable.");
                    carry.Drop(); f.gameObject.SetActive(false); Physics.SyncTransforms();
                }
                foreach (bool cheese in new[] { true, false })
                {
                    if (!station.TryPurchasePlate(out var plate)) throw new InvalidOperationException(station.LastMessage);
                    var prep = All<CleanableSurface>().Single(s => s.DisplayName == "Assembly 3").Support;
                    var pb = plate.GetComponent<BoxCollider>(); var body = plate.GetComponent<Rigidbody>();
                    body.position = new Vector3(prep.bounds.center.x, prep.bounds.max.y + pb.bounds.extents.y + .004f, prep.bounds.center.z); plate.transform.position = body.position; Settle();
                    var units = new System.Collections.Generic.List<FoodItem>(); Collider support = pb;
                    string prefix = cheese ? "Cheeseburger" : "Hamburger";
                    foreach (int product in cheese ? new[] { 0, 1, 2, 0 } : new[] { 0, 1, 0 })
                    {
                        var f = Buy(product); Place(f, support); units.Add(f); support = f.GetComponent<BoxCollider>();
                        Shot(prefix + "-Step" + units.Count, prep.bounds.center + Vector3.up * prep.bounds.extents.y);
                        receipt.AppendLine(prefix + " step " + units.Count + " " + f.State.InstanceId + " visual min=" + Visual(f).min.y + " max=" + Visual(f).max.y);
                    }
                    var positions = units.Select(u => Visual(u).center).ToArray();
                    if (!assembly.TryFinalize(units.Last(), out var dish)) throw new InvalidOperationException("Finalization failed."); Refresh();
                    Shot(prefix + "-Final", prep.bounds.center + Vector3.up * prep.bounds.extents.y);
                    receipt.AppendLine(prefix + " F max visual jump=" + units.Select((u, i) => Vector3.Distance(positions[i], Visual(u).center)).Max());
                    dish.gameObject.SetActive(false); Physics.SyncTransforms();
                }
                if (phase == "After")
                {
                    var prep = All<CleanableSurface>().Single(s => s.DisplayName == "Assembly 3").Support;
                    var grill = All<GrillHeatSource>().Single();
                    foreach (string scenario in new[] { "Partial-Prep", "Patty-Plate", "Cheeseburger-Prep", "Patty-Bun-Grill", "Cheeseburger-Grill" })
                    {
                        Collider support = scenario.Contains("Grill") ? grill.CleanableSurface.Support : prep;
                        Vector3 center = support.bounds.center + Vector3.up * support.bounds.extents.y;
                        if (scenario == "Patty-Plate")
                        {
                            if (!station.TryPurchasePlate(out var plate)) throw new InvalidOperationException(station.LastMessage);
                            var box = plate.GetComponent<BoxCollider>(); var body = plate.GetComponent<Rigidbody>();
                            body.position = new Vector3(center.x, center.y + box.bounds.extents.y + .004f, center.z); plate.transform.position = body.position; Settle(); support = box;
                        }
                        int[] sequence = scenario == "Partial-Prep" ? new[] { 0, 1 } : scenario == "Patty-Plate" ? new[] { 1 } : scenario == "Patty-Bun-Grill" ? new[] { 1, 0 } : new[] { 0, 1, 2, 0 };
                        var units = new System.Collections.Generic.List<FoodItem>();
                        foreach (int product in sequence)
                        {
                            var food = Buy(product); Place(food, support); units.Add(food); support = food.GetComponent<BoxCollider>();
                            if (scenario == "Patty-Bun-Grill" && units.Count == 1)
                            {
                                All<FoodSimulation>().Single().Advance(30); Refresh();
                                receipt.AppendLine("Grill original Patty temperature=" + food.State.TemperatureCelsius + " dose=" + food.State.Cooking.EquivalentSeconds);
                            }
                            Shot(scenario + "-Step" + units.Count, center);
                        }
                        var before = units.Select(u => Visual(u).center).ToArray();
                        if (!assembly.TryFinalize(units.Last(), out var dish)) throw new InvalidOperationException("Finalization failed: " + scenario);
                        Refresh(); Shot(scenario + "-Final", center);
                        receipt.AppendLine(scenario + " F max visual jump=" + units.Select((u, i) => Vector3.Distance(before[i], Visual(u).center)).Max());
                        dish.gameObject.SetActive(false); Physics.SyncTransforms();
                    }
                }
                File.WriteAllText(folder + "/Receipt.txt", receipt.ToString()); Debug.Log("Food visual placement " + phase + " captures complete.");
            }
            finally { carry.Drop(); Physics.simulationMode = mode; }
        }
        private static void Capture(Camera camera, string path)
        {
            var target = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGBHalf); var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                target.Create(); var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                RenderPipeline.SubmitRenderRequest(camera, request); RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target; texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
