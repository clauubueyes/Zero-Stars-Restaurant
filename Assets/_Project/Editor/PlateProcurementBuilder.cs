using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Editor
{
    public static class PlateProcurementBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Plate.prefab";
        public const string ProductPath = "Assets/_Project/ScriptableObjects/Economy/Plate.asset";
        [MenuItem("Zero Star Restaurant/Prototype/Install Purchasable Plates and Free Assembly")]
        public static void InstallInExistingScene() => PhysicalInteractionPolishBuilder.InstallInExistingScene();

        public static void ConfigureScene(Scene scene)
        {
            IngredientPurchaseStation station = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<IngredientPurchaseStation>(true)).Single();
            PlateProduct product = AssetDatabase.LoadAssetAtPath<PlateProduct>(ProductPath);
            if (product == null)
            {
                PlateItem prefab = AssetDatabase.LoadAssetAtPath<PlateItem>(PrefabPath);
                if (prefab == null)
                {
                    GameObject unit = GameObject.CreatePrimitive(PrimitiveType.Cube); unit.name = "Plate"; unit.SetActive(false);
                    try
                    {
                        unit.transform.localScale = new Vector3(.6f, .045f, .6f);
                        unit.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/DishTray.mat");
                        Rigidbody body = unit.AddComponent<Rigidbody>(); body.mass = .2f;
                        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; body.interpolation = RigidbodyInterpolation.Interpolate;
                        body.linearDamping = .05f; body.angularDamping = .25f; body.solverIterations = 12;
                        unit.AddComponent<PlateItem>();
                        var pickup = new SerializedObject(unit.GetComponent<Pickup>()); pickup.FindProperty("_displayName").stringValue = "Plate";
                        pickup.ApplyModifiedPropertiesWithoutUndo();
                        prefab = PrefabUtility.SaveAsPrefabAsset(unit, PrefabPath).GetComponent<PlateItem>();
                    }
                    finally { UnityEngine.Object.DestroyImmediate(unit); }
                }
                product = ScriptableObject.CreateInstance<PlateProduct>();
                var configuration = new SerializedObject(product); configuration.FindProperty("_prefab").objectReferenceValue = prefab;
                configuration.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.CreateAsset(product, ProductPath);
            }
            product.Validate();
            var stationData = new SerializedObject(station);
            stationData.FindProperty("_plateProduct").objectReferenceValue = product; stationData.ApplyModifiedPropertiesWithoutUndo();
            if (station.transform.Find("BuyPlate") == null)
            {
                Transform output = (Transform)stationData.FindProperty("_output").objectReferenceValue;
                GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube); button.name = "BuyPlate";
                button.transform.SetParent(station.transform, false);
                button.transform.SetPositionAndRotation(output.position + output.right * .8f + output.forward * .15f, output.rotation);
                button.transform.localScale = new Vector3(.5f, .3f, .25f);
                button.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/GreyboxVolume.mat");
                var label = new GameObject("LabelPlate", typeof(TextMesh)); label.transform.SetParent(station.transform, false);
                label.transform.SetPositionAndRotation(button.transform.position + output.forward * .13f, output.rotation * Quaternion.Euler(0, 180, 0));
                TextMesh text = label.GetComponent<TextMesh>(); text.text = "Plate\nBuy (" + IngredientPurchaseStation.FormatCents(product.PriceCents) + ") [E]";
                text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center;
                text.characterSize = .012f; text.fontSize = 48; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
                var data = new SerializedObject(button.AddComponent<IngredientPurchaseButton>());
                data.FindProperty("_station").objectReferenceValue = station;
                data.FindProperty("_productIndex").intValue = stationData.FindProperty("_products").arraySize;
                data.FindProperty("_label").objectReferenceValue = text; data.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
