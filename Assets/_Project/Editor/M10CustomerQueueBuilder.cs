using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Customers;

namespace ZeroStarRestaurant.Editor
{
    public static class M10CustomerQueueBuilder
    {
        [MenuItem("Zero Star Restaurant/Prototype/Install M10 Customer Queue")]
        public static void InstallInExistingScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetSceneByPath(M1GreyboxBuilder.ScenePath).IsValid())
                throw new InvalidOperationException("Stop Play and close PrototypeRestaurant before installing M10. Save scene edits first.");
            if (Application.isBatchMode && string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(M1GreyboxBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene); ConfigureScene(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save M10 queue scene.");
            }
            finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true); }
        }

        public static void ConfigureScene(Scene scene)
        {
            T[] Components<T>() where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
            CustomerServiceLoop service = Components<CustomerServiceLoop>().Single();
            CustomerQueueController installed = Components<CustomerQueueController>().SingleOrDefault();
            if (installed != null) { installed.Validate(); return; }
            var serviceData = new SerializedObject(service);
            CustomerMovement movement = (CustomerMovement)serviceData.FindProperty("_customer").objectReferenceValue;
            CustomerDishCarrier carrier = (CustomerDishCarrier)serviceData.FindProperty("_dishCarrier").objectReferenceValue;
            QueuedCustomer template = movement.gameObject.AddComponent<QueuedCustomer>();
            var templateData = new SerializedObject(template);
            templateData.FindProperty("_movement").objectReferenceValue = movement;
            templateData.FindProperty("_dishCarrier").objectReferenceValue = carrier;
            templateData.ApplyModifiedPropertiesWithoutUndo();

            Transform points = Components<Transform>().Single(item => item.name == "QueuePoints");
            if (points.childCount != 4) throw new InvalidOperationException("M10 requires the four ordered M9 QueuePoints.");
            var members = new GameObject("QueuedCustomers"); members.transform.SetParent(service.transform, false);
            CustomerQueueController queue = service.gameObject.AddComponent<CustomerQueueController>();
            var queueData = new SerializedObject(queue);
            queueData.FindProperty("_template").objectReferenceValue = template;
            queueData.FindProperty("_customersRoot").objectReferenceValue = members.transform;
            queueData.FindProperty("_servicePosition").objectReferenceValue = points.GetChild(0);
            SerializedProperty slots = queueData.FindProperty("_queuePoints"); slots.arraySize = points.childCount;
            for (int index = 0; index < slots.arraySize; index++) slots.GetArrayElementAtIndex(index).objectReferenceValue = points.GetChild(index);
            SerializedProperty approach = queueData.FindProperty("_entrancePath"); approach.arraySize = 2;
            SerializedProperty oldRoute = new SerializedObject(movement).FindProperty("_arrivalPath");
            for (int index = 0; index < 2; index++) approach.GetArrayElementAtIndex(index).objectReferenceValue = oldRoute.GetArrayElementAtIndex(index).objectReferenceValue;
            queueData.ApplyModifiedPropertiesWithoutUndo(); queue.Validate();
            serviceData.FindProperty("_queue").objectReferenceValue = queue; serviceData.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
