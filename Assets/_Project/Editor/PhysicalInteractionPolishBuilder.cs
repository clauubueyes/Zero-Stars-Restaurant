using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;

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
            inputData.FindProperty("_physicalAssembly").objectReferenceValue = physical;
            inputData.FindProperty("_surfaces").arraySize = 0;
            inputData.ApplyModifiedPropertiesWithoutUndo();
            // Keep every authored pose and collider: these are fixed preparation supports,
            // not free movable plates, draft order identities or volume-based assembly stations.
            foreach (AssemblySurface surface in surfaces)
            {
                DishItem draft = surface.Dish;
                if (draft != null)
                {
                    if (draft.GetComponentsInChildren<FoodItem>(true).Length != 0)
                        throw new InvalidOperationException("Save an empty preparation scene before adapting supports.");
                    draft.gameObject.name = "PrepSupport" + surface.name.Replace("AssemblyStation", "");
                    UnityEngine.Object.DestroyImmediate(draft);
                }
                if (surface.GetComponent<DishTraySupply>() != null)
                    UnityEngine.Object.DestroyImmediate(surface.GetComponent<DishTraySupply>());
                UnityEngine.Object.DestroyImmediate(surface);
            }
            var physicalInputData = new SerializedObject(Components<InteractionInput>().Single());
            physicalInputData.FindProperty("_assembly").objectReferenceValue = input;
            physicalInputData.ApplyModifiedPropertiesWithoutUndo();
            PlateProcurementBuilder.ConfigureScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
