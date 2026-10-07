#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;
using ZeroStarRestaurant.Orders;
using ZeroStarRestaurant.Player;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M10CustomerQueueTests
    {
        private Scene _scene;
        private CustomerServiceLoop _service;
        private CustomerQueueController _queue;
        private FoodSimulation _simulation;
        private DeliveryZone _delivery;
        private T[] Components<T>() where T : Component => _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _scene = EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/PrototypeRestaurant.unity", new LoadSceneParameters(LoadSceneMode.Additive));
            yield return null;
            Components<FirstPersonController>().Single().enabled = false;
            _simulation = Components<FoodSimulation>().Single(); _simulation.enabled = false;
            _service = Components<CustomerServiceLoop>().Single(); _queue = _service.Queue;
            _delivery = Components<DeliveryZone>().Single();
            _service.enabled = false;
            var data = new SerializedObject(_service); data.FindProperty("_advanceAutomatically").boolValue = false; data.ApplyModifiedPropertiesWithoutUndo();
            _service.enabled = true;
            Assert.That(_queue.Count, Is.Zero); Assert.That(_simulation.Foods, Is.Empty);
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1000));
        }
        [UnityTearDown]
        public IEnumerator TearDown() { if (_scene.IsValid()) yield return SceneManager.UnloadSceneAsync(_scene); }

        private void Ready()
        {
            _service.Advance(30);
            Assert.That(_queue.Count, Is.EqualTo(4)); Assert.That(_service.Visit.Stage, Is.EqualTo(CustomerStage.Wait));
            Assert.That(_queue.Customers.Count(customer => customer.State.Stage == QueuedCustomerStage.Service), Is.EqualTo(1));
        }
        private void CheckPhysicalClearance()
        {
            Collider[] solids = Components<Collider>().Where(item => item.enabled && item.gameObject.activeInHierarchy && !item.isTrigger &&
                item.GetComponentInParent<FoodItem>() == null && item.GetComponentInParent<CustomerMovement>() == null).ToArray();
            Assert.That(_queue.Customers.Select(customer => customer.State.QueueIndex).Distinct().Count(), Is.EqualTo(_queue.Count));
            foreach (QueuedCustomer customer in _queue.Customers)
            {
                Assert.That(customer.transform.position.z, Is.LessThan(0), "Customers stay on the public side.");
                BoxCollider body = customer.GetComponent<BoxCollider>();
                foreach (Collider solid in solids)
                    Assert.That(Physics.ComputePenetration(body, body.transform.position, body.transform.rotation, solid,
                        solid.transform.position, solid.transform.rotation, out _, out float depth) && depth > 0.01f,
                        Is.False, customer.name + " crossed " + solid.name);
                foreach (QueuedCustomer other in _queue.Customers.Where(other => other != customer))
                    Assert.That(Physics.ComputePenetration(body, body.transform.position, body.transform.rotation, other.GetComponent<BoxCollider>(),
                        other.transform.position, other.transform.rotation, out _, out float depth) && depth > 0.01f,
                        Is.False, customer.name + " overlaps " + other.name);
            }
        }

        [UnityTest]
        public IEnumerator EntryReservesAUniqueSlotWithoutAnOrderAndFullQueueBlocksNewObjects()
        {
            _service.Advance(0.55);
            Assert.That(_queue.Count, Is.EqualTo(1)); Assert.That(_queue.Head.State.Stage, Is.EqualTo(QueuedCustomerStage.Entering));
            Assert.That(_service.Visit, Is.Null); Assert.That(_queue.TryAdmit(), Is.False, "Entrance is still occupied.");
            for (int step = 0; step < 600; step++) { _service.Advance(0.05); CheckPhysicalClearance(); }
            Assert.That(_queue.IsFull, Is.True); Assert.That(_queue.Customers.Select(customer => customer.State.QueueIndex), Is.EqualTo(new[] { 0, 1, 2, 3 }));
            var ids = _queue.Customers.Select(customer => customer.State.InstanceId).ToArray();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(4));
            int objects = Components<CustomerMovement>().Length; int admitted = _queue.AdmittedCount;
            Assert.That(_queue.TryAdmit(), Is.False); _service.Advance(100);
            Assert.That(Components<CustomerMovement>().Length, Is.EqualTo(objects)); Assert.That(_queue.AdmittedCount, Is.EqualTo(admitted));
            Assert.That(_queue.Customers.Select(customer => customer.State.InstanceId), Is.EqualTo(ids));
            Assert.That(_service.Visit.InstanceId, Is.EqualTo(ids[0])); Assert.That(_service.Visit.Order.InstanceId, Is.Not.EqualTo(ids[0]));
            Assert.That(_queue.Template.gameObject.activeSelf, Is.False); Assert.That(_queue.Template.State, Is.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OnlyServicePositionReceivesAndExitAdvancesAllPositionsBeforeTheNextOrderAndPayment()
        {
            Ready(); QueuedCustomer first = _queue.Head; QueuedCustomerState[] original = _queue.Customers.Select(customer => customer.State).ToArray();
            var firstVisit = _service.Visit;
            DishItem firstDish = null; yield return BuildDish(false, dish => firstDish = dish);
            var firstIngredientIds = firstDish.State.Components.Select(food => food.InstanceId).ToArray();
            Vector3 atService = first.transform.position; first.transform.position += Vector3.left;
            Assert.That(_service.TryDeliver(firstDish), Is.False); Assert.That(firstDish.State.IsSold, Is.False);
            first.transform.position = atService; PlaceAndPoll(firstDish);
            Assert.That(_service.LastResult.Accepted, Is.True); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1350));
            Assert.That(first.DishCarrier.Dish, Is.SameAs(firstDish));
            Assert.That(_queue.Customers.Skip(1).All(customer => customer.DishCarrier.Dish == null), Is.True);
            Assert.That(_service.TryDeliver(firstDish), Is.False); _delivery.Poll();
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1350)); Assert.That(_queue.Count, Is.EqualTo(4));
            Assert.That(_queue.TryAdmit(), Is.False);
            double patience = first.State.WaitingSeconds;
            Assert.That(_service.ForceNextOrder(1), Is.True);
            for (int step = 0; step < 500 && _queue.Head == first; step++) { _service.Advance(0.05); CheckPhysicalClearance(); }
            Assert.That(original[0].Stage, Is.EqualTo(QueuedCustomerStage.Finished));
            Assert.That(original[0].WaitingSeconds, Is.EqualTo(patience), "Result/exit do not consume patience.");
            Assert.That(_queue.Customers.Select(customer => customer.State), Is.EqualTo(original.Skip(1)));
            Assert.That(_queue.Customers.Select(customer => customer.State.QueueIndex), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(_queue.Head.State.Stage, Is.EqualTo(QueuedCustomerStage.Advancing)); Assert.That(_service.Visit, Is.Null);
            Assert.That(_simulation.Foods, Is.Empty, "The originals retire at Exit.");
            Assert.That(firstDish.gameObject.activeSelf, Is.False);
            _service.Advance(0.05); Assert.That(_queue.Count, Is.EqualTo(4));
            QueuedCustomer newcomer = _queue.Customers.Last(); Assert.That(newcomer.State.Number, Is.EqualTo(5));
            Assert.That(original.Select(customer => customer.InstanceId), Has.No.Member(newcomer.State.InstanceId));
            for (int step = 0; step < 80 && (_service.Visit == null || _service.Visit.Stage != CustomerStage.Wait); step++) _service.Advance(0.05);
            Assert.That(_service.Visit.InstanceId, Is.EqualTo(original[1].InstanceId));
            Assert.That(_service.Visit.Order.InstanceId, Is.Not.EqualTo(firstVisit.Order.InstanceId));
            Assert.That(_service.Visit.Order.Offer.Dish.Id, Is.EqualTo("dish.cheeseburger"));
            Assert.That(_service.ActiveCustomer.transform.position, Is.EqualTo(_queue.ServicePosition.position));
            yield return null; Assert.That(firstDish == null, Is.True);
            DishItem secondDish = null; yield return BuildDish(true, dish => secondDish = dish); PlaceAndPoll(secondDish);
            Assert.That(_service.LastResult.Accepted, Is.True); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1825));
            Assert.That(_service.LastResult.Evaluation.OrderId, Is.EqualTo(_service.Visit.Order.InstanceId));
            Assert.That(_service.LastResult.Evaluation.DeliveredDish.Ingredients.Any(food => firstIngredientIds.Contains(food.InstanceId)), Is.False);
            Assert.That(_service.TryDeliver(secondDish), Is.False); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1825));
            Assert.That(_queue.Customers.Skip(1).All(customer => customer.DishCarrier.Dish == null), Is.True);
        }

        [UnityTest]
        public IEnumerator RejectedPlacementCannotBlockTheNextMatchingCustomerOrKeepThePreviousReservation()
        {
            Ready(); var firstOrderId = _service.Visit.Order.InstanceId;
            DishItem dish = null; yield return BuildDish(true, created => dish = created);
            var dishId = dish.State.InstanceId; var foods = dish.State.Components.ToArray(); PlaceAndPoll(dish);
            Assert.That(_service.LastResult.Accepted, Is.False); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(825));
            var feedback = Components<OrderFeedback>().Single();
            Assert.That(feedback.Text.IndexOf("DELIVERY REJECTED"), Is.LessThan(feedback.Text.IndexOf("QUEUE ")),
                "The rejection reason must be visible before the scrollable queue details.");
            Assert.That(dish.State.IsSold, Is.False); Assert.That(dish.GetComponent<Pickup>().enabled, Is.True);
            _service.Advance(30); yield return null;
            Assert.That(_service.Visit.Order.InstanceId, Is.Not.EqualTo(firstOrderId)); Assert.That(_service.Visit.Stage, Is.EqualTo(CustomerStage.Wait));
            Assert.That(_service.Visit.Order.Offer.Dish.Id, Is.EqualTo("dish.cheeseburger"));
            Assert.That(_queue.Head.State.InstanceId, Is.EqualTo(_service.Visit.InstanceId));
            Assert.That(_service.ActiveCustomer.transform.position, Is.EqualTo(_queue.ServicePosition.position));
            _delivery.Poll(); Assert.That(_service.Visit.Order.IsCompleted, Is.True,
                "The previous customer's rejected placement must not block the next matching order.");
            Assert.That(_service.LastResult.Accepted, Is.True);
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1475)); Assert.That(dish.State.InstanceId, Is.EqualTo(dishId));
            Assert.That(dish.State.Components, Is.EqualTo(foods));
            Assert.That(_service.ActiveDishCarrier.Dish, Is.SameAs(dish));
            _delivery.Poll(); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1475));
        }

        [UnityTest]
        public IEnumerator TwoAutomaticPaidVisitsCarryTheOriginalDishUntilExitAndFreeThePadForTheNextCustomer()
        {
            Ready();
            var nextCustomerId = _queue.Customers[1].State.InstanceId;
            Assert.That(_service.ForceNextOrder(1), Is.True);
            for (int visitIndex = 0; visitIndex < 2; visitIndex++)
            {
                if (visitIndex > 0)
                {
                    for (int frame = 0; frame < 500 && (_service.Visit == null || _service.Visit.Stage != CustomerStage.Wait); frame++)
                        yield return new WaitForFixedUpdate();
                    Assert.That(_service.Visit, Is.Not.Null, "The next customer must reach Service Position.");
                    Assert.That(_service.Visit.InstanceId, Is.EqualTo(nextCustomerId));
                    Assert.That(_service.Visit.Stage, Is.EqualTo(CustomerStage.Wait));
                }
                var visit = _service.Visit;
                var customer = _service.ActiveCustomer;
                var carrier = _service.ActiveDishCarrier;
                DishItem dish = null; yield return BuildDish(visitIndex == 1, value => dish = value);
                var dishId = dish.State.InstanceId;
                FoodItem[] originals = dish.GetComponentsInChildren<FoodItem>();
                Vector3[] offsets = originals.Select(food => dish.transform.InverseTransformPoint(food.transform.position)).ToArray();
                BoxCollider pad = _delivery.Support;
                Rigidbody body = dish.GetComponent<Rigidbody>();
                body.rotation = Quaternion.Euler(0f, visitIndex == 0 ? 25f : -30f, 0f);
                body.position = pad.bounds.center + new Vector3(visitIndex == 0 ? -.4f : .4f, 1f, visitIndex == 0 ? .2f : -.2f);
                Physics.SyncTransforms();
                body.position += Vector3.up * (pad.bounds.max.y + .08f - dish.GetComponent<BoxCollider>().bounds.min.y);
                body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
                var data = new SerializedObject(_service); data.FindProperty("_advanceAutomatically").boolValue = true; data.ApplyModifiedPropertiesWithoutUndo();
                for (int frame = 0; frame < 100 && !dish.State.IsSold; frame++) yield return new WaitForFixedUpdate();
                Assert.That(dish.State.IsSold, Is.True, _delivery.PlacementMessage);
                Assert.That(visit.Order.Result.Accepted, Is.True);
                Assert.That(carrier.Dish, Is.SameAs(dish));
                Assert.That(visit.Order.Result.Evaluation.DeliveredDish.InstanceId, Is.EqualTo(dishId));
                long expectedBalance = visitIndex == 0 ? 1350 : 1825;
                Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(expectedBalance));
                Assert.That(_service.Ledger.Transactions.Count(transaction => transaction.Category == LedgerCategory.Sales), Is.EqualTo(visitIndex + 1));
                for (int frame = 0; frame < 1600 && visit.Stage != CustomerStage.Finished; frame++)
                {
                    yield return new WaitForFixedUpdate(); yield return null;
                    if (visit.Stage == CustomerStage.Finished) break;
                    Assert.That(carrier.Dish, Is.SameAs(dish), "A paid customer cannot leave without the sold dish.");
                    Assert.That(dish.transform.IsChildOf(customer.transform), Is.True);
                    for (int index = 0; index < originals.Length; index++)
                        Assert.That(Vector3.Distance(originals[index].transform.position, dish.transform.TransformPoint(offsets[index])), Is.LessThan(.002f));
                    Assert.That(dish.GetComponentsInChildren<Collider>(true).All(collider => !collider.enabled), Is.True);
                    Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(expectedBalance));
                }
                Assert.That(visit.Stage, Is.EqualTo(CustomerStage.Finished), "Exit must release the FIFO reservation.");
                yield return null;
                Assert.That(dish == null && customer == null, Is.True);
                Assert.That(originals.All(food => food == null), Is.True);
                Assert.That(_simulation.Foods, Is.Empty);
                Assert.That(_queue.Customers.All(queued => queued.State.InstanceId != visit.InstanceId), Is.True);
            }
        }

        [UnityTest]
        public IEnumerator AutomaticNextMatchingCustomerCollectsTheRejectedDishWithoutMovingIt()
        {
            Ready();
            var rejectedVisit = _service.Visit;
            var matchingCustomerId = _queue.Customers[1].State.InstanceId;
            Assert.That(_service.ForceNextOrder(1), Is.True);
            DishItem dish = null; yield return BuildDish(true, value => dish = value);
            var originalDishId = dish.State.InstanceId;
            var originalFoods = dish.State.Components.ToArray();
            Rigidbody body = dish.GetComponent<Rigidbody>();
            body.position = _delivery.Support.bounds.center + Vector3.up;
            Physics.SyncTransforms();
            body.position += Vector3.up * (_delivery.Support.bounds.max.y + .08f - dish.GetComponent<BoxCollider>().bounds.min.y);
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            var data = new SerializedObject(_service); data.FindProperty("_advanceAutomatically").boolValue = true; data.ApplyModifiedPropertiesWithoutUndo();
            for (int frame = 0; frame < 1600 && !dish.State.IsSold; frame++)
                yield return new WaitForFixedUpdate();
            Assert.That(rejectedVisit.Stage, Is.EqualTo(CustomerStage.Finished));
            Assert.That(rejectedVisit.Order.Result.Accepted, Is.False);
            Assert.That(dish.State.IsSold, Is.True, "An old rejection cannot block a later matching customer.");
            Assert.That(_service.Visit.InstanceId, Is.EqualTo(matchingCustomerId));
            Assert.That(_service.ActiveDishCarrier.Dish, Is.SameAs(dish));
            Assert.That(dish.State.InstanceId, Is.EqualTo(originalDishId));
            Assert.That(dish.State.Components, Is.EqualTo(originalFoods));
            Assert.That(_service.ResultRevision, Is.EqualTo(2));
            Assert.That(_service.Ledger.Transactions.Count(transaction => transaction.Category == LedgerCategory.Sales), Is.EqualTo(1));
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1475));
            Assert.That(_queue.Customers.All(customer => customer.State.InstanceId != rejectedVisit.InstanceId), Is.True);
        }

        [UnityTest]
        public IEnumerator PatienceIsIndependentClampsAtZeroAndNeverBlocksTheExistingPayment()
        {
            var data = new SerializedObject(_queue.Template); data.FindProperty("_initialPatienceSeconds").floatValue = 2; data.ApplyModifiedPropertiesWithoutUndo();
            Ready(); Assert.That(_queue.Customers.All(customer => customer.State.PatienceExhausted), Is.True);
            Assert.That(_queue.Count, Is.EqualTo(4)); Assert.That(_service.Visit.Stage, Is.EqualTo(CustomerStage.Wait));
            Assert.That(OrderFeedback.QueueText(_queue), Does.Contain("0: debug only"));
            DishItem dish = null; yield return BuildDish(false, created => dish = created); PlaceAndPoll(dish);
            Assert.That(_service.LastResult.Accepted, Is.True); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1350));
        }

        [UnityTest]
        public IEnumerator CancellationClearsAllCustomersAndSoldFoodWithoutResettingTheSessionLedger()
        {
            Ready(); var ledger = _service.Ledger; var ids = _queue.Customers.Select(customer => customer.State.InstanceId).ToArray();
            DishItem dish = null; yield return BuildDish(false, created => dish = created); PlaceAndPoll(dish);
            _service.enabled = false; Assert.That(_queue.Count, Is.Zero); Assert.That(_service.Visit, Is.Null);
            Assert.That(_simulation.Foods, Is.Empty); Assert.That(ledger.BalanceCents, Is.EqualTo(1350));
            yield return null; Assert.That(dish == null, Is.True);
            _service.enabled = true; _service.Advance(30);
            Assert.That(_service.Ledger, Is.SameAs(ledger)); Assert.That(_queue.Count, Is.EqualTo(4));
            Assert.That(_queue.Customers.Any(customer => ids.Contains(customer.State.InstanceId)), Is.False);
            Assert.That(ledger.BalanceCents, Is.EqualTo(1350));
        }

        [UnityTest]
        public IEnumerator QueueMovementAndWaitingAreDeterministicAcrossElapsedTimePartitions()
        {
            _service.Advance(20);
            Vector3[] positions = _queue.Customers.Select(customer => customer.transform.position).ToArray();
            double[] waiting = _queue.Customers.Select(customer => customer.State.WaitingSeconds).ToArray();
            QueuedCustomerStage[] stages = _queue.Customers.Select(customer => customer.State.Stage).ToArray();
            _service.enabled = false; _service.enabled = true;
            for (int step = 0; step < 153; step++) _service.Advance(0.13);
            _service.Advance(0.11);
            Assert.That(_queue.Customers.Select(customer => customer.transform.position), Is.EqualTo(positions));
            Assert.That(_queue.Customers.Select(customer => customer.State.Stage), Is.EqualTo(stages));
            for (int index = 0; index < waiting.Length; index++) Assert.That(_queue.Customers[index].State.WaitingSeconds, Is.EqualTo(waiting[index]).Within(0.000001));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RetargetingAnEntrantAfterExitPreservesTheDoorwayAndApproachCorners()
        {
            var data = new SerializedObject(_queue); data.FindProperty("_arrivalIntervalSeconds").floatValue = 100; data.ApplyModifiedPropertiesWithoutUndo();
            _service.Advance(10); Assert.That(_queue.Count, Is.EqualTo(1));
            DishItem dish = null; yield return BuildDish(false, created => dish = created); PlaceAndPoll(dish);
            _service.Advance(10.4); Assert.That(_service.Visit.Stage, Is.EqualTo(CustomerStage.Leave));
            var oldHead = _queue.Head;
            Assert.That(_queue.TryAdmit(5), Is.True); QueuedCustomer entrant = _queue.Customers.Last();
            for (int step = 0; step < 240 && (_service.Visit == null || _service.Visit.InstanceId != entrant.State.InstanceId || _service.Visit.Stage != CustomerStage.Wait); step++)
            { _service.Advance(0.05); CheckPhysicalClearance(); }
            Assert.That(oldHead.State.Stage, Is.EqualTo(QueuedCustomerStage.Finished));
            Assert.That(_queue.Head, Is.SameAs(entrant)); Assert.That(_service.Visit.Stage, Is.EqualTo(CustomerStage.Wait));
            Assert.That(entrant.State.QueueIndex, Is.Zero); Assert.That(entrant.transform.position, Is.EqualTo(_queue.ServicePosition.position));
            yield return null;
        }

        private IEnumerator BuildDish(bool cheese, System.Action<DishItem> completed)
        {
            BoxCollider support = Components<BoxCollider>().Single(item => item.name == "PrepSupport3");
            PhysicalDishAssembly physical = Components<PhysicalDishAssembly>().Single();
            FoodItem last = null;
            IngredientPurchaseStation station = Components<IngredientPurchaseStation>().Single();
            int[] products = cheese ? new[] { 0, 1, 2, 0 } : new[] { 0, 1, 0 };
            float top = support.bounds.max.y;
            foreach (int product in products)
            {
                Assert.That(station.TryPurchase(product, out FoodItem food), Is.True, station.LastMessage);
                if (product == 1)
                {
                    food.State.SetTemperature(120);
                    food.State.Advance(45, new ThermalEnvironment(120, allowsCooking: true), 0);
                }
                float half = food.GetComponent<BoxCollider>().bounds.extents.y;
                food.GetComponent<Rigidbody>().position = new Vector3(support.transform.position.x, top + half + 0.003f, support.transform.position.z);
                top += 2 * half + 0.003f; last = food; Physics.SyncTransforms();
            }
            for (int frame = 0; frame < 40; frame++) yield return new WaitForFixedUpdate();
            Assert.That(physical.PreviewName(last), Is.EqualTo(cheese ? "Cheeseburger" : "Hamburger"));
            Assert.That(physical.TryFinalize(last, out DishItem dish), Is.True); completed(dish);
        }
        private void PlaceAndPoll(DishItem dish)
        {
            BoxCollider pad = Components<BoxCollider>().Single(item => item.name == "DeliveryPad");
            Rigidbody body = dish.GetComponent<Rigidbody>(); body.interpolation = RigidbodyInterpolation.None;
            dish.transform.position = pad.transform.position + Vector3.up; Physics.SyncTransforms();
            dish.transform.position += Vector3.up * (pad.bounds.max.y - dish.GetComponent<BoxCollider>().bounds.min.y);
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; Physics.SyncTransforms();
            Assert.That(dish.GetComponent<BoxCollider>().bounds.min.y, Is.EqualTo(pad.bounds.max.y).Within(0.001f));
            _delivery.Poll();
            Assert.That(_service.LastResult, Is.Not.Null, "A supported released dish must be evaluated at Service Position.");
        }
    }
}
#endif
