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
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Hygiene;
using ZeroStarRestaurant.Player;
using ZeroStarRestaurant.Presentation;
using ZeroStarRestaurant.Restaurant;
using ZeroStarRestaurant.Utilities;

namespace ZeroStarRestaurant.Editor
{
    // Runs only in a dedicated batch Editor with graphics; uses the real FPS camera in Play.
    public static class VP1BCVisualValidation
    {
        private const string Key = "ZeroStarRestaurant.VP1BC.Capture";
        private static int _frames;
        [InitializeOnLoadMethod]
        private static void Resume()
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
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) throw new InvalidOperationException("Captures require a graphics device.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            T[] All<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
            foreach (var component in new Component[] { All<RestaurantElectricity>().Single(), All<CustomerServiceLoop>().Single(), All<RestaurantDayController>().Single() })
            { var data = new SerializedObject(component); data.FindProperty("_advanceAutomatically").boolValue = false; data.ApplyModifiedPropertiesWithoutUndo(); }
            var simulation = All<FoodSimulation>().Single(); simulation.enabled = false; All<FirstPersonController>().Single().enabled = false;
            var camera = All<Camera>().Single(); var art = All<RestaurantArtPass>().Single();
            var supply = All<RestaurantElectricity>().Single();
            void Power(bool on) { supply.SetPower(on); foreach (var fixture in All<PoweredLightFixture>()) fixture.Refresh(); }
            Directory.CreateDirectory("Build/VP1BC/Captures");
            void View(string name, Vector3 position, Vector3 target, GameObject[] comparisonUnits = null)
            {
                camera.transform.position = position; camera.transform.LookAt(target);
                art.ArtOn(); Capture(camera, name + "-Art");
                // Purchased prefabs are outside the scene comparison registry. Restore their original
                // renderers only for this staged reference frame; the same live FoodState stays intact.
                var unitsForReference = comparisonUnits ?? Array.Empty<GameObject>();
                var shells = unitsForReference.Select(u => u.transform.Find("Visual_VP1BC").gameObject).ToArray();
                var renderers = unitsForReference.Select(u => u.GetComponent<Renderer>()).ToArray();
                var active = shells.Select(s => s.activeSelf).ToArray();
                var enabled = renderers.Select(r => r.enabled).ToArray();
                art.ArtOff();
                try
                {
                    for (int i = 0; i < shells.Length; i++) { shells[i].SetActive(false); renderers[i].enabled = true; }
                    Capture(camera, name + "-VP1A-Before");
                }
                finally
                {
                    for (int i = 0; i < shells.Length; i++) { shells[i].SetActive(active[i]); renderers[i].enabled = enabled[i]; }
                    art.ArtOn();
                }
            }
            Power(true);
            View("01-Kitchen-Grill-Prep", new Vector3(-4.1f, 1.65f, 3.3f), new Vector3(-.8f, 1.25f, 6.7f));
            View("02-Kitchen-PASS", new Vector3(-2.6f, 1.65f, 3.6f), new Vector3(0, 1.08f, .5f));
            All<CustomerServiceLoop>().Single().Advance(30);
            View("03-Customers-Counter", new Vector3(3.2f, 1.65f, -3.8f), new Vector3(-1, 1.05f, 1));
            View("04-ColdStorage", new Vector3(-3.8f, 1.65f, 4.9f), new Vector3(-6.2f, 1.3f, 5.2f));
            Power(false); View("05-Power-OFF", new Vector3(-4.1f, 1.65f, 3.3f), new Vector3(-.8f, 1.25f, 6.7f)); Power(true);
            View("06-Grill-Close", new Vector3(-2.3f, 1.65f, 5.2f), new Vector3(-2, .99f, 6.5f));
            View("08-Procurement", new Vector3(-8.45f, 1.65f, 2.4f), new Vector3(-9.7f, 1.1f, .7f));
            var station = All<IngredientPurchaseStation>().Single();
            var grill = All<CleanableSurface>().Single(s => s.DisplayName == "Grill").Support.bounds;
            var prep = All<CleanableSurface>().Single(s => s.DisplayName == "Assembly 2").Support.bounds;
            var appliance = All<ElectricalAppliance>().Single(a => a.ThermalSource is GrillHeatSource); appliance.SetOn(true);
            void Place(FoodItem food, Vector3 point)
            {
                var body = food.GetComponent<Rigidbody>(); body.interpolation = RigidbodyInterpolation.None; body.isKinematic = true;
                point.y += food.GetComponent<BoxCollider>().bounds.extents.y + .001f;
                food.transform.position = point; body.position = point; Physics.SyncTransforms();
            }
            var units = new FoodItem[3];
            for (int i = 0; i < 3; i++)
            {
                if (!station.TryPurchase(1, out var food)) throw new InvalidOperationException("Real Patty purchase failed: " + station.LastMessage);
                units[i] = food;
                Place(food, new Vector3(grill.center.x, grill.max.y, grill.center.z));
                if (i > 0)
                {
                    var desired = i == 1 ? CookingStage.Cooked : CookingStage.Burnt;
                    int steps = 0; while (food.State.Cooking.Stage < desired && ++steps < 800) simulation.Advance(.25);
                    if (food.State.Cooking.Stage != desired) throw new InvalidOperationException("Real grill did not reach capture stage.");
                }
                Place(food, new Vector3(prep.center.x - .45f + i * .45f, prep.max.y, prep.center.z - .12f));
                food.GetComponentInChildren<FoodStageVisual>().Refresh();
            }
            View("07-Raw-Cooked-Burnt-Patty", new Vector3(prep.center.x, 1.65f, prep.center.z - 1.1f), new Vector3(prep.center.x, prep.max.y + .05f, prep.center.z), units.Select(f => f.gameObject).ToArray());
            if (!station.TryPurchase(0, out var bun)) throw new InvalidOperationException("Bun purchase failed.");
            Place(bun, new Vector3(prep.center.x - .3f, prep.max.y, prep.center.z + .40f));
            if (!station.TryPurchase(2, out var cheese)) throw new InvalidOperationException("Cheese purchase failed.");
            Place(cheese, new Vector3(prep.center.x + .3f, prep.max.y, prep.center.z + .40f));
            if (!station.TryPurchasePlate(out var plate)) throw new InvalidOperationException("Plate purchase failed.");
            var pb = plate.GetComponent<Rigidbody>(); pb.isKinematic = true; plate.transform.position = new Vector3(prep.center.x + .8f, prep.max.y + .025f, prep.center.z + .20f); pb.position = plate.transform.position;
            var foodAndPlate = units.Select(f => f.gameObject).Concat(new[] { bun.gameObject, cheese.gameObject, plate.gameObject }).ToArray();
            View("09-Food-First-Pass", new Vector3(prep.center.x, 1.65f, prep.center.z - 1.2f), new Vector3(prep.center.x, prep.max.y + .05f, prep.center.z + .15f), foodAndPlate);
            var dirty = All<CleanableSurface>().Single(s => s.DisplayName == "Grill"); dirty.DevelopmentMakeFilthy();
            foreach (var view in All<DirtSurfaceView>()) view.Refresh();
            View("10-M17-Dynamic-Dirt", new Vector3(-2.3f, 1.65f, 5.2f), new Vector3(-2, .99f, 6.5f), foodAndPlate);
            File.WriteAllText("Build/VP1BC/Captures/Receipt.txt", "Actual PlayerCamera in Play, 1600x900, existing FOV. Runtime-only staged positions; no scene/asset save.\nGPU: " + SystemInfo.graphicsDeviceName + " / " + SystemInfo.graphicsDeviceType + "\nThree purchased original units cooked by FoodSimulation + original Grill: " +
                string.Join(", ", units.Select(f => f.State.InstanceId + "=" + f.State.Cooking.Stage)) + "\n");
            Debug.Log("VP1BC gameplay camera captures complete: " + SystemInfo.graphicsDeviceName);
        }
        private static void Capture(Camera camera, string name)
        {
            var target = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGBHalf); var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                target.Create(); var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                if (!RenderPipeline.SupportsRenderRequest(camera, request)) throw new InvalidOperationException("URP capture unavailable.");
                // Warm the same camera/volume/shadow state before reading a manually submitted frame.
                RenderPipeline.SubmitRenderRequest(camera, request); RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target; texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); texture.Apply();
                File.WriteAllBytes("Build/VP1BC/Captures/" + name + ".png", texture.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
