using System;
using System.Collections.Generic;
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
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Orders;

namespace ZeroStarRestaurant.Editor
{
    public static class M1GreyboxBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/PrototypeRestaurant.unity";
        private const string MaterialFolder = "Assets/_Project/Materials";
        private const string FoodDefinitionFolder = "Assets/_Project/ScriptableObjects/Food";
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
                actions.FindAction("Player/Throw") == null || actions.FindAction("Player/FinalizeDish") == null ||
                actions.FindAction("Player/PlaceIngredient") == null)
                throw new InvalidOperationException("The existing Player input actions are required.");

            Material floor = GetOrCreateMaterial("GreyboxFloor", new Color(0.25f, 0.27f, 0.29f));
            Material wall = GetOrCreateMaterial("GreyboxWall", new Color(0.65f, 0.67f, 0.68f));
            Material volume = GetOrCreateMaterial("GreyboxVolume", new Color(0.40f, 0.46f, 0.48f));
            FoodDefinition beef = GetOrCreateFoodDefinition("RawBeefPatty", "food.raw_beef_patty", "Raw Beef Patty",
                FoodCategory.Meat, 80, 600f, 60f, cookable: true);
            FoodDefinition bun = GetOrCreateFoodDefinition("Bun", "food.bun", "Bun",
                FoodCategory.Bakery, 35, 1800f, 90f);
            FoodDefinition cheese = GetOrCreateFoodDefinition("Cheese", "food.cheese", "Cheese",
                FoodCategory.Dairy, 25, 1200f, 45f);
            FoodPreservationSettings preservation = GetOrCreatePreservationSettings();
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
                CreateBox(environment.transform, "KitchenDivider", new Vector3(4.5f, 1.75f, 3.75f), new Vector3(0.3f, 3.5f, 4.5f), wall);
                CreateBox(environment.transform, "ServiceCounterVolume", new Vector3(0f, 0.55f, 0.5f), new Vector3(4f, 1.1f, 0.8f), volume);
                CreateBox(environment.transform, "WorktopVolume", new Vector3(5.5f, 0.45f, 3.5f), new Vector3(1.2f, 0.9f, 3f), volume);
                CreateBox(environment.transform, "TableVolume", new Vector3(4f, 0.4f, -4f), new Vector3(1.5f, 0.8f, 1.5f), volume);
                // A low step and tall blocker make gravity, jump and collisions easy to check.
                CreateBox(environment.transform, "LowStep", new Vector3(4f, 0.1f, -0.8f), new Vector3(1.5f, 0.2f, 1f), volume);
                CreateBox(environment.transform, "TallBlocker", new Vector3(5.5f, 1.5f, -2f), new Vector3(1.2f, 3f, 1.2f), volume);

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
                FoodInspectionFeedback foodFeedback = player.AddComponent<FoodInspectionFeedback>();
                Wire(foodFeedback, "_interaction", interaction, "_carry", carry);
                DishInspectionFeedback dishFeedback = player.AddComponent<DishInspectionFeedback>();
                DishAssemblyInteraction assemblyInput = player.AddComponent<DishAssemblyInteraction>();
                var assemblyInputData = new SerializedObject(assemblyInput);
                assemblyInputData.FindProperty("_interaction").objectReferenceValue = interaction;
                assemblyInputData.FindProperty("_detector").objectReferenceValue = detector;
                assemblyInputData.FindProperty("_carry").objectReferenceValue = carry;
                assemblyInputData.FindProperty("_inputActions").objectReferenceValue = actions;
                assemblyInputData.ApplyModifiedPropertiesWithoutUndo();
                var dishFeedbackData = new SerializedObject(dishFeedback);
                dishFeedbackData.FindProperty("_interaction").objectReferenceValue = interaction;
                dishFeedbackData.FindProperty("_detector").objectReferenceValue = detector;
                dishFeedbackData.FindProperty("_carry").objectReferenceValue = carry;
                dishFeedbackData.FindProperty("_assembly").objectReferenceValue = assemblyInput;
                dishFeedbackData.ApplyModifiedPropertiesWithoutUndo();

                var pickups = new GameObject("PhysicalTestObjects");
                CreatePickup(pickups.transform, "Light box (0.5 kg)", -1.8f, 0.45f, 0.5f,
                    GetOrCreateMaterial("PickupLight", new Color(0.8f, 0.35f, 0.15f)));
                CreatePickup(pickups.transform, "Heavy box (12 kg)", -0.6f, 0.45f, 12f,
                    GetOrCreateMaterial("PickupHeavy", new Color(0.45f, 0.2f, 0.55f)));
                CreatePickup(pickups.transform, "Small box (0.25 kg)", 0.6f, 0.25f, 0.25f,
                    GetOrCreateMaterial("PickupSmall", new Color(0.15f, 0.5f, 0.8f)));
                CreatePickup(pickups.transform, "Large box (4 kg)", 1.8f, 0.9f, 4f,
                    GetOrCreateMaterial("PickupLarge", new Color(0.8f, 0.7f, 0.2f)));

                var foodZone = new GameObject("FoodTestZone");
                CreateBox(foodZone.transform, "FoodTestBench", new Vector3(-4f, 0.4f, -3.5f),
                    new Vector3(2.4f, 0.8f, 1.2f), volume);
                Material beefMaterial = GetOrCreateMaterial("FoodBeef", new Color(0.58f, 0.2f, 0.2f));
                Material bunMaterial = GetOrCreateMaterial("FoodBun", new Color(0.73f, 0.5f, 0.25f));
                Material cheeseMaterial = GetOrCreateMaterial("FoodCheese", new Color(0.9f, 0.75f, 0.3f));
                var foods = new List<FoodItem>
                {
                    CreateFood(foodZone.transform, "Raw Beef Patty - Fresh fixture", -4.8f, new Vector3(0.4f, 0.12f, 0.4f),
                        0.15f, beefMaterial, beef, 0f, 100f, 21f, false),
                    CreateFood(foodZone.transform, "Raw Beef Patty - Aged fixture", -4.3f, new Vector3(0.4f, 0.12f, 0.4f),
                        0.15f, beefMaterial, beef, 564f, 6f, 21f, false),
                    CreateFood(foodZone.transform, "Bun - Cold fixture", -3.8f, new Vector3(0.4f, 0.22f, 0.4f),
                        0.08f, bunMaterial, bun, 0f, 100f, 5f, false),
                    CreateFood(foodZone.transform, "Cheese - Contaminated fixture", -3.3f, new Vector3(0.32f, 0.06f, 0.32f),
                        0.025f, cheeseMaterial, cheese, 312f, 74f, 22f, true)
                };
                var cookingZone = new GameObject("CookingTestZone");
                var grill = new GameObject("GrillStation");
                grill.transform.SetParent(cookingZone.transform, false);
                grill.transform.localPosition = new Vector3(-2.5f, 0f, 3.7f);
                CreateBox(grill.transform, "GrillBase", new Vector3(0f, 0.45f, 0f),
                    new Vector3(2.4f, 0.9f, 1.2f), volume);
                CreateBox(grill.transform, "GrillHotSurface", new Vector3(0f, 0.95f, 0f),
                    new Vector3(2.4f, 0.1f, 1.2f),
                    GetOrCreateMaterial("GrillSurface", new Color(0.65f, 0.22f, 0.1f)));
                var zoneObject = new GameObject("GrillThermalZone", typeof(BoxCollider));
                zoneObject.transform.SetParent(grill.transform, false);
                zoneObject.transform.localPosition = new Vector3(0f, 1.1f, 0f);
                BoxCollider zone = zoneObject.GetComponent<BoxCollider>();
                zone.size = new Vector3(2.3f, 0.22f, 1.1f);
                zone.isTrigger = true;
                GrillHeatSource heat = grill.AddComponent<GrillHeatSource>();
                var heatData = new SerializedObject(heat);
                heatData.FindProperty("_effectiveZone").objectReferenceValue = zone;
                heatData.ApplyModifiedPropertiesWithoutUndo();
                CreateBox(cookingZone.transform, "CookingPrepBench", new Vector3(-5.2f, 0.4f, 2f),
                    new Vector3(2.4f, 0.8f, 0.7f), volume);
                float[] temperatures = { 21f, -18f, 21f, 21f };
                string[] fixtureNames = { "Fresh", "Frozen", "Rotten", "Contaminated" };
                for (int index = 0; index < fixtureNames.Length; index++)
                {
                    foods.Add(CreateFood(cookingZone.transform, "Raw Beef Patty - " + fixtureNames[index] + " cooking fixture",
                        0f, new Vector3(0.4f, 0.12f, 0.4f), 0.15f, beefMaterial, beef, 0f,
                        index == 2 ? 0f : 100f, temperatures[index], index == 3,
                        new Vector3(-6f + index * 0.5f, 0.91f, 2f)));
                }
                FoodSimulation foodSimulation = foodZone.AddComponent<FoodSimulation>();
                var assemblyZone = new GameObject("AssemblyTestZone");
                // Compact working row: nearby prep/supplies -> grill -> assembly, with an open aisle to delivery.
                CreateBox(assemblyZone.transform, "AssemblyWorkbench", new Vector3(0.7f, 0.45f, 3.7f),
                    new Vector3(3.4f, 0.9f, 1.5f), volume);
                CreateBox(assemblyZone.transform, "AssemblySupplyBench", new Vector3(-5.2f, 0.4f, 3.7f),
                    new Vector3(2.4f, 0.8f, 1.2f), volume);
                for (int index = 0; index < 6; index++)
                    foods.Add(CreateFood(assemblyZone.transform, "Bun - Assembly supply " + (index + 1), 0f,
                        new Vector3(0.4f, 0.22f, 0.4f), 0.08f, bunMaterial, bun, 0f, 100f, 21f, false,
                        new Vector3(-6f + index % 3 * 0.45f, 0.96f, index < 3 ? 3.4f : 4f)));
                for (int index = 0; index < 4; index++)
                    foods.Add(CreateFood(assemblyZone.transform, "Cheese - Assembly supply " + (index + 1), 0f,
                        new Vector3(0.32f, 0.06f, 0.32f), 0.025f, cheeseMaterial, cheese, 0f, 100f, 21f, false,
                        new Vector3(-4.6f + index % 2 * 0.35f, 0.88f, index < 2 ? 3.4f : 4f)));
                DishDefinition[] dishDefinitions =
                {
                    GetOrCreateDishDefinition("Hamburger", "dish.hamburger", "Hamburger", new[] { bun, beef, bun }),
                    GetOrCreateDishDefinition("Cheeseburger", "dish.cheeseburger", "Cheeseburger", new[] { bun, beef, cheese, bun })
                };
                Material trayMaterial = GetOrCreateMaterial("DishTray", new Color(0.7f, 0.85f, 0.85f));
                var assemblySurfaces = new List<AssemblySurface>();
                for (int index = 0; index < 3; index++)
                    assemblySurfaces.Add(CreateAssemblyStation(assemblyZone.transform, index + 1, new Vector3(-0.4f + index * 1.1f, 0f, 3.7f),
                        foodSimulation, dishDefinitions, trayMaterial));
                assemblyInputData.Update();
                SerializedProperty surfaces = assemblyInputData.FindProperty("_surfaces");
                surfaces.arraySize = assemblySurfaces.Count;
                for (int index = 0; index < assemblySurfaces.Count; index++)
                    surfaces.GetArrayElementAtIndex(index).objectReferenceValue = assemblySurfaces[index];
                assemblyInputData.ApplyModifiedPropertiesWithoutUndo();
                var simulationData = new SerializedObject(foodSimulation);
                simulationData.FindProperty("_preservationSettings").objectReferenceValue = preservation;
                SerializedProperty foodReferences = simulationData.FindProperty("_foods");
                foodReferences.arraySize = foods.Count;
                for (int index = 0; index < foods.Count; index++)
                    foodReferences.GetArrayElementAtIndex(index).objectReferenceValue = foods[index];
                SerializedProperty heatReferences = simulationData.FindProperty("_heatSources");
                var storageZone = new GameObject("FoodStorageZone");
                ColdStorage fridge = CreateColdStorage(storageZone.transform, "Fridge", new Vector3(-5.8f, 0f, -0.1f), 4f,
                    GetOrCreateMaterial("FridgeShell", new Color(0.4f, 0.7f, 0.8f)), volume);
                ColdStorage freezer = CreateColdStorage(storageZone.transform, "Freezer", new Vector3(-5.8f, 0f, -1.9f), -18f,
                    GetOrCreateMaterial("FreezerShell", new Color(0.2f, 0.35f, 0.7f)), volume);
                heatReferences.arraySize = 3;
                heatReferences.GetArrayElementAtIndex(0).objectReferenceValue = heat;
                heatReferences.GetArrayElementAtIndex(1).objectReferenceValue = fridge;
                heatReferences.GetArrayElementAtIndex(2).objectReferenceValue = freezer;
                simulationData.ApplyModifiedPropertiesWithoutUndo();

                CreateCustomerService(foodSimulation, dishDefinitions);

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

                M8ProcurementBuilder.ConfigureScene(scene);
                M9RestaurantLayoutBuilder.ConfigureScene(scene);

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

        private static FoodPreservationSettings GetOrCreatePreservationSettings()
        {
            string path = FoodDefinitionFolder + "/PreservationSettings.asset";
            FoodPreservationSettings settings = AssetDatabase.LoadAssetAtPath<FoodPreservationSettings>(path);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<FoodPreservationSettings>();
                AssetDatabase.CreateAsset(settings, path);
            }
            settings.CreateProfile(); // Validate existing authoring without overwriting it.
            return settings;
        }

        private static ColdStorage CreateColdStorage(Transform parent, string name, Vector3 position,
            float temperature, Material shell, Material shelf)
        {
            var cabinet = new GameObject(name);
            cabinet.transform.SetParent(parent, false); cabinet.transform.localPosition = position;
            cabinet.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); // Open front faces the working aisle (east).
            CreateBox(cabinet.transform, "Base", new Vector3(0f, 0.4f, 0f), new Vector3(1.6f, 0.8f, 1.4f), shell);
            CreateBox(cabinet.transform, "Shelf", new Vector3(0f, 0.85f, 0f), new Vector3(1.6f, 0.1f, 1.4f), shelf);
            CreateBox(cabinet.transform, "LeftWall", new Vector3(-0.75f, 1.575f, 0f), new Vector3(0.1f, 1.35f, 1.4f), shell);
            CreateBox(cabinet.transform, "RightWall", new Vector3(0.75f, 1.575f, 0f), new Vector3(0.1f, 1.35f, 1.4f), shell);
            CreateBox(cabinet.transform, "BackWall", new Vector3(0f, 1.575f, 0.65f), new Vector3(1.6f, 1.35f, 0.1f), shell);
            CreateBox(cabinet.transform, "Top", new Vector3(0f, 2.3f, 0f), new Vector3(1.6f, 0.1f, 1.4f), shell);
            var interiorObject = new GameObject("ThermalInterior", typeof(BoxCollider));
            interiorObject.transform.SetParent(cabinet.transform, false);
            BoxCollider interior = interiorObject.GetComponent<BoxCollider>(); interior.isTrigger = true;
            interior.center = new Vector3(0f, 1.575f, -0.025f); interior.size = new Vector3(1.4f, 1.35f, 1.25f);
            ColdStorage storage = cabinet.AddComponent<ColdStorage>();
            var data = new SerializedObject(storage);
            data.FindProperty("_interior").objectReferenceValue = interior;
            data.FindProperty("_temperatureCelsius").floatValue = temperature;
            data.ApplyModifiedPropertiesWithoutUndo();
            var label = new GameObject("DevelopmentLabel", typeof(TextMesh));
            label.transform.SetParent(cabinet.transform, false); label.transform.localPosition = new Vector3(0f, 2.3f, -0.71f);
            TextMesh text = label.GetComponent<TextMesh>(); text.text = name + (temperature < 0f ? " -18 C" : " +4 C");
            text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center;
            text.characterSize = 0.035f; text.fontSize = 48; text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
            return storage;
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
            AddPickupPhysics(item, mass);
        }

        private static void AddPickupPhysics(GameObject item, float mass)
        {
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

        private static FoodItem CreateFood(Transform parent, string name, float x, Vector3 size, float mass,
            Material material, FoodDefinition definition, float age, float freshness, float temperature, bool contaminated,
            Vector3? position = null)
        {
            GameObject item = CreateBox(parent, name, position ?? new Vector3(x, 0.85f + size.y * 0.5f, -3.5f), size, material);
            AddPickupPhysics(item, mass);
            var pickupData = new SerializedObject(item.GetComponent<Pickup>());
            pickupData.FindProperty("_displayName").stringValue = definition.DisplayName;
            pickupData.ApplyModifiedPropertiesWithoutUndo();
            FoodItem food = item.AddComponent<FoodItem>();
            var data = new SerializedObject(food);
            data.FindProperty("_definition").objectReferenceValue = definition;
            data.FindProperty("_initialAgeSeconds").floatValue = age;
            data.FindProperty("_initialFreshnessPercent").floatValue = freshness;
            data.FindProperty("_initialTemperatureCelsius").floatValue = temperature;
            data.FindProperty("_initiallyContaminated").boolValue = contaminated;
            data.ApplyModifiedPropertiesWithoutUndo();
            return food;
        }

        private static FoodDefinition GetOrCreateFoodDefinition(string assetName, string id, string displayName,
            FoodCategory category, int costCents, float freshnessLifetime, float thermalResponse, bool cookable = false)
        {
            if (!AssetDatabase.IsValidFolder(FoodDefinitionFolder))
                AssetDatabase.CreateFolder("Assets/_Project/ScriptableObjects", "Food");
            string path = FoodDefinitionFolder + "/" + assetName + ".asset";
            FoodDefinition existing = AssetDatabase.LoadAssetAtPath<FoodDefinition>(path);
            if (existing != null)
            {
                existing.CreateProfile(); // Reject invalid authoring data before replacing the scene.
                return existing;
            }
            FoodDefinition definition = ScriptableObject.CreateInstance<FoodDefinition>();
            definition.name = assetName;
            var data = new SerializedObject(definition);
            data.FindProperty("_id").stringValue = id;
            data.FindProperty("_displayName").stringValue = displayName;
            data.FindProperty("_category").enumValueIndex = (int)category;
            data.FindProperty("_referenceCostCents").intValue = costCents;
            data.FindProperty("_freshnessLifetimeSeconds").floatValue = freshnessLifetime;
            data.FindProperty("_thermalResponseSeconds").floatValue = thermalResponse;
            data.FindProperty("_isCookable").boolValue = cookable;
            data.ApplyModifiedPropertiesWithoutUndo();
            definition.CreateProfile();
            AssetDatabase.CreateAsset(definition, path);
            return definition;
        }

        private static DishDefinition GetOrCreateDishDefinition(string assetName, string id, string displayName, FoodDefinition[] ingredients)
        {
            const string folder = "Assets/_Project/ScriptableObjects/Dishes";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/_Project/ScriptableObjects", "Dishes");
            string path = folder + "/" + assetName + ".asset";
            DishDefinition existing = AssetDatabase.LoadAssetAtPath<DishDefinition>(path);
            if (existing != null) { existing.CreateProfile(); return existing; }
            var definition = ScriptableObject.CreateInstance<DishDefinition>();
            definition.name = assetName;
            var data = new SerializedObject(definition);
            data.FindProperty("_id").stringValue = id;
            data.FindProperty("_displayName").stringValue = displayName;
            SerializedProperty refs = data.FindProperty("_orderedIngredients");
            refs.arraySize = ingredients.Length;
            for (int index = 0; index < ingredients.Length; index++) refs.GetArrayElementAtIndex(index).objectReferenceValue = ingredients[index];
            data.ApplyModifiedPropertiesWithoutUndo();
            definition.CreateProfile(); AssetDatabase.CreateAsset(definition, path);
            return definition;
        }

        private static AssemblySurface CreateAssemblyStation(Transform parent, int number, Vector3 position, FoodSimulation simulation,
            DishDefinition[] definitions, Material material)
        {
            var station = new GameObject("AssemblyStation" + number);
            station.transform.SetParent(parent, false); station.transform.localPosition = position;
            var tray = new GameObject("DraftTray" + number, typeof(Rigidbody), typeof(BoxCollider), typeof(DishItem));
            tray.transform.SetParent(station.transform, false); tray.transform.localPosition = new Vector3(0f, 0.935f, 0f);
            tray.GetComponent<Rigidbody>().isKinematic = true; tray.GetComponent<Rigidbody>().useGravity = false;
            tray.GetComponent<BoxCollider>().size = new Vector3(0.7f, 0.045f, 0.7f);
            GameObject visual = CreateBox(tray.transform, "TrayVisual", Vector3.zero, new Vector3(0.7f, 0.045f, 0.7f), material);
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<BoxCollider>());
            CreateBox(station.transform, "FinalizeTab", new Vector3(0.55f, 0.94f, 0f), new Vector3(0.15f, 0.06f, 0.25f), material);
            var sensorObject = new GameObject("AssemblyVolume", typeof(BoxCollider));
            sensorObject.transform.SetParent(station.transform, false); sensorObject.transform.localPosition = new Vector3(0f, 1.49f, 0f);
            BoxCollider sensor = sensorObject.GetComponent<BoxCollider>(); sensor.isTrigger = true;
            sensor.size = new Vector3(0.85f, 1.1f, 0.85f);
            AssemblySurface surface = station.AddComponent<AssemblySurface>();
            var data = new SerializedObject(surface);
            data.FindProperty("_dish").objectReferenceValue = tray.GetComponent<DishItem>();
            data.FindProperty("_assemblyZone").objectReferenceValue = sensor;
            data.FindProperty("_simulation").objectReferenceValue = simulation;
            data.FindProperty("_displayName").stringValue = "Assembly " + number;
            SerializedProperty profiles = data.FindProperty("_definitions"); profiles.arraySize = definitions.Length;
            for (int index = 0; index < definitions.Length; index++) profiles.GetArrayElementAtIndex(index).objectReferenceValue = definitions[index];
            data.ApplyModifiedPropertiesWithoutUndo();
            return surface;
        }

        private static void CreateCustomerService(FoodSimulation simulation, DishDefinition[] dishes)
        {
            const string folder = "Assets/_Project/ScriptableObjects/Customers";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/_Project/ScriptableObjects", "Customers");
            string path = folder + "/CustomerService.asset";
            CustomerServiceConfiguration configuration = AssetDatabase.LoadAssetAtPath<CustomerServiceConfiguration>(path);
            if (configuration == null)
            {
                configuration = ScriptableObject.CreateInstance<CustomerServiceConfiguration>();
                var configData = new SerializedObject(configuration);
                SerializedProperty menu = configData.FindProperty("_menu"); menu.arraySize = dishes.Length;
                for (int index = 0; index < dishes.Length; index++)
                {
                    menu.GetArrayElementAtIndex(index).FindPropertyRelative("_dish").objectReferenceValue = dishes[index];
                    menu.GetArrayElementAtIndex(index).FindPropertyRelative("_salePriceCents").intValue = index == 0 ? 500 : 650;
                }
                configData.ApplyModifiedPropertiesWithoutUndo(); configuration.CreateOffers();
                AssetDatabase.CreateAsset(configuration, path);
            }
            Material customerMaterial = GetOrCreateMaterial("CustomerPlaceholder", new Color(0.2f, 0.55f, 0.8f));
            Material deliveryMaterial = GetOrCreateMaterial("DeliveryPad", new Color(0.2f, 0.7f, 0.35f));
            var serviceRoot = new GameObject("CustomerServiceZone");
            Transform entry = CreateRoutePoint(serviceRoot.transform, "CustomerEntrance", new Vector3(6.25f, 0f, -5.3f), deliveryMaterial, true);
            Transform exit = CreateRoutePoint(serviceRoot.transform, "CustomerExit", new Vector3(6.25f, 0f, -5.65f), deliveryMaterial, false);
            Transform[] route =
            {
                CreateRoutePoint(serviceRoot.transform, "ArrivalCorner1", new Vector3(2.7f, 0f, -5.3f), null, false),
                CreateRoutePoint(serviceRoot.transform, "ArrivalCorner2", new Vector3(2.7f, 0f, -1.4f), null, false),
                CreateRoutePoint(serviceRoot.transform, "ArrivalCorner3", new Vector3(0f, 0f, -1.4f), null, false),
                CreateRoutePoint(serviceRoot.transform, "CustomerWaitingPoint", new Vector3(0f, 0f, -0.65f), customerMaterial, true)
            };
            var customer = new GameObject("CustomerPlaceholder", typeof(Rigidbody), typeof(BoxCollider), typeof(CustomerMovement));
            customer.transform.SetParent(serviceRoot.transform, false); customer.transform.position = entry.position;
            customer.GetComponent<Rigidbody>().isKinematic = true; customer.GetComponent<Rigidbody>().useGravity = false;
            customer.GetComponent<BoxCollider>().size = new Vector3(0.5f, 1.8f, 0.5f);
            customer.GetComponent<BoxCollider>().center = Vector3.up * 0.9f;
            GameObject torso = CreateBox(customer.transform, "BodyVisual", new Vector3(0f, 0.75f, 0f), new Vector3(0.45f, 1.3f, 0.4f), customerMaterial);
            UnityEngine.Object.DestroyImmediate(torso.GetComponent<BoxCollider>());
            GameObject head = CreateBox(customer.transform, "HeadVisual", new Vector3(0f, 1.6f, 0f), Vector3.one * 0.35f, customerMaterial);
            UnityEngine.Object.DestroyImmediate(head.GetComponent<BoxCollider>());
            CustomerMovement movement = customer.GetComponent<CustomerMovement>();
            var anchor = new GameObject("DishCarryAnchor"); anchor.transform.SetParent(customer.transform, false);
            anchor.transform.localPosition = new Vector3(0f, 1.2f, 0.65f);
            CustomerDishCarrier carrier = customer.AddComponent<CustomerDishCarrier>();
            var carrierData = new SerializedObject(carrier);
            carrierData.FindProperty("_anchor").objectReferenceValue = anchor.transform;
            carrierData.ApplyModifiedPropertiesWithoutUndo();
            var moverData = new SerializedObject(movement);
            moverData.FindProperty("_entryPoint").objectReferenceValue = entry;
            moverData.FindProperty("_exitPoint").objectReferenceValue = exit;
            SerializedProperty points = moverData.FindProperty("_arrivalPath"); points.arraySize = route.Length;
            for (int index = 0; index < route.Length; index++) points.GetArrayElementAtIndex(index).objectReferenceValue = route[index];
            moverData.ApplyModifiedPropertiesWithoutUndo(); customer.SetActive(false);

            CustomerServiceLoop service = serviceRoot.AddComponent<CustomerServiceLoop>();
            var serviceData = new SerializedObject(service);
            serviceData.FindProperty("_configuration").objectReferenceValue = configuration;
            serviceData.FindProperty("_customer").objectReferenceValue = movement;
            serviceData.FindProperty("_dishCarrier").objectReferenceValue = carrier;
            serviceData.FindProperty("_foodSimulation").objectReferenceValue = simulation;
            serviceData.ApplyModifiedPropertiesWithoutUndo();
            Vector3 deliverySize = new Vector3(1.8f, 0.03f, 1f);
            GameObject pad = CreateBox(serviceRoot.transform, "DeliveryPad", new Vector3(0f, 1.115f, 0.5f), deliverySize, deliveryMaterial);
            var zoneObject = new GameObject("DeliveryZone", typeof(BoxCollider), typeof(DeliveryZone));
            zoneObject.transform.SetParent(serviceRoot.transform, false); zoneObject.transform.position = new Vector3(0f, 1.65f, 0.5f);
            BoxCollider sensor = zoneObject.GetComponent<BoxCollider>(); sensor.isTrigger = true; sensor.size = new Vector3(deliverySize.x, 1.04f, deliverySize.z);
            var deliveryData = new SerializedObject(zoneObject.GetComponent<DeliveryZone>());
            deliveryData.FindProperty("_zone").objectReferenceValue = sensor;
            deliveryData.FindProperty("_support").objectReferenceValue = pad.GetComponent<BoxCollider>();
            deliveryData.FindProperty("_service").objectReferenceValue = service; deliveryData.ApplyModifiedPropertiesWithoutUndo();
            OrderFeedback feedback = serviceRoot.AddComponent<OrderFeedback>();
            var feedbackData = new SerializedObject(feedback); feedbackData.FindProperty("_service").objectReferenceValue = service;
            feedbackData.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform CreateRoutePoint(Transform parent, string name, Vector3 position, Material material, bool visible)
        {
            var point = new GameObject(name); point.transform.SetParent(parent, false); point.transform.position = position;
            if (visible)
            {
                GameObject marker = CreateBox(point.transform, "MarkerVisual", new Vector3(0f, 0.01f, 0f), new Vector3(0.7f, 0.02f, 0.45f), material);
                UnityEngine.Object.DestroyImmediate(marker.GetComponent<BoxCollider>());
            }
            return point.transform;
        }
    }
}
