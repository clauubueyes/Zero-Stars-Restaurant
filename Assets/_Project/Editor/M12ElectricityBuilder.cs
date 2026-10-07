using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Utilities;

namespace ZeroStarRestaurant.Editor
{
    public static class M12ElectricityBuilder
    {
        [MenuItem("Zero Star Restaurant/Prototype/Install M12 Electricity and Utilities")]
        public static void InstallInExistingScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetSceneByPath(M1GreyboxBuilder.ScenePath).IsValid())
                throw new InvalidOperationException("Stop Play and close PrototypeRestaurant before installing M12. Save scene edits first.");
            if (Application.isBatchMode && string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(M1GreyboxBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene); ConfigureScene(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save M12 electricity scene.");
            }
            finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true); }
        }

        public static void ConfigureScene(Scene scene)
        {
            T[] Components<T>() where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
            RestaurantElectricity installed = Components<RestaurantElectricity>().SingleOrDefault();
            if (installed != null) { installed.Validate(); InstallApplianceSwitches(installed); return; }
            HeatSource[] sources = { Components<GrillHeatSource>().Single(),
                Components<ColdStorage>().Single(item => item.name == "Fridge"),
                Components<ColdStorage>().Single(item => item.name == "Freezer") };
            if (sources.Any(source => source.Electricity != null)) throw new InvalidOperationException("Thermal appliances already have electrical dependencies.");
            var root = new GameObject("RestaurantUtilities"); SceneManager.MoveGameObjectToScene(root, scene);
            RestaurantElectricity supply = root.AddComponent<RestaurantElectricity>();
            var appliances = new ElectricalAppliance[sources.Length];
            float[] watts = { 2000, 150, 200 };
            for (int index = 0; index < sources.Length; index++)
            {
                HeatSource source = sources[index];
                ElectricalAppliance appliance = source.gameObject.AddComponent<ElectricalAppliance>(); appliances[index] = appliance;
                var data = new SerializedObject(appliance);
                data.FindProperty("_supply").objectReferenceValue = supply;
                data.FindProperty("_thermalSource").objectReferenceValue = source;
                data.FindProperty("_ratedWatts").floatValue = watts[index]; data.ApplyModifiedPropertiesWithoutUndo();
                data = new SerializedObject(source); data.FindProperty("_electricity").objectReferenceValue = appliance;
                data.FindProperty("_requiresElectricity").boolValue = true; data.ApplyModifiedPropertiesWithoutUndo();
            }
            var supplyData = new SerializedObject(supply); SerializedProperty list = supplyData.FindProperty("_appliances");
            list.arraySize = appliances.Length;
            for (int index = 0; index < appliances.Length; index++) list.GetArrayElementAtIndex(index).objectReferenceValue = appliances[index];
            supplyData.ApplyModifiedPropertiesWithoutUndo();

            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube); button.name = "ElectricitySwitch";
            button.transform.SetParent(root.transform, false);
            // On the north wall, accessible from the kitchen aisle, outside Grill and cabinet volumes.
            button.transform.position = new Vector3(.2f, 1.45f, 5.9f);
            button.transform.localScale = new Vector3(.5f, .65f, .15f);
            button.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/GreyboxVolume.mat");
            var label = new GameObject("ElectricityLabel", typeof(TextMesh)); label.transform.SetParent(root.transform, false);
            label.transform.SetPositionAndRotation(new Vector3(.2f, 1.45f, 5.81f), Quaternion.Euler(0, 180, 0));
            TextMesh text = label.GetComponent<TextMesh>(); text.text = "ELECTRICITY OFF\nPower ON [E]";
            text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center;
            text.characterSize = .015f; text.fontSize = 48; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
            var switchData = new SerializedObject(button.AddComponent<ElectricitySwitch>());
            switchData.FindProperty("_supply").objectReferenceValue = supply;
            switchData.FindProperty("_label").objectReferenceValue = text; switchData.ApplyModifiedPropertiesWithoutUndo();
            var feedback = new SerializedObject(root.AddComponent<ElectricityFeedback>());
            feedback.FindProperty("_supply").objectReferenceValue = supply; feedback.ApplyModifiedPropertiesWithoutUndo();
            supply.Validate(); InstallApplianceSwitches(supply); EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void InstallApplianceSwitches(RestaurantElectricity supply)
        {
            foreach (ElectricalAppliance appliance in supply.Appliances)
            {
                ApplianceSwitch installed = appliance.GetComponent<ApplianceSwitch>();
                if (installed != null)
                {
                    if (installed.Appliance != appliance) throw new InvalidOperationException("Appliance switch references a different appliance.");
                    continue;
                }
                var data = new SerializedObject(appliance);
                // Populate only absent labels when upgrading; keep user settings and existing references.
                if (string.IsNullOrWhiteSpace(data.FindProperty("_applianceName").stringValue))
                    data.FindProperty("_applianceName").stringValue = appliance.ThermalSource is GrillHeatSource ? "Grill" : appliance.name;
                data.ApplyModifiedPropertiesWithoutUndo();
                var switchData = new SerializedObject(appliance.gameObject.AddComponent<ApplianceSwitch>());
                switchData.FindProperty("_appliance").objectReferenceValue = appliance; switchData.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(supply.gameObject.scene);
            }
        }
    }
}
