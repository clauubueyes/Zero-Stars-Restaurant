using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Editor
{
    // Repositions the existing M1-M8 objects; does not recreate their components or GUIDs.
    public static class M9RestaurantLayoutBuilder
    {
        [MenuItem("Zero Star Restaurant/Prototype/Apply M9 Restaurant Layout")]
        public static void InstallInExistingScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetSceneByPath(M1GreyboxBuilder.ScenePath).IsValid())
                throw new InvalidOperationException("Stop Play and close PrototypeRestaurant before applying M9. Save your scene edits first.");
            if (Application.isBatchMode && string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(M1GreyboxBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                ConfigureScene(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save M9 layout.");
            }
            finally
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        public static void ConfigureScene(Scene scene)
        {
            Transform[] existing = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            Transform Find(string name) => existing.Single(item => item.name == name);
            Transform greybox = Find("Greybox"), service = Find("CustomerServiceZone");
            Material wall = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/GreyboxWall.mat");
            Material volume = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/GreyboxVolume.mat");
            Material marker = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/CustomerPlaceholder.mat");
            Material pass = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/DeliveryPad.mat");

            SetBox(Find("Floor"), new Vector3(0, -0.25f, -1.5f), new Vector3(14, 0.5f, 15));
            SetBox(Find("WallSouth"), new Vector3(0, 1.75f, -6.25f), new Vector3(7.5f, 3.5f, 0.5f));
            Box(greybox, "EntranceWallEnd", new Vector3(-6.375f, 1.75f, -6.25f), new Vector3(1.25f, 3.5f, 0.5f), wall);
            Box(greybox, "ExitWallEnd", new Vector3(6.375f, 1.75f, -6.25f), new Vector3(1.25f, 3.5f, 0.5f), wall);
            Box(greybox, "EntranceLintel", new Vector3(-4.75f, 3.1f, -6.25f), new Vector3(2, 0.8f, 0.5f), wall);
            Box(greybox, "ExitLintel", new Vector3(4.75f, 3.1f, -6.25f), new Vector3(2, 0.8f, 0.5f), wall);
            // Wall-to-wall separation, retaining the 1.1 m support height and delivery pad dimensions.
            SetBox(Find("ServiceCounterVolume"), new Vector3(0, 0.55f, 0.5f), new Vector3(14, 1.1f, 1));
            Find("Player").position = new Vector3(-3.8f, 0.05f, 2.55f);

            MoveBenchAndFixtures("CookingPrepBench", new Vector3(-2.5f, 0.4f, 1.75f), "CookingTestZone");
            MoveBenchAndFixtures("AssemblySupplyBench", new Vector3(5.2f, 0.4f, 4.8f), "AssemblyTestZone");
            Find("Fridge").position = new Vector3(-6.2f, 0, 4.9f);
            Find("Freezer").position = new Vector3(-6.2f, 0, 3.1f);
            Find("GrillStation").position = new Vector3(-2f, 0, 3.7f);

            Transform development = Child(greybox, "DevelopmentGeometry");
            foreach (string name in new[] { "KitchenDivider", "TableVolume", "WorktopVolume", "LowStep", "TallBlocker" })
            {
                Transform item = Find(name); item.SetParent(development, true); item.gameObject.SetActive(false);
            }
            Find("PhysicalTestObjects").gameObject.SetActive(false);
            GameObject supplyBench = Find("AssemblySupplyBench").gameObject; supplyBench.SetActive(false);
            var supply = new SerializedObject(Find("FoodTestZone").GetComponent<DevelopmentIngredientSupply>());
            supply.FindProperty("_fixtureSupports").arraySize = 1;
            supply.FindProperty("_fixtureSupports").GetArrayElementAtIndex(0).objectReferenceValue = supplyBench;
            supply.ApplyModifiedPropertiesWithoutUndo();

            Find("CustomerEntrance").position = new Vector3(-4.75f, 0, -7.4f);
            Find("CustomerExit").position = new Vector3(4.75f, 0, -7.4f);
            Find("CustomerPlaceholder").position = Find("CustomerEntrance").position;
            Find("ArrivalCorner1").position = new Vector3(-4.75f, 0, -4.4f);
            Find("ArrivalCorner2").position = new Vector3(0, 0, -4.4f);
            Find("ArrivalCorner3").position = new Vector3(0, 0, -1.9f);
            Transform queues = Child(service, "QueuePoints");
            Transform waiting = Find("CustomerWaitingPoint"); waiting.SetParent(queues, true);
            waiting.position = new Vector3(0, 0, -0.65f);
            for (int index = 1; index < 4; index++)
            {
                Transform point = Child(queues, "QueuePoint" + (index + 1).ToString("D2"));
                point.position = new Vector3(0, 0, -0.65f - index * 1.25f);
                Box(point, "MarkerVisual", point.position + Vector3.up * 0.01f, new Vector3(0.7f, 0.02f, 0.45f), marker, false);
            }
            Transform departures = Child(service, "DeparturePoints");
            Vector3[] exitPositions = { new Vector3(2.5f, 0, -0.65f), new Vector3(4.75f, 0, -3), new Vector3(4.75f, 0, -6.9f) };
            var movement = new SerializedObject(Find("CustomerPlaceholder").GetComponent<CustomerMovement>());
            SerializedProperty route = movement.FindProperty("_departurePath"); route.arraySize = exitPositions.Length;
            for (int index = 0; index < exitPositions.Length; index++)
            {
                Transform point = Child(departures, "ExitCorner" + (index + 1)); point.position = exitPositions[index];
                route.GetArrayElementAtIndex(index).objectReferenceValue = point;
            }
            movement.ApplyModifiedPropertiesWithoutUndo();

            Transform signs = Child(greybox, "LayoutSigns");
            Label(signs, "EntranceSign", "ENTRANCE", new Vector3(-4.75f, 2.9f, -6.52f));
            Label(signs, "ExitSign", "EXIT", new Vector3(4.75f, 2.9f, -6.52f));
            Label(signs, "CounterSign", "SERVICE COUNTER / DELIVERY", new Vector3(0, 0.7f, -0.015f));
            Label(signs, "QueueSign", "QUEUE", new Vector3(0, 0.025f, -4.95f), true);
            Label(signs, "PrepSign", "PREP", new Vector3(-2.5f, 0.55f, 1.39f));
            Label(signs, "GrillSign", "GRILL", new Vector3(-2f, 0.65f, 3.085f));
            Label(signs, "AssemblySign", "ASSEMBLY", new Vector3(0.7f, 0.65f, 2.935f));
            Label(signs, "PassSign", "PASS / DELIVERY", new Vector3(0, 0.7f, 1.015f)).rotation = Quaternion.Euler(0, 180, 0);
            Box(signs, "KitchenPassMarker", new Vector3(0, 0.006f, 1.45f), new Vector3(1.8f, 0.012f, 0.3f), pass, false);
            ConfigureProcurementArea(scene);
            EditorSceneManager.MarkSceneDirty(scene);

            void MoveBenchAndFixtures(string benchName, Vector3 destination, string fixtureRoot)
            {
                Transform bench = Find(benchName); Vector3 delta = destination - bench.position; bench.position = destination;
                foreach (FoodItem food in Find(fixtureRoot).GetComponentsInChildren<FoodItem>(true)) food.transform.position += delta;
            }
        }

        [MenuItem("Zero Star Restaurant/Prototype/Move Procurement Outside Kitchen")]
        public static void InstallProcurementAreaInExistingScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetSceneByPath(M1GreyboxBuilder.ScenePath).IsValid())
                throw new InvalidOperationException("Stop Play and close PrototypeRestaurant before relocating Procurement. Save scene edits first.");
            if (Application.isBatchMode && string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(M1GreyboxBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene); ConfigureProcurementArea(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save Procurement area.");
            }
            finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true); }
        }

        // A local annex and access opening only. Preserve the station, its children,
        // support and all explicit references; never rebuild the rest of the restaurant.
        public static void ConfigureProcurementArea(Scene scene)
        {
            Transform[] existing = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            Transform Find(string name) => existing.Single(item => item.name == name);
            Transform greybox = Find("Greybox"), area = Child(greybox, "ProcurementArea");
            Material wall = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/GreyboxWall.mat");
            Material floor = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/GreyboxFloor.mat");
            // Keep the existing west wall as the lower segment; 1.8 x 2.7 m clear doorway.
            SetBox(Find("WallWest"), new Vector3(-7.25f, 1.75f, -2.65f), new Vector3(.5f, 3.5f, 7.7f));
            Box(area, "ProcurementAccessNorthWall", new Vector3(-7.25f, 1.75f, 4.75f), new Vector3(.5f, 3.5f, 3.5f), wall);
            Box(area, "ProcurementAccessLintel", new Vector3(-7.25f, 3.1f, 2.1f), new Vector3(.5f, .8f, 1.8f), wall);
            Box(area, "ProcurementFloor", new Vector3(-9.1f, -.25f, 1.625f), new Vector3(4.2f, .5f, 3.75f), floor);
            Box(area, "ProcurementWestWall", new Vector3(-11.45f, 1.75f, 1.625f), new Vector3(.5f, 3.5f, 4.75f), wall);
            Box(area, "ProcurementNorthWall", new Vector3(-9.1f, 1.75f, 3.75f), new Vector3(4.2f, 3.5f, .5f), wall);
            Box(area, "ProcurementSouthWall", new Vector3(-9.1f, 1.75f, -.5f), new Vector3(4.2f, 3.5f, .5f), wall);

            Transform bench = Find("FoodTestBench");
            Vector3 delta = new Vector3(-9.7f, .4f, 1.25f) - bench.position;
            bench.position += delta;
            Find("IngredientProcurementStation").position += delta;
            foreach (FoodItem food in Find("FoodTestZone").GetComponentsInChildren<FoodItem>(true)) food.transform.position += delta;
            // The authored local arrangement is unchanged from M9; explicit output supports Rebuild too.
            Find("PurchaseOutput").position = new Vector3(-9.7f, 1.12f, 1.5f);
            Find("OutputClearance").position = new Vector3(-9.7f, 1.08f, 1.5f);
            Find("OutputMarker").position = new Vector3(-9.7f, .806f, 1.5f);
            Find("OutputLabel").position = new Vector3(-9.7f, .82f, 1.88f);
            string[] products = { "Bun", "RawBeefPatty", "Cheese" };
            for (int index = 0; index < products.Length; index++)
            {
                Transform button = Find("Buy" + products[index]);
                button.position = new Vector3(-10.5f + index * .8f, 1f, .8f);
                Transform label = Find("Label" + products[index]);
                label.position = button.position + Vector3.forward * .13f;
                label.rotation = Quaternion.Euler(0, 180, 0);
            }
            Label(Child(greybox, "LayoutSigns"), "ProcurementSign", "PROCUREMENT", new Vector3(-9.7f, 1.5f, 1.73f));
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static Transform Child(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return child;
            var item = new GameObject(name); item.transform.SetParent(parent, false); return item.transform;
        }

        private static void SetBox(Transform item, Vector3 position, Vector3 size)
        { item.position = position; item.localScale = size; }

        private static void Box(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool solid = true)
        {
            Transform item = parent.Find(name);
            if (item == null)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.name = name;
                item = cube.transform; item.SetParent(parent, false);
                if (!solid) UnityEngine.Object.DestroyImmediate(cube.GetComponent<BoxCollider>());
            }
            SetBox(item, position, size); item.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Transform Label(Transform parent, string name, string text, Vector3 position, bool floor = false)
        {
            Transform item = Child(parent, name); item.position = position;
            item.rotation = floor ? Quaternion.Euler(90, 0, 0) : Quaternion.identity;
            TextMesh label = item.GetComponent<TextMesh>();
            if (label == null) label = item.gameObject.AddComponent<TextMesh>();
            label.text = text; label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
            label.fontSize = 48; label.characterSize = 0.035f; label.color = Color.white;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            item.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
            return item;
        }
    }
}
