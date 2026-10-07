using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Restaurant;
using ZeroStarRestaurant.Utilities;

namespace ZeroStarRestaurant.Editor
{
    public static class M13OperatingCostsBuilder
    {
        public const string SettingsPath = "Assets/_Project/ScriptableObjects/Economy/OperatingCosts.asset";
        [MenuItem("Zero Star Restaurant/Prototype/Install M13 Bills and Operating Costs")]
        public static void InstallInExistingScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetSceneByPath(M1GreyboxBuilder.ScenePath).IsValid())
                throw new InvalidOperationException("Stop Play and close PrototypeRestaurant before installing M13. Save scene edits first.");
            if (Application.isBatchMode && string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(M1GreyboxBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene); ConfigureScene(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save M13 operating costs scene.");
                AssetDatabase.SaveAssets();
            }
            finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true); }
        }
        public static void ConfigureScene(Scene scene)
        {
            T[] Components<T>() where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
            RestaurantOperatingCosts installed = Components<RestaurantOperatingCosts>().SingleOrDefault();
            if (installed != null) { installed.Validate(); return; }
            RestaurantDayController day = Components<RestaurantDayController>().Single();
            CustomerServiceLoop service = Components<CustomerServiceLoop>().Single();
            RestaurantElectricity supply = Components<RestaurantElectricity>().Single();
            OperatingCostSettings settings = AssetDatabase.LoadAssetAtPath<OperatingCostSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<OperatingCostSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            RestaurantOperatingCosts costs = day.gameObject.AddComponent<RestaurantOperatingCosts>();
            var data = new SerializedObject(costs);
            data.FindProperty("_day").objectReferenceValue = day;
            data.FindProperty("_service").objectReferenceValue = service;
            data.FindProperty("_electricity").objectReferenceValue = supply;
            data.FindProperty("_settings").objectReferenceValue = settings; data.ApplyModifiedPropertiesWithoutUndo();
            data = new SerializedObject(day); data.FindProperty("_operatingCosts").objectReferenceValue = costs;
            data.FindProperty("_requiresOperatingCosts").boolValue = true; data.ApplyModifiedPropertiesWithoutUndo();
            data = new SerializedObject(day.gameObject.AddComponent<OperatingCostsFeedback>());
            data.FindProperty("_costs").objectReferenceValue = costs; data.ApplyModifiedPropertiesWithoutUndo();
            day.Validate(); EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
