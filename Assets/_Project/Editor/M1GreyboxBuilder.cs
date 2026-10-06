using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Player;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Editor
{
    public static class M1GreyboxBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/PrototypeRestaurant.unity";
        private const string MaterialFolder = "Assets/_Project/Materials";
        [MenuItem("Zero Star Restaurant/Prototype/Rebuild Greybox Scene")]
        private static void RebuildFromMenu()
        {
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Rebuild prototype greybox",
                    "Replace the saved PrototypeRestaurant scene with the current prototype layout? " +
                    "Custom scene edits will be lost. Material assets are preserved.", "Rebuild", "Cancel"))
                return;
            GenerateScene();
        }

        [MenuItem("Zero Star Restaurant/Prototype/Rebuild Greybox Scene", true)]
        private static bool CanRebuild()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;
        }

        public static void GenerateScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before generating the prototype scene.");
            if (SceneManager.GetSceneByPath(ScenePath).IsValid())
                throw new InvalidOperationException("Close PrototypeRestaurant before rebuilding it. Other open scenes are preserved.");

            // A fresh batch Editor starts with a clean untitled scene. Unity forbids
            // creating an additive scene until that placeholder is replaced/saved.
            if (Application.isBatchMode && SceneManager.sceneCount == 1 &&
                string.IsNullOrEmpty(SceneManager.GetActiveScene().path) && !SceneManager.GetActiveScene().isDirty)
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            for (int index = 0; index < SceneManager.sceneCount; index++)
                if (string.IsNullOrEmpty(SceneManager.GetSceneAt(index).path))
                    throw new InvalidOperationException("Save or close untitled scenes before generating the greybox. No scene will be discarded.");

            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (actions == null || actions.FindAction("Player/Move") == null ||
                actions.FindAction("Player/Look") == null || actions.FindAction("Player/Jump") == null ||
                actions.FindAction("Player/Interact") == null || actions.FindAction("Player/Drop") == null ||
                actions.FindAction("Player/Throw") == null)
                throw new InvalidOperationException("The existing Player input actions are required.");

            Material floor = GetOrCreateMaterial("GreyboxFloor", new Color(0.25f, 0.27f, 0.29f));
            Material wall = GetOrCreateMaterial("GreyboxWall", new Color(0.65f, 0.67f, 0.68f));
            Material volume = GetOrCreateMaterial("GreyboxVolume", new Color(0.40f, 0.46f, 0.48f));
            Scene previousScene = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var environment = new GameObject("Greybox");
                // Interior: 14 x 12 metres, open top for neutral development lighting.
                CreateBox(environment.transform, "Floor", new Vector3(0f, -0.25f, 0f), new Vector3(14f, 0.5f, 12f), floor);
                CreateBox(environment.transform, "WallWest", new Vector3(-7.25f, 1.75f, 0f), new Vector3(0.5f, 3.5f, 13f), wall);
                CreateBox(environment.transform, "WallEast", new Vector3(7.25f, 1.75f, 0f), new Vector3(0.5f, 3.5f, 13f), wall);
                CreateBox(environment.transform, "WallNorth", new Vector3(0f, 1.75f, 6.25f), new Vector3(14f, 3.5f, 0.5f), wall);
                CreateBox(environment.transform, "WallSouth", new Vector3(0f, 1.75f, -6.25f), new Vector3(14f, 3.5f, 0.5f), wall);
                CreateBox(environment.transform, "KitchenDivider", new Vector3(1f, 1.75f, 3.75f), new Vector3(0.3f, 3.5f, 4.5f), wall);
                CreateBox(environment.transform, "ServiceCounterVolume", new Vector3(0f, 0.55f, 0.5f), new Vector3(4f, 1.1f, 0.8f), volume);
                CreateBox(environment.transform, "WorktopVolume", new Vector3(5.5f, 0.45f, 3.5f), new Vector3(1.2f, 0.9f, 3f), volume);
                CreateBox(environment.transform, "TableVolume", new Vector3(-4f, 0.4f, 2.5f), new Vector3(1.5f, 0.8f, 1.5f), volume);
                // A low step and tall blocker make gravity, jump and collisions easy to check.
                CreateBox(environment.transform, "LowStep", new Vector3(-4f, 0.1f, -1f), new Vector3(1.5f, 0.2f, 1f), volume);
                CreateBox(environment.transform, "TallBlocker", new Vector3(4f, 1.5f, -2f), new Vector3(1.2f, 3f, 1.2f), volume);

                var player = new GameObject("Player");
                player.transform.position = new Vector3(0f, 0.05f, -4.5f);
                var capsule = player.AddComponent<CharacterController>();
                capsule.height = 1.8f;
                capsule.radius = 0.3f;
                capsule.center = new Vector3(0f, 0.9f, 0f);
                capsule.stepOffset = 0.25f;
                capsule.slopeLimit = 45f;
                capsule.skinWidth = 0.03f;
                capsule.minMoveDistance = 0f;
                var cameraObject = new GameObject("PlayerCamera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                cameraObject.transform.SetParent(player.transform, false);
                cameraObject.transform.localPosition = new Vector3(0f, 1.65f, 0f);
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.fieldOfView = 75f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 100f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.3f, 0.35f, 0.4f);
                cameraObject.AddComponent<UniversalAdditionalCameraData>();
                FirstPersonController controller = player.AddComponent<FirstPersonController>();
                var serialized = new SerializedObject(controller);
                serialized.FindProperty("_inputActions").objectReferenceValue = actions;
                serialized.FindProperty("_viewTransform").objectReferenceValue = cameraObject.transform;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PhysicalCarry carry = player.AddComponent<PhysicalCarry>();
                Wire(carry, "_viewTransform", cameraObject.transform, "_actorRoot", player.transform);
                InteractionDetector detector = player.AddComponent<InteractionDetector>();
                Wire(detector, "_origin", cameraObject.transform, "_actorRoot", player.transform);
                PlayerInteraction interaction = player.AddComponent<PlayerInteraction>();
                var interactionData = new SerializedObject(interaction);
                interactionData.FindProperty("_detector").objectReferenceValue = detector;
                interactionData.FindProperty("_carry").objectReferenceValue = carry;
                interactionData.FindProperty("_player").objectReferenceValue = controller;
                interactionData.ApplyModifiedPropertiesWithoutUndo();
                InteractionInput input = player.AddComponent<InteractionInput>();
                Wire(input, "_inputActions", actions, "_interaction", interaction);
                InteractionFeedback feedback = player.AddComponent<InteractionFeedback>();
                Wire(feedback, "_interaction", interaction, "_input", input);

                var pickups = new GameObject("PhysicalTestObjects");
                CreatePickup(pickups.transform, "Light box (0.5 kg)", -1.8f, 0.45f, 0.5f,
                    GetOrCreateMaterial("PickupLight", new Color(0.8f, 0.35f, 0.15f)));
                CreatePickup(pickups.transform, "Heavy box (12 kg)", -0.6f, 0.45f, 12f,
                    GetOrCreateMaterial("PickupHeavy", new Color(0.45f, 0.2f, 0.55f)));
                CreatePickup(pickups.transform, "Small box (0.25 kg)", 0.6f, 0.25f, 0.25f,
                    GetOrCreateMaterial("PickupSmall", new Color(0.15f, 0.5f, 0.8f)));
                CreatePickup(pickups.transform, "Large box (4 kg)", 1.8f, 0.9f, 4f,
                    GetOrCreateMaterial("PickupLarge", new Color(0.8f, 0.7f, 0.2f)));

                var lightObject = new GameObject("DevelopmentSun", typeof(Light));
                lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                Light light = lightObject.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.5f;
                light.shadows = LightShadows.Soft;
                RenderSettings.sun = light;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
                RenderSettings.skybox = null;
                RenderSettings.fog = false;

                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new InvalidOperationException("Could not save the prototype scene.");
                Debug.Log("Prototype greybox saved: " + ScenePath);
            }
            finally
            {
                if (previousScene.IsValid() && previousScene.isLoaded)
                    SceneManager.SetActiveScene(previousScene);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("The installed URP/Lit shader was not found.");
            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0f);
            material.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject CreateBox(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = scale;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
            return box;
        }

        private static void Wire(UnityEngine.Object component, string firstField, UnityEngine.Object first,
            string secondField, UnityEngine.Object second)
        {
            var serialized = new SerializedObject(component);
            serialized.FindProperty(firstField).objectReferenceValue = first;
            serialized.FindProperty(secondField).objectReferenceValue = second;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreatePickup(Transform parent, string name, float x, float size, float mass, Material material)
        {
            GameObject item = CreateBox(parent, name, new Vector3(x, size * 0.5f + 0.1f, -2.4f), Vector3.one * size, material);
            Rigidbody body = item.AddComponent<Rigidbody>();
            body.mass = mass;
            body.linearDamping = 0.05f;
            body.angularDamping = 0.25f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.maxLinearVelocity = 20f;
            body.maxAngularVelocity = 10f;
            body.solverIterations = 12;
            item.AddComponent<Pickup>();
        }
    }
}
