using System;
using NUnit.Framework;
using ZeroStarRestaurant.Utilities;

namespace ZeroStarRestaurant.Tests
{
    public sealed class ElectricitySupplyTests
    {
        [Test]
        public void IndividualSwitchChoicesSurviveCutsAndDoNotResetMeters()
        {
            var supply = new ElectricitySupplyState(true);
            var fridge = new ElectricalApplianceState(); var grill = new ElectricalApplianceState(false);
            var meter = new ElectricityMeter(150);
            supply.Advance(3600, new[] { meter });
            for (int cut = 0; cut < 3; cut++)
            {
                supply.SetOn(false);
                Assert.That(fridge.HasPower(supply.IsOn), Is.False); Assert.That(fridge.IsOn, Is.True);
                Assert.That(grill.HasPower(supply.IsOn), Is.False); Assert.That(grill.IsOn, Is.False);
                supply.Advance(3600, new[] { meter }); supply.SetOn(true);
                Assert.That(fridge.HasPower(supply.IsOn), Is.True); Assert.That(grill.HasPower(supply.IsOn), Is.False);
            }
            fridge.SetOn(false); supply.SetOn(false); supply.SetOn(true);
            Assert.That(fridge.HasPower(supply.IsOn), Is.False);
            Assert.That(meter.ConsumedKilowattHours, Is.EqualTo(.15).Within(1e-12));
            Assert.That(new ElectricalApplianceState().IsOn, Is.True, "A new session uses its own initial switch setting.");
        }

        [Test]
        public void NewSessionStartsOffAndCutsAndRestorationPreserveIndividualAndTotalEnergy()
        {
            var supply = new ElectricitySupplyState();
            var grill = new ElectricityMeter(2000); var fridge = new ElectricityMeter(150); var freezer = new ElectricityMeter(200);
            var all = new[] { grill, fridge, freezer };
            supply.Advance(3600, all); Assert.That(supply.ConsumedKilowattHours, Is.Zero);
            supply.SetOn(true); supply.Advance(3600, all);
            Assert.That(grill.ConsumedKilowattHours, Is.EqualTo(2).Within(1e-12));
            Assert.That(fridge.ConsumedKilowattHours, Is.EqualTo(.15).Within(1e-12));
            Assert.That(freezer.ConsumedKilowattHours, Is.EqualTo(.2).Within(1e-12));
            Assert.That(supply.ConsumedKilowattHours, Is.EqualTo(2.35).Within(1e-12));
            supply.SetOn(false); supply.Advance(86400, all); supply.SetOn(false);
            Assert.That(supply.ConsumedKilowattHours, Is.EqualTo(2.35).Within(1e-12));
            supply.SetOn(true); supply.SetOn(true); supply.Advance(1800, all);
            Assert.That(supply.ConsumedKilowattHours, Is.EqualTo(3.525).Within(1e-12));
            Assert.That(grill.ConsumedKilowattHours, Is.EqualTo(3).Within(1e-12));
        }

        [Test]
        public void ConfigurableLoadsAreDeduplicatedAndOnlyOperatingMetersConsume()
        {
            var supply = new ElectricitySupplyState(true); var load = new ElectricityMeter(1234); var idle = new ElectricityMeter(500);
            var zero = new ElectricityMeter(0);
            supply.Advance(3600, new[] { load, load, zero });
            supply.Advance(0, new[] { load }); supply.Advance(3600, Array.Empty<ElectricityMeter>());
            Assert.That(supply.ConsumedKilowattHours, Is.EqualTo(1.234).Within(1e-12));
            Assert.That(idle.ConsumedKilowattHours, Is.Zero); Assert.That(zero.ConsumedKilowattHours, Is.Zero);
        }

        [Test]
        public void LargeAndPartitionedStepsAgreeAndSuppliesAndSessionsAreIndependent()
        {
            var large = new ElectricitySupplyState(true); var split = new ElectricitySupplyState(true);
            var a = new ElectricityMeter(2000); var b = new ElectricityMeter(2000);
            large.Advance(3600, new[] { a });
            for (int i = 0; i < 3600; i++) split.Advance(1, new[] { b });
            Assert.That(split.ConsumedKilowattHours, Is.EqualTo(large.ConsumedKilowattHours).Within(1e-10));
            large.SetOn(false); Assert.That(split.IsOn, Is.True);
            Assert.That(new ElectricitySupplyState().ConsumedKilowattHours, Is.Zero);
        }

        [TestCase(-1)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(double.NegativeInfinity)]
        public void InvalidPowerOrElapsedIsRejectedWithoutMutating(double value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ElectricityMeter(value));
            var supply = new ElectricitySupplyState(true); var meter = new ElectricityMeter(2000);
            supply.Advance(60, new[] { meter }); double before = supply.ConsumedKilowattHours;
            Assert.Throws<ArgumentOutOfRangeException>(() => supply.Advance(value, new[] { meter }));
            Assert.That(supply.ConsumedKilowattHours, Is.EqualTo(before));
            Assert.That(meter.ConsumedKilowattHours, Is.EqualTo(before));
        }

        [Test]
        public void InvalidListsAndOverflowRejectTheWholeStepBeforeAnyMeterChanges()
        {
            var supply = new ElectricitySupplyState(true); var valid = new ElectricityMeter(100);
            var huge = new ElectricityMeter(double.MaxValue);
            Assert.Throws<ArgumentNullException>(() => supply.Advance(1, null));
            Assert.Throws<ArgumentException>(() => supply.Advance(1, new[] { valid, null }));
            Assert.Throws<ArgumentOutOfRangeException>(() => supply.Advance(double.MaxValue, new[] { valid, huge }));
            Assert.That(valid.ConsumedKilowattHours, Is.Zero); Assert.That(huge.ConsumedKilowattHours, Is.Zero);
            Assert.That(supply.ConsumedKilowattHours, Is.Zero);
        }
    }
}
