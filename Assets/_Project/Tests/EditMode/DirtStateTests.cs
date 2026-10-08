using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ZeroStarRestaurant.Hygiene;

namespace ZeroStarRestaurant.Tests
{
    public sealed class DirtStateTests
    {
        [Test]
        public void NewSurfacesAreCleanIndependentAndHaveDistinctIdentity()
        {
            var profile = new DirtProfile(); var first = new DirtState(profile); var second = new DirtState(profile);
            Assert.That(first.Amount, Is.Zero); Assert.That(first.Category, Is.EqualTo(DirtCategory.Clean));
            Assert.That(first.SurfaceId, Is.Not.EqualTo(second.SurfaceId));
            first.AddDirt(.3, DirtKind.Grease, "Cooking"); Assert.That(second.Amount, Is.Zero);
            Assert.That(first.Profile, Is.SameAs(second.Profile)); Assert.That(first.AmountsByKind.Values.Sum(), Is.EqualTo(.3));
        }

        [TestCase(0, DirtCategory.Clean)] [TestCase(.05, DirtCategory.Clean)]
        [TestCase(.05001, DirtCategory.Used)] [TestCase(.34999, DirtCategory.Used)]
        [TestCase(.35, DirtCategory.Dirty)] [TestCase(.74999, DirtCategory.Dirty)]
        [TestCase(.75, DirtCategory.Filthy)] [TestCase(1, DirtCategory.Filthy)]
        public void CategoriesUseContinuousAmountAndExactConfiguredBoundaries(double amount, DirtCategory expected)
        { Assert.That(new DirtState(new DirtProfile(), amount).Category, Is.EqualTo(expected)); }

        [Test]
        public void ConfiguredThresholdsChangeInterpretationWithoutChangingDirt()
        {
            var standard = new DirtState(new DirtProfile(), .2); var configured = new DirtState(new DirtProfile(.2, .4, .8), .2);
            Assert.That(standard.Category, Is.EqualTo(DirtCategory.Used)); Assert.That(configured.Category, Is.EqualTo(DirtCategory.Clean));
            Assert.That(standard.Amount, Is.EqualTo(configured.Amount));
        }

        [Test]
        public void AddingDirtClampsAndRetainsActualKindOriginUnitAndEventIdentity()
        {
            var state = new DirtState(new DirtProfile()); var unit = Guid.NewGuid(); var changes = new List<DirtChange>(); state.Changed += changes.Add;
            Assert.That(state.AddDirt(.8, DirtKind.Grease, "Cooking", unit), Is.EqualTo(.8));
            Assert.That(state.AddDirt(double.MaxValue, DirtKind.FoodResidue, "Food contact"), Is.EqualTo(.2).Within(1e-12));
            Assert.That(state.Amount, Is.EqualTo(1)); Assert.That(state.AmountsByKind[DirtKind.Grease], Is.EqualTo(.8));
            Assert.That(changes.Count, Is.EqualTo(2)); Assert.That(changes[0].FoodUnitId, Is.EqualTo(unit));
            Assert.That(changes[0].Kind, Is.EqualTo(DirtKind.Grease)); Assert.That(changes[0].Origin, Is.EqualTo("Cooking"));
            Assert.That(changes[0].SurfaceId, Is.EqualTo(state.SurfaceId)); Assert.That(changes[0].Before, Is.Zero); Assert.That(changes[0].After, Is.EqualTo(.8));
            Assert.That(state.AddDirt(.1, DirtKind.GeneralDirt, "Event"), Is.Zero); Assert.That(changes.Count, Is.EqualTo(2));
            Assert.Throws<NotSupportedException>(() => ((IDictionary<DirtKind, double>)state.AmountsByKind)[DirtKind.Grease] = 0);
        }

        [Test]
        public void CleaningRequiresElapsedTimeAndRemovesKindsProportionally()
        {
            var state = new DirtState(new DirtProfile()); state.AddDirt(.2, DirtKind.FoodResidue, "Prep"); state.AddDirt(.4, DirtKind.Grease, "Grill");
            Assert.That(state.Clean(0, .12), Is.Zero); Assert.That(state.Clean(1, 0), Is.Zero);
            state.Clean(2.5, .12); Assert.That(state.Amount, Is.EqualTo(.3).Within(1e-12));
            Assert.That(state.AmountsByKind[DirtKind.FoodResidue], Is.EqualTo(.1).Within(1e-12));
            Assert.That(state.AmountsByKind[DirtKind.Grease], Is.EqualTo(.2).Within(1e-12));
            Assert.That(state.LastChange.Origin, Is.EqualTo("Cleaning")); Assert.That(state.LastChange.Kind, Is.Null);
            state.Clean(double.MaxValue, double.MaxValue); Assert.That(state.Amount, Is.Zero);
            Assert.That(state.AmountsByKind.Values, Is.All.Zero); Assert.That(state.Clean(100, .12), Is.Zero);
        }

        [Test]
        public void SplitAndCombinedCleaningStepsGiveTheSameProgress()
        {
            var first = new DirtState(new DirtProfile(), 1); var second = new DirtState(new DirtProfile(), 1);
            first.Clean(4, .12); for (int i = 0; i < 240; i++) second.Clean(1.0 / 60, .12);
            Assert.That(first.Amount, Is.EqualTo(second.Amount).Within(1e-12));
        }

        [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(-1)]
        public void InvalidAmountsAndTimesLeaveStateUnchanged(double invalid)
        {
            var state = new DirtState(new DirtProfile(), .5);
            Assert.Throws<ArgumentOutOfRangeException>(() => state.AddDirt(invalid, DirtKind.GeneralDirt, "Event"));
            Assert.Throws<ArgumentOutOfRangeException>(() => state.Clean(invalid, .1));
            Assert.Throws<ArgumentOutOfRangeException>(() => state.Clean(1, invalid));
            Assert.That(state.Amount, Is.EqualTo(.5)); Assert.That(state.LastChange, Is.Null);
        }

        [TestCase(.4, .3, .8)] [TestCase(.1, .5, .5)] [TestCase(-.1, .3, .8)]
        [TestCase(.1, .3, 1.1)] [TestCase(double.NaN, .3, .8)]
        public void InvalidThresholdsAreRejected(double clean, double dirty, double filthy)
        { Assert.Throws<ArgumentException>(() => new DirtProfile(clean, dirty, filthy)); }

        [Test]
        public void DevelopmentSetReplacesCompositionExplicitlyAndDoesNotAffectOtherSurfaces()
        {
            var a = new DirtState(new DirtProfile()); var b = new DirtState(new DirtProfile(), .8);
            a.AddDirt(.2, DirtKind.Grease, "Cooking"); a.SetForDevelopment(1);
            Assert.That(a.AmountsByKind[DirtKind.Grease], Is.Zero); Assert.That(a.AmountsByKind[DirtKind.GeneralDirt], Is.EqualTo(1));
            a.SetForDevelopment(0); Assert.That(a.Amount, Is.Zero); Assert.That(b.Amount, Is.EqualTo(.8));
            Assert.Throws<ArgumentOutOfRangeException>(() => a.SetForDevelopment(1.1)); Assert.That(a.Amount, Is.Zero);
        }
    }
}
