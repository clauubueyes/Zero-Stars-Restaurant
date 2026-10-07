using System;
using System.Linq;
using NUnit.Framework;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Utilities;

namespace ZeroStarRestaurant.Tests
{
    public sealed class OperatingCostsTests
    {
        private static OperatingCostPolicy Policy(int rate = 30, int rent = 300) =>
            new OperatingCostPolicy(rate, new[] { new DailyFixedCost("rent", "Rent", rent) });

        [TestCase(0, 0)] [TestCase(2.35, 71)] [TestCase(.016, 0)] [TestCase(.0166666666666667, 1)]
        public void BillUsesRealMeterKwhAndRoundsOnlyTheFinalAmount(double kwh, long cents)
        {
            var supply = new ElectricitySupplyState(true);
            var meter = new ElectricityMeter(2350);
            supply.Advance(kwh * 3600000 / 2350, new[] { meter });
            Assert.That(Policy().ElectricityCostCents(supply.DailyConsumedKilowattHours), Is.EqualTo(cents));
            var ledger = new PaymentLedger(1000);
            ledger.TrySettleDay(Policy(), supply.DailyConsumedKilowattHours, out DailySummary summary);
            Assert.That(summary.ConsumedKilowattHours, Is.EqualTo(meter.DailyConsumedKilowattHours));
            Assert.That(summary.ElectricityCents, Is.EqualTo(cents));
            Assert.That(summary.ClosingBalanceCents, Is.EqualTo(700 - cents));
        }
        [Test]
        public void ConsumptionPartitionsAndSupplyCutsProduceOneRoundedCharge()
        {
            var supply = new ElectricitySupplyState(true); var meter = new ElectricityMeter(2350);
            for (int index = 0; index < 3600; index++) supply.Advance(1, new[] { meter });
            supply.SetOn(false); supply.Advance(3600, new[] { meter });
            Assert.That(supply.DailyConsumedKilowattHours, Is.EqualTo(2.35).Within(1e-12));
            Assert.That(Policy().ElectricityCostCents(supply.DailyConsumedKilowattHours), Is.EqualTo(71));
        }
        [Test]
        public void SummaryContainsRealPurchasesSalesAndFixedCostsAndReconcilesBalance()
        {
            var ledger = new PaymentLedger(1000); Guid unit = Guid.NewGuid(), purchase = Guid.NewGuid(), dish = Guid.NewGuid(), order = Guid.NewGuid();
            Assert.That(ledger.TrySpend(purchase, unit, 80), Is.True);
            Assert.That(ledger.TrySpend(purchase, Guid.NewGuid(), 80), Is.False);
            Assert.That(ledger.TrySpend(Guid.NewGuid(), Guid.NewGuid(), 1000), Is.False);
            Assert.That(ledger.TryRecord(order, dish, 500, true), Is.True);
            Assert.That(ledger.TryRecord(Guid.NewGuid(), dish, 500, true), Is.False);
            Assert.That(ledger.TryRecord(Guid.NewGuid(), Guid.NewGuid(), 650, false), Is.True);
            Assert.That(ledger.BalanceCents, Is.EqualTo(1420), "No continuous electricity/rent charges.");
            ledger.TrySettleDay(Policy(), 2.35, out DailySummary summary);
            Assert.That(summary.SalesCents, Is.EqualTo(500)); Assert.That(summary.PurchasesCents, Is.EqualTo(80));
            Assert.That(summary.ElectricityCents, Is.EqualTo(71)); Assert.That(summary.FixedCostsCents, Is.EqualTo(300));
            Assert.That(summary.NetCents, Is.EqualTo(49)); Assert.That(summary.ClosingBalanceCents, Is.EqualTo(1049));
            Assert.That(ledger.BalanceCents, Is.EqualTo(summary.OpeningBalanceCents + summary.NetCents));
            Assert.That(summary.Transactions.Single(item => item.Category == LedgerCategory.Procurement).ObjectId, Is.EqualTo(unit));
            Assert.That(summary.Transactions.Single(item => item.Category == LedgerCategory.Sales).TransactionId, Is.EqualTo(order));
            Assert.That(summary.Transactions, Has.Count.EqualTo(4));
        }
        [Test]
        public void RepeatedSettlementAndNextDayCallsNeverChargeTwiceAndRetainHistory()
        {
            var ledger = new PaymentLedger(0);
            Assert.That(ledger.TryBeginNextDay(2), Is.False);
            ledger.TrySettleDay(Policy(), 0, out DailySummary first);
            for (int index = 0; index < 10; index++)
            {
                Assert.That(ledger.TrySettleDay(Policy(500, 999), 100, out DailySummary repeated), Is.True);
                Assert.That(repeated, Is.SameAs(first));
                Assert.That(ledger.TryBeginNextDay(3), Is.False);
            }
            Assert.That(ledger.BalanceCents, Is.EqualTo(-300)); Assert.That(ledger.Transactions, Has.Count.EqualTo(2));
            Assert.That(ledger.TrySpend(Guid.NewGuid(), Guid.NewGuid(), 1), Is.False);
            Assert.That(ledger.TryRecord(Guid.NewGuid(), Guid.NewGuid(), 500, true), Is.False);
            Assert.That(ledger.TryBeginNextDay(2), Is.True); Assert.That(ledger.TryBeginNextDay(2), Is.False);
            Assert.That(ledger.CurrentSummary, Is.Null); Assert.That(ledger.BalanceCents, Is.EqualTo(-300));
            Assert.That(ledger.Summaries.Single(), Is.SameAs(first));
        }
        [Test]
        public void TwoDaysResetDailyAccountingPreserveDeduplicationAndAllowNegativeBalance()
        {
            var ledger = new PaymentLedger(150); Guid purchase = Guid.NewGuid(), unit = Guid.NewGuid(), order = Guid.NewGuid(), dish = Guid.NewGuid();
            ledger.TrySpend(purchase, unit, 150); ledger.TryRecord(order, dish, 500, true);
            ledger.TrySettleDay(Policy(), 0, out DailySummary first);
            Assert.That(first.NetCents, Is.EqualTo(50)); Assert.That(ledger.BalanceCents, Is.EqualTo(200));
            ledger.TryBeginNextDay(2);
            Assert.That(ledger.TrySpend(purchase, Guid.NewGuid(), 1), Is.False);
            Assert.That(ledger.TrySpend(Guid.NewGuid(), unit, 1), Is.False);
            Assert.That(ledger.TryRecord(order, Guid.NewGuid(), 500, true), Is.False);
            Assert.That(ledger.TryRecord(Guid.NewGuid(), dish, 500, true), Is.False);
            ledger.TrySettleDay(Policy(), 0, out DailySummary second);
            Assert.That(second.DayNumber, Is.EqualTo(2)); Assert.That(second.OpeningBalanceCents, Is.EqualTo(200));
            Assert.That(second.SalesCents, Is.Zero); Assert.That(second.PurchasesCents, Is.Zero);
            Assert.That(second.NetCents, Is.EqualTo(-300)); Assert.That(second.ClosingBalanceCents, Is.EqualTo(-100));
            Assert.That(first.ClosingBalanceCents, Is.EqualTo(200)); Assert.That(ledger.Summaries, Has.Count.EqualTo(2));
        }
        [Test]
        public void DailyEnergyFreezesAtSettlementWhileHistoricalEnergyAndSwitchesSurvive()
        {
            var supply = new ElectricitySupplyState(true); var meter = new ElectricityMeter(2000);
            supply.Advance(3600, new[] { meter }); supply.CompleteDay(); meter.CompleteDay();
            supply.Advance(3600, new[] { meter });
            Assert.That(supply.DailyConsumedKilowattHours, Is.EqualTo(2)); Assert.That(meter.DailyConsumedKilowattHours, Is.EqualTo(2));
            Assert.That(supply.ConsumedKilowattHours, Is.EqualTo(4));
            supply.BeginDay(); meter.BeginDay();
            Assert.That(supply.IsOn, Is.True); Assert.That(supply.DailyConsumedKilowattHours, Is.Zero);
            Assert.That(meter.DailyConsumedKilowattHours, Is.Zero); Assert.That(meter.ConsumedKilowattHours, Is.EqualTo(4));
            supply.Advance(1800, new[] { meter });
            Assert.That(supply.DailyConsumedKilowattHours, Is.EqualTo(1)); Assert.That(supply.ConsumedKilowattHours, Is.EqualTo(5));
        }
        [Test]
        public void FixedCostsAreDataDrivenAndConfigurationAndSummaryAreImmutableCopies()
        {
            var entries = new[] { new DailyFixedCost("rent", "Rent", 300), new DailyFixedCost("fixture", "Test fixture", 20) };
            var policy = new OperatingCostPolicy(0, entries); entries[0] = new DailyFixedCost("rent", "Changed", 999);
            var ledger = new PaymentLedger(0); ledger.TrySettleDay(policy, 0, out DailySummary first); ledger.TryBeginNextDay(2);
            Assert.That(first.FixedCostsCents, Is.EqualTo(320));
            Assert.That(first.Transactions.Single(item => item.CostId == "rent").Label, Is.EqualTo("Rent"));
            Assert.That(first.Transactions, Has.Count.EqualTo(3));
            Assert.Throws<NotSupportedException>(() => ((System.Collections.Generic.IList<LedgerTransaction>)first.Transactions).Clear());
            Assert.Throws<ArgumentException>(() => new OperatingCostPolicy(0, new[] { entries[0], entries[0] }));
        }
        [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(-1)]
        public void InvalidEnergyNeverMutatesLedger(double energy)
        {
            var ledger = new PaymentLedger(1000);
            Assert.Throws<ArgumentOutOfRangeException>(() => ledger.TrySettleDay(Policy(), energy, out _));
            Assert.That(ledger.BalanceCents, Is.EqualTo(1000)); Assert.That(ledger.Transactions, Is.Empty); Assert.That(ledger.IsDaySettled, Is.False);
        }
        [Test]
        public void OverflowNeverPartiallyPostsExpensesOrConsumesTheSettlement()
        {
            var ledger = new PaymentLedger(0);
            Assert.Throws<OverflowException>(() => ledger.TrySettleDay(Policy(), double.MaxValue, out _));
            Assert.That(ledger.BalanceCents, Is.Zero); Assert.That(ledger.Transactions, Is.Empty);
            Assert.That(ledger.IsDaySettled, Is.False); ledger.TrySettleDay(Policy(), 0, out _);
            Assert.That(ledger.BalanceCents, Is.EqualTo(-300));
        }
        [TestCase(-5, "-€0.05")] [TestCase(-105, "-€1.05")] [TestCase(0, "€0.00")]
        [TestCase(long.MinValue, "-€92233720368547758.08")]
        public void NegativeMoneyIsLegibleEvenBelowOneEuro(long cents, string text)
        {
            Assert.That(IngredientPurchaseStation.FormatCents(cents), Is.EqualTo(text));
        }
        [Test]
        public void SummaryTextRepresentsTheSettledTransactions()
        {
            var ledger = new PaymentLedger(0); ledger.TryRecord(Guid.NewGuid(), Guid.NewGuid(), 500, true);
            ledger.TrySettleDay(Policy(), 2.35, out DailySummary summary);
            Assert.That(OperatingCostsFeedback.SummaryText(summary).Replace("\r\n", "\n"), Is.EqualTo(
                "DAY 1 SUMMARY\nOpening balance: €0.00\nSales: €5.00\nPurchases: -€0.00\nElectricity: -€0.71\n" +
                "  2.350000 kWh at 30 cents/kWh\nRent: -€3.00\nNet: €1.29\nBalance: €1.29"));
        }
    }
}
