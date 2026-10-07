using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Editor;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M9RestaurantLayoutTests
    {
        private Scene _scene;
        [SetUp] public void SetUp() => _scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
        [TearDown] public void TearDown() => EditorSceneManager.ClosePreviewScene(_scene);
        private T[] Components<T>() where T : Component => _scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        private Collider[] Solids() => Components<Collider>().Where(collider => collider.enabled && !collider.isTrigger &&
            collider.gameObject.activeInHierarchy && collider.GetComponentInParent<FoodItem>() == null &&
            collider.GetComponentInParent<CustomerMovement>() == null).ToArray();

        [Test]
        public void ActualArrivalAndDepartureRoutesClearWallsCounterAndDoorwaysWithCustomerWidth()
        {
            CustomerMovement customer = Components<CustomerMovement>().Single();
            var data = new SerializedObject(customer);
            Vector3 entry = ((Transform)data.FindProperty("_entryPoint").objectReferenceValue).position;
            Vector3 exit = ((Transform)data.FindProperty("_exitPoint").objectReferenceValue).position;
            Assert.That(entry.x, Is.LessThan(0)); Assert.That(exit.x, Is.GreaterThan(0));
            var probe = new GameObject("Customer clearance probe", typeof(BoxCollider));
            try
            {
                BoxCollider body = probe.GetComponent<BoxCollider>(); body.size = new Vector3(0.7f, 1.8f, 0.7f);
                body.center = new Vector3(0, 0.92f, 0);
                SerializedProperty arrival = data.FindProperty("_arrivalPath"), departure = data.FindProperty("_departurePath");
                Assert.That(departure.arraySize, Is.GreaterThan(0));
                Transform[] points = Enumerable.Range(0, arrival.arraySize).Select(index => (Transform)arrival.GetArrayElementAtIndex(index).objectReferenceValue)
                    .Concat(Enumerable.Range(0, departure.arraySize).Select(index => (Transform)departure.GetArrayElementAtIndex(index).objectReferenceValue)).ToArray();
                Vector3 start = entry;
                foreach (Vector3 end in points.Select(point => point.position).Append(exit))
                {
                    Assert.That(end.z, Is.LessThan(0), "Customer route stays on the public side.");
                    Sweep(body, start, end); start = end;
                }
            }
            finally { Object.DestroyImmediate(probe); }
        }

        [Test]
        public void SingleCustomerFollowsSeparateExitAndLegacyEmptyDepartureStillRetracesArrival()
        {
            CustomerMovement customer = Components<CustomerMovement>().Single();
            var data = new SerializedObject(customer);
            Transform firstExitCorner = (Transform)data.FindProperty("_departurePath").GetArrayElementAtIndex(0).objectReferenceValue;
            Transform finalWait = (Transform)data.FindProperty("_arrivalPath").GetArrayElementAtIndex(data.FindProperty("_arrivalPath").arraySize - 1).objectReferenceValue;
            Transform exit = (Transform)data.FindProperty("_exitPoint").objectReferenceValue;
            customer.BeginEntering(1); Assert.That(customer.Advance(100, 1), Is.True);
            Assert.That(customer.transform.position, Is.EqualTo(finalWait.position));
            customer.BeginLeaving(); Assert.That(customer.Advance(0.1, 1), Is.False);
            Assert.That(customer.transform.position.x, Is.GreaterThan(finalWait.position.x));
            Assert.That(customer.Advance(Vector3.Distance(customer.transform.position, firstExitCorner.position), 1), Is.False);
            Assert.That(customer.transform.position, Is.EqualTo(firstExitCorner.position));
            Assert.That(customer.Advance(100, 1), Is.True); Assert.That(customer.transform.position, Is.EqualTo(exit.position));
            customer.BeginEntering(2); customer.Advance(100, 1);
            data.FindProperty("_departurePath").arraySize = 0; data.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(customer.HasValidRoute, Is.True); customer.BeginLeaving(); customer.Advance(0.1, 1);
            Assert.That(customer.transform.position.z, Is.LessThan(finalWait.position.z));
            Assert.That(customer.Advance(100, 1), Is.True); Assert.That(customer.transform.position, Is.EqualTo(exit.position));
        }

        [Test]
        public void WorkingRouteHasCapsuleClearanceFromColdStorageThroughPrepGrillAssemblyAndPass()
        {
            var probe = new GameObject("Kitchen clearance probe", typeof(CapsuleCollider));
            try
            {
                CapsuleCollider body = probe.GetComponent<CapsuleCollider>(); body.height = 1.8f;
                body.radius = 0.35f; body.center = new Vector3(0, 0.92f, 0);
                Vector3[] stops = { new Vector3(-4.4f, 0, 4.9f), new Vector3(-4.4f, 0, 3.1f),
                    new Vector3(-4.4f, 0, 2.65f), new Vector3(-2.5f, 0, 2.65f),
                    new Vector3(-2f, 0, 2.55f), new Vector3(0.7f, 0, 2.05f), new Vector3(0, 0, 2.25f) };
                for (int index = 1; index < stops.Length; index++) Sweep(body, stops[index - 1], stops[index]);
            }
            finally { Object.DestroyImmediate(probe); }
        }

        private void Sweep(Collider body, Vector3 start, Vector3 end)
        {
            Collider[] solids = Solids();
            int samples = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(start, end) / 0.05f));
            for (int step = 0; step <= samples; step++)
            {
                Vector3 at = Vector3.Lerp(start, end, (float)step / samples);
                foreach (Collider obstacle in solids)
                    Assert.That(Physics.ComputePenetration(body, at, Quaternion.identity, obstacle,
                        obstacle.transform.position, obstacle.transform.rotation, out _, out float depth) && depth > 0.005f,
                        Is.False, "Route blocked by " + obstacle.name + " at " + at);
            }
        }

        [Test]
        public void ProcurementAnnexHasWalkableAccessOutsideKitchenAndExistingWorkingRoutes()
        {
            Transform bench = Components<Transform>().Single(item => item.name == "FoodTestBench");
            Assert.That(bench.GetComponent<BoxCollider>().bounds.max.x, Is.LessThan(-7.5f));
            var probe = new GameObject("Procurement access probe", typeof(CapsuleCollider));
            try
            {
                CapsuleCollider body = probe.GetComponent<CapsuleCollider>(); body.height = 1.8f;
                body.radius = .35f; body.center = new Vector3(0, .92f, 0);
                Vector3[] stops = { new Vector3(-3.8f, 0, 2.55f), new Vector3(-4.4f, 0, 2.65f),
                    new Vector3(-4.4f, 0, 1.6f), new Vector3(-6.5f, 0, 1.6f), new Vector3(-8.05f, 0, 1.6f),
                    new Vector3(-8.05f, 0, 2.65f), new Vector3(-9.7f, 0, 2.65f) };
                BoxCollider[] floors = Components<BoxCollider>().Where(item => item.name == "Floor" || item.name == "ProcurementFloor").ToArray();
                for (int index = 1; index < stops.Length; index++)
                {
                    Sweep(body, stops[index - 1], stops[index]);
                    for (int sample = 0; sample <= 30; sample++)
                    {
                        Vector3 at = Vector3.Lerp(stops[index - 1], stops[index], sample / 30f);
                        Assert.That(floors.Any(floor => floor.bounds.Contains(at + Vector3.down * .1f)), Is.True, "Access has physical floor at " + at);
                    }
                }
            }
            finally { Object.DestroyImmediate(probe); }
        }

        [Test]
        public void RelocatingOnlyProcurementPreservesGameplayReferencesAndOtherTransforms()
        {
            Component[] originals = Components<Component>();
            var references = Components<MonoBehaviour>().ToDictionary(item => item, item => EditorJsonUtility.ToJson(item));
            Transform station = Components<Transform>().Single(item => item.name == "IngredientProcurementStation");
            Transform foodRoot = Components<Transform>().Single(item => item.name == "FoodTestZone");
            Transform procurementArea = Components<Transform>().Single(item => item.name == "ProcurementArea");
            var unchanged = Components<Transform>().Where(item => !item.IsChildOf(station) && !item.IsChildOf(foodRoot) &&
                !item.IsChildOf(procurementArea) &&
                item.name != "WallWest" && item.name != "ProcurementSign")
                .ToDictionary(item => item, item => (item.position, item.rotation, item.localScale));
            M9RestaurantLayoutBuilder.ConfigureProcurementArea(_scene);
            int count = Components<Component>().Length;
            M9RestaurantLayoutBuilder.ConfigureProcurementArea(_scene);
            Assert.That(Components<Component>().Length, Is.EqualTo(count)); Assert.That(originals.All(item => item != null), Is.True);
            foreach (var pair in references) Assert.That(EditorJsonUtility.ToJson(pair.Key), Is.EqualTo(pair.Value), pair.Key.name);
            foreach (var pair in unchanged) Assert.That((pair.Key.position, pair.Key.rotation, pair.Key.localScale), Is.EqualTo(pair.Value), pair.Key.name);
        }

        [Test]
        public void CounterSeparatesBothSidesQueuePointsAreOnlyMarkersAndTestGeometryIsOptIn()
        {
            Transform queues = Components<Transform>().Single(item => item.name == "QueuePoints");
            Assert.That(queues.childCount, Is.EqualTo(4));
            Vector3[] points = Enumerable.Range(0, 4).Select(index => queues.GetChild(index).position).ToArray();
            Assert.That(points.All(point => point.z < 0), Is.True);
            for (int index = 1; index < 4; index++) Assert.That(points[index - 1].z - points[index].z, Is.GreaterThanOrEqualTo(1.2f));
            Assert.That(queues.GetComponentsInChildren<MonoBehaviour>(true), Is.Empty);
            Assert.That(queues.GetComponentsInChildren<Collider>(true), Is.Empty);
            BoxCollider counter = Components<BoxCollider>().Single(item => item.name == "ServiceCounterVolume");
            Assert.That(counter.bounds.min.x, Is.EqualTo(-7).Within(0.001));
            Assert.That(counter.bounds.max.x, Is.EqualTo(7).Within(0.001));
            Assert.That(counter.isTrigger, Is.False);
            foreach (string name in new[] { "PhysicalTestObjects", "KitchenDivider", "LowStep", "TallBlocker", "WorktopVolume", "TableVolume", "AssemblySupplyBench" })
                Assert.That(Components<Transform>().Single(item => item.name == name).gameObject.activeSelf, Is.False, name);
            var supply = new SerializedObject(Components<DevelopmentIngredientSupply>().Single());
            Assert.That(supply.FindProperty("_fixtureSupports").arraySize, Is.EqualTo(1));
            Assert.That(supply.FindProperty("_enableOnStart").boolValue, Is.False);
        }

        [Test]
        public void ApplyingLayoutTwiceKeepsExistingObjectsReferencesAndDoesNotDuplicateFixturesOrMarkers()
        {
            Component[] originals = Components<Component>();
            M9RestaurantLayoutBuilder.ConfigureScene(_scene);
            int count = Components<Transform>().Length;
            M9RestaurantLayoutBuilder.ConfigureScene(_scene);
            Assert.That(Components<Transform>().Length, Is.EqualTo(count));
            Assert.That(originals.All(component => component != null), Is.True);
            Assert.That(Components<FoodItem>(), Has.Length.EqualTo(18));
            Assert.That(Components<CustomerMovement>(), Has.Length.EqualTo(1));
            Assert.That(Components<FoodSimulation>(), Has.Length.EqualTo(1));
        }
    }
}
