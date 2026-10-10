using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Player;
using ZeroStarRestaurant.Presentation;
using ZeroStarRestaurant.Restaurant;
using ZeroStarRestaurant.Utilities;

namespace ZeroStarRestaurant.Editor
{
    // Runs only in a dedicated batch Editor with graphics; uses the real FPS camera in Play.
    public static class PostM19VisualValidation
    {
        private const string Key = "ZeroStarRestaurant.PostM19.Capture";
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
        private static string Output
        {
            get
            {
                var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "-postM19Phase");
                return "Build/PostM19/" + (i >= 0 ? args[i + 1] : "After");
            }
        }
        private static void Views()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) throw new InvalidOperationException("Graphics required.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            T[] All<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
            foreach (var component in new Component[] { All<RestaurantElectricity>().Single(), All<CustomerServiceLoop>().Single(), All<RestaurantDayController>().Single() })
            { var data = new SerializedObject(component); data.FindProperty("_advanceAutomatically").boolValue = false; data.ApplyModifiedPropertiesWithoutUndo(); }
            All<FoodSimulation>().Single().enabled = false; All<FirstPersonController>().Single().enabled = false;
            var camera = All<Camera>().Single(); var supply = All<RestaurantElectricity>().Single();
            Directory.CreateDirectory(Output);
            string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
            File.WriteAllLines(Output + "/Renderers.tsv", All<MeshRenderer>().Where(r => r.enabled && r.gameObject.activeInHierarchy).Select(r =>
                PathOf(r.transform) + "\t" + r.bounds.min.ToString("F4") + "\t" + r.bounds.max.ToString("F4") + "\t" + r.sharedMaterial?.name));
            void View(string name, Vector3 position, Vector3 target)
            {
                camera.transform.position = position; camera.transform.LookAt(target);
                foreach (bool on in new[] { true, false })
                {
                    supply.SetPower(on); foreach (var fixture in All<PoweredLightFixture>()) fixture.Refresh();
                    Capture(camera, name + (on ? "-ON" : "-OFF"));
                }
            }
            View("01-Kitchen", new Vector3(-4.1f, 1.65f, 3.3f), new Vector3(-.8f, 1.25f, 6.7f));
            View("02-Prep-Assembly", new Vector3(.5f, 1.65f, 4.7f), new Vector3(1.5f, 1f, 6.5f));
            View("03-Grill", new Vector3(-2.3f, 1.65f, 5.2f), new Vector3(-2, .99f, 6.5f));
            View("04-Storage", new Vector3(-3.8f, 1.65f, 4.9f), new Vector3(-6.2f, 1.3f, 5.2f));
            View("05-PASS", new Vector3(-2.6f, 1.65f, 3.6f), new Vector3(0, 1.08f, .5f));
            View("06-Customer-Counter", new Vector3(3.2f, 1.65f, -3.8f), new Vector3(-1, 1.05f, 1));
            View("07-Procurement", new Vector3(-8.45f, 1.65f, 2.4f), new Vector3(-9.7f, 1.1f, .7f));
            View("08-Annex-Floor", new Vector3(-7.6f, 1.65f, 2.8f), new Vector3(-7.3f, 0, 1.2f));
            View("09-Annex-Door-Header", new Vector3(-8.6f, 1.65f, 2.1f), new Vector3(-7.25f, 3.1f, 2.4f));
            View("10-Entrance", new Vector3(-4.2f, 1.65f, -3.6f), new Vector3(-4.75f, 2.4f, -6.25f));
            View("11-Exit", new Vector3(4.2f, 1.65f, -3.6f), new Vector3(4.75f, 2.4f, -6.25f));
            View("12-North-East-Corner", new Vector3(5.7f, 1.65f, 5.8f), new Vector3(6.9f, .1f, 7.15f));
            View("13-Ceiling", new Vector3(-1f, 1.65f, 3.1f), new Vector3(-1f, 3.5f, 5.5f));
            View("14-Divider", new Vector3(2.8f, 1.65f, 3.8f), new Vector3(4.5f, 1f, 5.2f));
            View("15-South-West-Corner", new Vector3(-5.8f, 1.65f, -4.4f), new Vector3(-6.9f, .1f, -5.9f));
            View("16-Annex-Ceiling", new Vector3(-8.5f, 1.65f, 2.8f), new Vector3(-7.3f, 3.5f, 3.4f));
            File.WriteAllText(Output + "/Receipt.txt", "PlayerCamera in Play, 1600x900, unchanged FOV; URP framebuffer, ON/OFF, runtime camera poses only. No scene save.\nGPU: " + SystemInfo.graphicsDeviceName + " / " + SystemInfo.graphicsDeviceType + "\n");
            Debug.Log("Post-M19 player camera captures complete: " + Output);
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
                File.WriteAllBytes(Output + "/" + name + ".png", texture.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
