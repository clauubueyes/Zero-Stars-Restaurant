using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Editor
{
    public static class M8ProcurementBuilder
    {
        // Add M8 without rebuilding any of the existing greybox's objects or identifiers.
        public static void InstallInExistingScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetSceneByPath(M1GreyboxBuilder.ScenePath).IsValid())
                throw new InvalidOperationException("Stop Play and close PrototypeRestaurant before installing M8.");
            if (Application.isBatchMode && string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(M1GreyboxBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                ConfigureScene(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save M8 scene.");
            }
            finally
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        public static void ConfigureScene(Scene scene)
        {
            T[] Components<T>() where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
            if (Components<IngredientPurchaseStation>().Length > 0) return;
            FoodSimulation simulation = Components<FoodSimulation>().Single();
            FoodItem[] fixtures = Components<FoodItem>();
            var supply = simulation.gameObject.AddComponent<DevelopmentIngredientSupply>();
            var supplyData = new SerializedObject(supply);
            supplyData.FindProperty("_simulation").objectReferenceValue = simulation;
            SerializedProperty fixtureRefs = supplyData.FindProperty("_fixtures"); fixtureRefs.arraySize = fixtures.Length;
            for (int index = 0; index < fixtures.Length; index++) fixtureRefs.GetArrayElementAtIndex(index).objectReferenceValue = fixtures[index];
            supplyData.ApplyModifiedPropertiesWithoutUndo();

            CustomerServiceLoop service = Components<CustomerServiceLoop>().Single();
            var serviceData = new SerializedObject(service);
            serviceData.FindProperty("_developmentInitialBalanceCents").intValue = 1000;
            serviceData.ApplyModifiedPropertiesWithoutUndo();

            EnsureFolder("Assets/_Project/Prefabs", "Food");
            EnsureFolder("Assets/_Project/ScriptableObjects", "Economy");
            IngredientProduct[] products =
            {
                Product("Bun", 35, new Vector3(0.4f, 0.22f, 0.4f), 0.08f, 21f, "FoodBun"),
                Product("RawBeefPatty", 80, new Vector3(0.4f, 0.12f, 0.4f), 0.15f, 4f, "FoodBeef"),
                Product("Cheese", 25, new Vector3(0.32f, 0.06f, 0.32f), 0.025f, 4f, "FoodCheese")
            };
            var root = new GameObject("IngredientProcurementStation");
            var output = new GameObject("PurchaseOutput"); output.transform.SetParent(root.transform, false);
            output.transform.position = new Vector3(-4f, 1.12f, -3.75f);
            var clearance = new GameObject("OutputClearance", typeof(BoxCollider)); clearance.transform.SetParent(root.transform, false);
            clearance.transform.position = new Vector3(-4f, 1.08f, -3.75f);
            BoxCollider clearanceBox = clearance.GetComponent<BoxCollider>(); clearanceBox.isTrigger = true;
            clearanceBox.size = new Vector3(0.55f, 0.55f, 0.55f);
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube); marker.name = "OutputMarker";
            marker.transform.SetParent(root.transform, false); marker.transform.position = new Vector3(-4f, 0.806f, -3.75f);
            marker.transform.localScale = new Vector3(0.55f, 0.012f, 0.55f);
            marker.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/DishTray.mat");
            UnityEngine.Object.DestroyImmediate(marker.GetComponent<BoxCollider>());
            var outputLabel = new GameObject("OutputLabel", typeof(TextMesh)); outputLabel.transform.SetParent(root.transform, false);
            outputLabel.transform.position = new Vector3(-4f, 0.82f, -4.13f); outputLabel.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            TextMesh outputText = outputLabel.GetComponent<TextMesh>(); outputText.text = "OUTPUT";
            outputText.anchor = TextAnchor.MiddleCenter; outputText.characterSize = 0.02f; outputText.fontSize = 48;
            outputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            outputLabel.GetComponent<MeshRenderer>().sharedMaterial = outputText.font.material;
            IngredientPurchaseStation station = root.AddComponent<IngredientPurchaseStation>();
            var data = new SerializedObject(station);
            data.FindProperty("_service").objectReferenceValue = service;
            data.FindProperty("_simulation").objectReferenceValue = simulation;
            data.FindProperty("_output").objectReferenceValue = output.transform;
            data.FindProperty("_outputClearance").objectReferenceValue = clearanceBox;
            SerializedProperty productRefs = data.FindProperty("_products"); productRefs.arraySize = products.Length;
            for (int index = 0; index < products.Length; index++) productRefs.GetArrayElementAtIndex(index).objectReferenceValue = products[index];
            data.ApplyModifiedPropertiesWithoutUndo();
            Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/GreyboxVolume.mat");
            for (int index = 0; index < products.Length; index++)
            {
                GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube); button.name = "Buy" + products[index].name;
                button.transform.SetParent(root.transform, false);
                button.transform.position = new Vector3(-4.8f + index * 0.8f, 1f, -3.05f);
                button.transform.localScale = new Vector3(0.7f, 0.3f, 0.25f);
                button.GetComponent<MeshRenderer>().sharedMaterial = material;
                var label = new GameObject("Label" + products[index].name, typeof(TextMesh)); label.transform.SetParent(root.transform, false);
                label.transform.position = button.transform.position + new Vector3(0f, 0f, -0.13f);
                TextMesh text = label.GetComponent<TextMesh>();
                text.text = products[index].Prefab.Definition.DisplayName + "\nBuy (" + IngredientPurchaseStation.FormatCents(products[index].PriceCents) + ") [E]";
                text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center;
                text.characterSize = 0.012f; text.fontSize = 48; text.color = Color.white;
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
                IngredientPurchaseButton interaction = button.AddComponent<IngredientPurchaseButton>();
                var interactionData = new SerializedObject(interaction);
                interactionData.FindProperty("_station").objectReferenceValue = station;
                interactionData.FindProperty("_productIndex").intValue = index;
                interactionData.FindProperty("_label").objectReferenceValue = text;
                interactionData.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssets();
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }

        private static IngredientProduct Product(string name, int price, Vector3 size, float mass, float temperature, string materialName)
        {
            string prefabPath = "Assets/_Project/Prefabs/Food/" + name + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                GameObject unit = GameObject.CreatePrimitive(PrimitiveType.Cube); unit.name = name; unit.SetActive(false);
                try
                {
                    unit.transform.localScale = size;
                    unit.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/" + materialName + ".mat");
                    Rigidbody body = unit.AddComponent<Rigidbody>(); body.mass = mass; body.linearDamping = 0.05f; body.angularDamping = 0.25f;
                    body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; body.interpolation = RigidbodyInterpolation.Interpolate;
                    body.maxLinearVelocity = 20f; body.maxAngularVelocity = 10f; body.solverIterations = 12;
                    unit.AddComponent<Pickup>();
                    FoodDefinition definition = AssetDatabase.LoadAssetAtPath<FoodDefinition>("Assets/_Project/ScriptableObjects/Food/" + name + ".asset");
                    var pickupData = new SerializedObject(unit.GetComponent<Pickup>()); pickupData.FindProperty("_displayName").stringValue = definition.DisplayName;
                    pickupData.ApplyModifiedPropertiesWithoutUndo();
                    var foodData = new SerializedObject(unit.AddComponent<FoodItem>());
                    foodData.FindProperty("_definition").objectReferenceValue = definition;
                    foodData.FindProperty("_initialTemperatureCelsius").floatValue = temperature;
                    foodData.ApplyModifiedPropertiesWithoutUndo();
                    prefab = PrefabUtility.SaveAsPrefabAsset(unit, prefabPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(unit); }
            }
            string path = "Assets/_Project/ScriptableObjects/Economy/" + name + ".asset";
            IngredientProduct product = AssetDatabase.LoadAssetAtPath<IngredientProduct>(path);
            if (product == null)
            {
                product = ScriptableObject.CreateInstance<IngredientProduct>(); product.name = name;
                var data = new SerializedObject(product); data.FindProperty("_prefab").objectReferenceValue = prefab.GetComponent<FoodItem>();
                data.FindProperty("_priceCents").intValue = price; data.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(product, path);
            }
            product.Validate(); return product;
        }
    }
}
