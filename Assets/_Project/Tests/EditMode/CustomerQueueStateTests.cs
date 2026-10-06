using System;
using System.Linq;
using NUnit.Framework;
using ZeroStarRestaurant.Customers;

namespace ZeroStarRestaurant.Tests
{
    public sealed class CustomerQueueStateTests
    {
        [TestCase(0)] [TestCase(-1)] [TestCase(5)]
        public void CapacityIsBoundedToOneThroughFour(int capacity)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new CustomerQueueState(capacity));

        [Test]
        public void AdmissionReservesExclusivePositionsAndFullQueueDoesNotMutate()
        {
            var queue = new CustomerQueueState(4);
            var customers = Enumerable.Range(1, 4).Select(number => new QueuedCustomerState(number, 30)).ToArray();
            foreach (QueuedCustomerState customer in customers) Assert.That(queue.TryEnqueue(customer), Is.True);
            Assert.That(queue.Customers.Select(customer => customer.QueueIndex), Is.EqualTo(new[] { 0, 1, 2, 3 }));
            Assert.That(customers.Select(customer => customer.InstanceId).Distinct().Count(), Is.EqualTo(4));
            var fifth = new QueuedCustomerState(5, 10);
            Assert.That(queue.TryEnqueue(fifth), Is.False); Assert.That(fifth.QueueIndex, Is.EqualTo(-1));
            Assert.That(queue.Count, Is.EqualTo(4)); Assert.That(queue.IsFull, Is.True);
            Assert.That(queue.Head, Is.SameAs(customers[0]));
        }

        [Test]
        public void DuplicateCustomerOrNumberCannotTakeAnotherPositionOrAnotherQueue()
        {
            var firstQueue = new CustomerQueueState(4); var secondQueue = new CustomerQueueState(4);
            var customer = new QueuedCustomerState(1, 30);
            Assert.That(firstQueue.TryEnqueue(customer), Is.True);
            Assert.That(firstQueue.TryEnqueue(customer), Is.False);
            Assert.That(secondQueue.TryEnqueue(customer), Is.False);
            Assert.That(firstQueue.TryEnqueue(new QueuedCustomerState(1, 30)), Is.False);
            Assert.That(firstQueue.TryEnqueue(null), Is.False); Assert.That(firstQueue.Count, Is.EqualTo(1));
        }

        [Test]
        public void OnlyArrivedHeadCanServeAndOnlyLeavingHeadCanReleaseCapacity()
        {
            var queue = new CustomerQueueState(4);
            var first = new QueuedCustomerState(1, 60); var second = new QueuedCustomerState(2, 60);
            queue.TryEnqueue(first); queue.TryEnqueue(second);
            Assert.That(queue.TryBeginService(first), Is.False, "Entering is not service.");
            first.Arrive(); second.Arrive();
            Assert.That(queue.TryBeginService(second), Is.False); Assert.That(queue.TryBeginService(first), Is.True);
            Assert.That(queue.TryBeginService(first), Is.False);
            Assert.That(queue.TryCompleteExit(first), Is.False, "The service slot stays reserved before exit.");
            Assert.That(queue.TryBeginLeaving(second), Is.False); Assert.That(queue.TryBeginLeaving(first), Is.True);
            Assert.That(queue.TryCompleteExit(second), Is.False); Assert.That(queue.TryCompleteExit(first), Is.True);
            Assert.That(first.QueueIndex, Is.EqualTo(-1)); Assert.That(first.Stage, Is.EqualTo(QueuedCustomerStage.Finished));
            Assert.That(queue.Head, Is.SameAs(second)); Assert.That(second.QueueIndex, Is.Zero);
            Assert.That(second.Stage, Is.EqualTo(QueuedCustomerStage.Advancing));
            Assert.That(queue.TryBeginService(second), Is.False, "An advancing customer cannot order yet.");
            second.Arrive(); Assert.That(queue.TryBeginService(second), Is.True);
            Assert.That(queue.TryCompleteExit(first), Is.False, "No second release.");
        }

        [Test]
        public void ExitShiftsAllRemainingReservationsPreservingIdsAndAnUnfinishedEntrance()
        {
            var queue = new CustomerQueueState(4);
            var members = Enumerable.Range(1, 4).Select(number => new QueuedCustomerState(number, 40 + number)).ToArray();
            foreach (QueuedCustomerState customer in members) queue.TryEnqueue(customer);
            foreach (QueuedCustomerState customer in members.Take(3)) customer.Arrive();
            var ids = members.Select(customer => customer.InstanceId).ToArray();
            queue.TryBeginService(members[0]); queue.TryBeginLeaving(members[0]); queue.TryCompleteExit(members[0]);
            Assert.That(queue.Customers.Select(customer => customer.QueueIndex), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(queue.Customers.Select(customer => customer.InstanceId), Is.EqualTo(ids.Skip(1)));
            Assert.That(members[3].Stage, Is.EqualTo(QueuedCustomerStage.Entering));
            var newcomer = new QueuedCustomerState(5, 1);
            Assert.That(queue.TryEnqueue(newcomer), Is.True); Assert.That(newcomer.QueueIndex, Is.EqualTo(3));
            Assert.That(queue.Count, Is.EqualTo(4));
        }

        [Test]
        public void IndependentPatienceAccumulatesOnlyWaitingAndZeroDoesNotRemoveOrResetACustomer()
        {
            var queue = new CustomerQueueState(4);
            var first = new QueuedCustomerState(1, 2); var second = new QueuedCustomerState(2, 10);
            queue.TryEnqueue(first); queue.TryEnqueue(second); first.AdvancePatience(100);
            Assert.That(first.WaitingSeconds, Is.Zero, "Walking is not waiting.");
            first.Arrive(); second.Arrive(); first.AdvancePatience(3); second.AdvancePatience(1);
            Assert.That(first.RemainingPatienceSeconds, Is.Zero); Assert.That(first.PatienceExhausted, Is.True);
            Assert.That(second.RemainingPatienceSeconds, Is.EqualTo(9)); Assert.That(queue.Count, Is.EqualTo(2));
            Assert.That(queue.TryBeginService(first), Is.True); first.AdvancePatience(2);
            Assert.That(first.WaitingSeconds, Is.EqualTo(5)); Assert.That(first.RemainingPatienceSeconds, Is.Zero);
            queue.TryBeginLeaving(first); first.AdvancePatience(100);
            Assert.That(first.WaitingSeconds, Is.EqualTo(5));
        }

        [Test]
        public void CancellationFinishesEveryMemberAndCannotReinsertTheSameState()
        {
            var queue = new CustomerQueueState(4); var customer = new QueuedCustomerState(1, 1);
            queue.TryEnqueue(customer); queue.Clear(); queue.Clear();
            Assert.That(queue.Count, Is.Zero); Assert.That(customer.Stage, Is.EqualTo(QueuedCustomerStage.Finished));
            Assert.That(queue.TryEnqueue(customer), Is.False);
        }

        [TestCase(-1)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)]
        public void InvalidPatienceAndElapsedTimeAreRejectedWithoutMutation(double invalid)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new QueuedCustomerState(1, invalid));
            var customer = new QueuedCustomerState(1, 10);
            Assert.Throws<ArgumentOutOfRangeException>(() => customer.AdvancePatience(invalid));
            Assert.That(customer.WaitingSeconds, Is.Zero);
        }
    }
}
