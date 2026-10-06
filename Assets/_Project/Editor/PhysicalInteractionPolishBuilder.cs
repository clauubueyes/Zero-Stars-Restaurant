using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Editor
{
    public static class PhysicalInteractionPolishBuilder
    {
        public const string TrayPrefabPath = "Assets/_Project/Prefabs/EmptyDishTray.prefab";
        [MenuItem("Zero Star Restaurant/Prototype/Install Physical Interaction Polish")]
        public static void InstallInExistingScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetSceneByPath(M1GreyboxBuilder.ScenePath).IsValid())
                throw new InvalidOperationException("Stop Play and close PrototypeRestaurant before installing polish. Save scene edits first.");
            if (Application.isBatchMode && string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(M1GreyboxBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene); ConfigureScene(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save interaction polish.");
            }
            finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true); }
        }

        public static void ConfigureScene(Scene scene)
        {
            T[] Components<T>() where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
            AssemblySurface[] surfaces = Components<AssemblySurface>();
            DishItem prefab = AssetDatabase.LoadAssetAtPath<DishItem>(TrayPrefabPath);
            if (prefab == null)
            {
                DishItem source = new SerializedObject(surfaces.First()).FindProperty("_dish").objectReferenceValue as DishItem;
                if (source == null || source.GetComponentsInChildren<FoodItem>(true).Length != 0)
                    throw new InvalidOperationException("Only a clean authored tray can become the supply prefab.");
                GameObject copy = UnityEngine.Object.Instantiate(source.gameObject);
                try
                {
                    copy.name = "EmptyDishTray"; copy.transform.SetParent(null);
                    copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); copy.SetActive(false);
                    PrefabUtility.SaveAsPrefabAsset(copy, TrayPrefabPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(copy); }
                prefab = AssetDatabase.LoadAssetAtPath<DishItem>(TrayPrefabPath);
            }
            foreach (AssemblySurface surface in surfaces)
            {
                if (surface.GetComponent<DishTraySupply>() != null) continue;
                var data = new SerializedObject(surface.gameObject.AddComponent<DishTraySupply>());
                data.FindProperty("_surface").objectReferenceValue = surface;
                data.FindProperty("_emptyTrayPrefab").objectReferenceValue = prefab;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            DishAssemblyInteraction input = Components<DishAssemblyInteraction>().Single();
            PhysicalDishAssembly physical = input.GetComponent<PhysicalDishAssembly>();
            if (physical == null)
            {
                physical = input.gameObject.AddComponent<PhysicalDishAssembly>();
                var data = new SerializedObject(physical);
                data.FindProperty("_simulation").objectReferenceValue = Components<FoodSimulation>().Single();
                var definitions = new SerializedObject(surfaces.First()).FindProperty("_definitions");
                var destination = data.FindProperty("_definitions"); destination.arraySize = definitions.arraySize;
                for (int i = 0; i < definitions.arraySize; i++)
                    destination.GetArrayElementAtIndex(i).objectReferenceValue = definitions.GetArrayElementAtIndex(i).objectReferenceValue;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            var inputData = new SerializedObject(input);
            inputData.FindProperty("_physicalAssembly").objectReferenceValue = physical; inputData.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
