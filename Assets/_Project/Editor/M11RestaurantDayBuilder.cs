using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Restaurant;

namespace ZeroStarRestaurant.Editor
{
    public static class M11RestaurantDayBuilder
    {
        [MenuItem("Zero Star Restaurant/Prototype/Install M11 Restaurant Day")]
        public static void InstallInExistingScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetSceneByPath(M1GreyboxBuilder.ScenePath).IsValid())
                throw new InvalidOperationException("Stop Play and close PrototypeRestaurant before installing M11. Save scene edits first.");
            if (Application.isBatchMode && string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(M1GreyboxBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene); ConfigureScene(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save M11 day scene.");
            }
            finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true); }
        }
        public static void ConfigureScene(Scene scene)
        {
            T[] Components<T>() where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
            RestaurantDayController installed = Components<RestaurantDayController>().SingleOrDefault();
            if (installed != null) { installed.Validate(); return; }
            CustomerQueueController queue = Components<CustomerQueueController>().Single();
            RestaurantDayController day = queue.gameObject.AddComponent<RestaurantDayController>();
            var dayData = new SerializedObject(day); dayData.FindProperty("_queue").objectReferenceValue = queue; dayData.ApplyModifiedPropertiesWithoutUndo();
            var queueData = new SerializedObject(queue); queueData.FindProperty("_restaurantDay").objectReferenceValue = day; queueData.ApplyModifiedPropertiesWithoutUndo();
            RestaurantDayFeedback feedback = queue.gameObject.AddComponent<RestaurantDayFeedback>();
            var feedbackData = new SerializedObject(feedback); feedbackData.FindProperty("_day").objectReferenceValue = day; feedbackData.ApplyModifiedPropertiesWithoutUndo();
            day.Validate(); EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
