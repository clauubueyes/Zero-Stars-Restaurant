using System;
using System.Linq;
using NUnit.Framework;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Utilities;

namespace ZeroStarRestaurant.Tests
{
    public sealed class ElectricityBillingTests
    {
        private static OperatingCostPolicy Policy => new OperatingCostPolicy(30, new[] { new DailyFixedCost("rent", "Rent", 300) });

        [Test]
        public void TwoDaysAccrueOnePeriodWithoutChargingElectricityAndResetOnlyDailyMeters()
        {
            var supply = new ElectricitySupplyState(true); var meter = new ElectricityMeter(2000);
            var ledger = new PaymentLedger(1000); Guid period = supply.BillingPeriodId;
            supply.Advance(3600, new[] { meter }); ledger.TrySettleDay(Policy, supply.DailyConsumedKilowattHours, out DailySummary first);
            supply.CompleteDay(); meter.CompleteDay();
            Assert.That(first.ElectricityAccruedCents, Is.EqualTo(60)); Assert.That(first.ElectricityPaidCents, Is.Zero);
            Assert.That(ledger.BalanceCents, Is.EqualTo(700));
            // Appliances keep running while reviewing the summary; the billing period captures it too.
            supply.Advance(1800, new[] { meter });
            Assert.That(supply.DailyConsumedKilowattHours, Is.EqualTo(2)); Assert.That(supply.PendingKilowattHours, Is.EqualTo(3));
            ledger.TryBeginNextDay(2); supply.BeginDay(); meter.BeginDay();
            Assert.That(supply.BillingPeriodId, Is.EqualTo(period)); Assert.That(supply.PendingKilowattHours, Is.EqualTo(3));
            Assert.That(supply.DailyConsumedKilowattHours, Is.Zero); Assert.That(meter.DailyConsumedKilowattHours, Is.Zero);
            supply.Advance(3600, new[] { meter }); ledger.TrySettleDay(Policy, supply.DailyConsumedKilowattHours, out DailySummary second);
            Assert.That(second.ElectricityAccruedCents, Is.EqualTo(60)); Assert.That(ledger.BalanceCents, Is.EqualTo(400));
            Assert.That(supply.PendingKilowattHours, Is.EqualTo(5)); Assert.That(Policy.ElectricityCostCents(supply.PendingKilowattHours), Is.EqualTo(150));
            Assert.That(ledger.Transactions.All(item => item.Category == LedgerCategory.FixedCost), Is.True);
        }

        [Test]
        public void BillPostsActualReceiptOnceAndPreservesDailyHistoryAndSwitches()
        {
            var supply = new ElectricitySupplyState(true); var meter = new ElectricityMeter(2350);
            var ledger = new PaymentLedger(0); supply.Advance(3600, new[] { meter }); Guid period = supply.BillingPeriodId;
            Assert.That(ledger.TryPayElectricityBill(period, Policy, supply.PendingKilowattHours, out ElectricityBillReceipt receipt), Is.True);
            Assert.That(receipt.AmountCents, Is.EqualTo(71)); Assert.That(receipt.ConsumedKilowattHours, Is.EqualTo(2.35).Within(1e-12));
            Assert.That(receipt.BillingPeriodId, Is.EqualTo(period)); Assert.That(receipt.PaidOnDayNumber, Is.EqualTo(1));
            Assert.That(ledger.BalanceCents, Is.EqualTo(-71));
            Assert.That(ledger.TryPayElectricityBill(period, Policy, 100, out _), Is.False);
            Assert.That(supply.TryCompleteBillingPeriod(period), Is.True); Assert.That(supply.TryCompleteBillingPeriod(period), Is.False);
            Assert.That(supply.PendingKilowattHours, Is.Zero); Assert.That(supply.DailyConsumedKilowattHours, Is.EqualTo(2.35).Within(1e-12));
            Assert.That(supply.ConsumedKilowattHours, Is.EqualTo(2.35).Within(1e-12)); Assert.That(meter.ConsumedKilowattHours, Is.EqualTo(2.35).Within(1e-12));
            Assert.That(supply.IsOn, Is.True); Assert.That(ledger.ElectricityBills.Single(), Is.SameAs(receipt));
            supply.Advance(3600, new[] { meter });
            Assert.That(supply.PendingKilowattHours, Is.EqualTo(2.35).Within(1e-12)); Assert.That(supply.BillingPeriodId, Is.Not.EqualTo(period));
            Assert.That(ledger.TryPayElectricityBill(supply.BillingPeriodId, Policy, supply.PendingKilowattHours, out _), Is.True);
            Assert.That(ledger.BalanceCents, Is.EqualTo(-142)); Assert.That(ledger.ElectricityBills, Has.Count.EqualTo(2));
        }

