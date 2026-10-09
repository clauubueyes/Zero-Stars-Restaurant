using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
    // Batch-only development capture of the actual M12-powered scene, never saves a scene.
    public static class VP1AVisualValidation
    {
        private static int _frames;
        private const string PendingKey = "ZeroStarRestaurant.VP1ACapturePending";

        [InitializeOnLoadMethod]
        private static void ResumeAfterReload()
        {
            if (!Application.isBatchMode || !SessionState.GetBool(PendingKey, false)) return;
            EditorApplication.playModeStateChanged -= PlayChanged;
            EditorApplication.playModeStateChanged += PlayChanged;
        }

        public static void Run()
        {
            if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Run VP1A captures in a separate batch invocation with graphics, outside Play.");
            EditorSceneManager.OpenScene(M1GreyboxBuilder.ScenePath);
            SessionState.SetBool(PendingKey, true); SessionState.SetInt(PendingKey + "ExitCode", 0);
            EditorApplication.playModeStateChanged += PlayChanged;
            EditorApplication.EnterPlaymode();
        }

        private static void PlayChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode) { _frames = 0; EditorApplication.update += CaptureWhenReady; }
            if (change == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.playModeStateChanged -= PlayChanged;
                SessionState.SetBool(PendingKey, false);
                EditorApplication.Exit(SessionState.GetInt(PendingKey + "ExitCode", 1));
            }
        }

        private static void CaptureWhenReady()
        {
            if (++_frames < 20) return;
            EditorApplication.update -= CaptureWhenReady;
            try { CaptureViews(); }
            catch (Exception exception) { Debug.LogException(exception); SessionState.SetInt(PendingKey + "ExitCode", 1); }
            finally { EditorApplication.ExitPlaymode(); }
        }

        private static void CaptureViews()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) throw new InvalidOperationException("A graphics device is required for visual validation.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            T[] All<T>() where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
            foreach (var component in new Component[] { All<RestaurantElectricity>().Single(), All<CustomerServiceLoop>().Single(), All<RestaurantDayController>().Single() })
            { var data = new SerializedObject(component); data.FindProperty("_advanceAutomatically").boolValue = false; data.ApplyModifiedPropertiesWithoutUndo(); }
            All<FoodSimulation>().Single().enabled = false; All<FirstPersonController>().Single().enabled = false;
            var camera = All<Camera>().Single(); var atmosphere = All<RestaurantAtmosphere>().Single();
            var supply = All<RestaurantElectricity>().Single();
            var fixtures = All<PoweredLightFixture>();
            Directory.CreateDirectory("Build/VP1A/Captures");
            void Power(bool on) { supply.SetPower(on); foreach (var fixture in fixtures) fixture.Refresh(); }
            camera.transform.LookAt(new Vector3(-.6f, 1.3f, 6.3f));
            Power(true); atmosphere.AtmosphereOn(); Capture(camera, "Kitchen-Atmosphere-PowerOn");
            Power(false); Capture(camera, "Kitchen-Atmosphere-PowerOff");
            atmosphere.AtmosphereOff(); Capture(camera, "Kitchen-Reference");
            atmosphere.AtmosphereOn(); Power(true);
            camera.transform.SetPositionAndRotation(new Vector3(3.2f, 1.65f, -3.8f), Quaternion.identity);
            camera.transform.LookAt(new Vector3(-1, 1.05f, 1.0f)); Capture(camera, "Customers-Atmosphere-PowerOn");
            atmosphere.AtmosphereOff(); Capture(camera, "Customers-Reference");
            atmosphere.AtmosphereOn();
            camera.transform.position = new Vector3(-8.45f, 1.65f, 2.4f);
            camera.transform.LookAt(new Vector3(-9.7f, 1.1f, .7f)); Capture(camera, "Procurement-Atmosphere-PowerOn");
            var station = All<IngredientPurchaseStation>().Single();
            var grillBounds = All<CleanableSurface>().Single(surface => surface.DisplayName == "Grill").Support.bounds;
            var assemblyBounds = All<CleanableSurface>().Single(surface => surface.DisplayName == "Assembly 2").Support.bounds;
            for (int product = 0; product < 3; product++)
            {
                if (!station.TryPurchase(product, out var food)) throw new InvalidOperationException("Could not prepare real purchased food for readability capture.");
                var support = product == 1 ? grillBounds : assemblyBounds;
                var point = support.center; point.x += product == 2 ? .35f : 0;
                point.y = support.max.y + food.GetComponent<Collider>().bounds.extents.y + .003f;
                var body = food.GetComponent<Rigidbody>(); body.interpolation = RigidbodyInterpolation.None;
                food.transform.position = point; body.position = point; Physics.SyncTransforms();
            }
            camera.transform.position = new Vector3(-2.4f, 1.65f, 5.0f);
            camera.transform.LookAt(new Vector3(-.5f, .98f, 6.55f)); Capture(camera, "CookingReadability-PowerOn");
            All<CustomerServiceLoop>().Single().Advance(30);
            camera.transform.position = new Vector3(3.2f, 1.65f, -3.8f);
            camera.transform.LookAt(new Vector3(-1, 1.05f, .5f)); Capture(camera, "Customers-Service-PowerOn");
            Debug.Log("VP1A captures complete. GPU: " + SystemInfo.graphicsDeviceName + "; 7 powered spots + 2 exterior spots, 4 shadow casters, no bake.");
        }

        private static void Capture(Camera camera, string name)
        {
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGBHalf);
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                target.Create();
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                if (!RenderPipeline.SupportsRenderRequest(camera, request)) throw new InvalidOperationException("URP single camera capture is not available.");
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target; texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
                File.WriteAllBytes("Build/VP1A/Captures/" + name + ".png", texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; target.Release();
                UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