        [TestCase(false)] [TestCase(true)]
        public void PaymentBeforeOrAfterClosingIsRealCashButAccrualIsSeparate(bool afterClosing)
        {
            var ledger = new PaymentLedger(1000); ledger.TrySpend(Guid.NewGuid(), Guid.NewGuid(), 80);
            ledger.TryRecord(Guid.NewGuid(), Guid.NewGuid(), 500, true);
            DailySummary before = null;
            if (afterClosing) ledger.TrySettleDay(Policy, 2.35, out before);
            ledger.TryPayElectricityBill(Guid.NewGuid(), Policy, 5, out _);
            ledger.TrySettleDay(Policy, 2.35, out DailySummary summary);
            Assert.That(summary.OperatingNetCents, Is.EqualTo(120)); Assert.That(summary.ElectricityAccruedCents, Is.EqualTo(71));
            Assert.That(summary.ElectricityPaidCents, Is.EqualTo(150)); Assert.That(summary.NetCents, Is.EqualTo(-30));
            Assert.That(summary.ClosingBalanceCents, Is.EqualTo(970)); Assert.That(ledger.BalanceCents, Is.EqualTo(970));
            Assert.That(summary.Transactions.Count(item => item.CostId == "rent"), Is.EqualTo(1));
            if (afterClosing)
            { Assert.That(before.ClosingBalanceCents, Is.EqualTo(1120)); Assert.That(summary, Is.Not.SameAs(before)); }
            Assert.That(ledger.Summaries.Single(), Is.SameAs(summary));
            ledger.TryBeginNextDay(2); ledger.TrySettleDay(Policy, 0, out DailySummary next);
            Assert.That(next.OpeningBalanceCents, Is.EqualTo(970)); Assert.That(next.ElectricityPaidCents, Is.Zero);
        }

        [Test]
        public void WholePeriodRoundsOnceInsteadOfAddingRoundedDailyAccruals()
        {
            var supply = new ElectricitySupplyState(true); var meter = new ElectricityMeter(1000);
            for (int day = 0; day < 4; day++)
            {
                supply.BeginDay(); meter.BeginDay(); supply.Advance(18, new[] { meter });
                Assert.That(Policy.ElectricityCostCents(supply.DailyConsumedKilowattHours), Is.Zero);
            }
            Assert.That(supply.PendingKilowattHours, Is.EqualTo(.02).Within(1e-12));
            Assert.That(Policy.ElectricityCostCents(supply.PendingKilowattHours), Is.EqualTo(1));
        }

        [TestCase(0)] [TestCase(-1)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(double.MaxValue)]
        public void InvalidEmptyOrOverflowingBillCannotMutateTheLedger(double energy)
        {
            var ledger = new PaymentLedger(1000); Guid period = Guid.NewGuid();
            if (energy == 0) Assert.That(ledger.TryPayElectricityBill(period, Policy, energy, out _), Is.False);
            else Assert.Catch<Exception>(() => ledger.TryPayElectricityBill(period, Policy, energy, out _));
            Assert.That(ledger.BalanceCents, Is.EqualTo(1000)); Assert.That(ledger.Transactions, Is.Empty); Assert.That(ledger.ElectricityBills, Is.Empty);
            Assert.That(ledger.TryPayElectricityBill(period, Policy, 1, out _), Is.True);
        }

        [Test]
        public void OffSupplyAndEmptyOperatingMetersDoNotAccruePendingEnergy()
        {
            var supply = new ElectricitySupplyState(); var meter = new ElectricityMeter(2000);
            supply.Advance(3600, new[] { meter }); supply.SetOn(true); supply.Advance(3600, new ElectricityMeter[0]);
            Assert.That(supply.PendingKilowattHours, Is.Zero); Assert.That(supply.DailyConsumedKilowattHours, Is.Zero);
        }
    }
}
