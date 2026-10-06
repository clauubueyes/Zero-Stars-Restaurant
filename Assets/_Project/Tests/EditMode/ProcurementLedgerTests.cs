using System;
using NUnit.Framework;
using ZeroStarRestaurant.Economy;

namespace ZeroStarRestaurant.Tests
{
    public sealed class ProcurementLedgerTests
    {
        [Test]
        public void ExactFundsCanBeSpentOnceAndSaleIncomeCanBeReinvested()
        {
            var ledger = new PaymentLedger(150);
            Assert.That(ledger.TrySpend(Guid.NewGuid(), Guid.NewGuid(), 35), Is.True);
            Assert.That(ledger.TrySpend(Guid.NewGuid(), Guid.NewGuid(), 80), Is.True);
            Assert.That(ledger.TrySpend(Guid.NewGuid(), Guid.NewGuid(), 35), Is.True);
            Assert.That(ledger.BalanceCents, Is.Zero);
            Guid order = Guid.NewGuid(), dish = Guid.NewGuid();
            Assert.That(ledger.TryRecord(order, dish, 500, true), Is.True);
            Assert.That(ledger.TryRecord(order, dish, 500, true), Is.False);
            Assert.That(ledger.TrySpend(Guid.NewGuid(), Guid.NewGuid(), 175), Is.True);
            Assert.That(ledger.BalanceCents, Is.EqualTo(325));
        }

        [TestCase(0, 35)]
        [TestCase(34, 35)]
        [TestCase(100, 0)]
        [TestCase(100, -35)]
        public void InvalidOrUnaffordableSpendingDoesNotChangeBalanceOrConsumeTransaction(long balance, int price)
        {
            var ledger = new PaymentLedger(balance); Guid purchase = Guid.NewGuid(), unit = Guid.NewGuid();
            Assert.That(ledger.TrySpend(purchase, unit, price), Is.False);
            Assert.That(ledger.BalanceCents, Is.EqualTo(balance));
            Assert.That(ledger.TryRecord(Guid.NewGuid(), Guid.NewGuid(), 500, true), Is.True);
            Assert.That(ledger.TrySpend(purchase, unit, 35), Is.True);
            Assert.That(ledger.BalanceCents, Is.EqualTo(balance + 465));
        }

        [Test]
        public void DuplicatePurchaseOrUnitAndEmptyIdsNeverSpendTwice()
        {
            var ledger = new PaymentLedger(1000); Guid purchase = Guid.NewGuid(), unit = Guid.NewGuid();
            Assert.That(ledger.TrySpend(Guid.Empty, unit, 35), Is.False);
            Assert.That(ledger.TrySpend(purchase, Guid.Empty, 35), Is.False);
            Assert.That(ledger.TrySpend(purchase, unit, 35), Is.True);
            Assert.That(ledger.TrySpend(purchase, Guid.NewGuid(), 35), Is.False);
            Assert.That(ledger.TrySpend(Guid.NewGuid(), unit, 35), Is.False);
            Assert.That(ledger.BalanceCents, Is.EqualTo(965));
        }

        [Test]
        public void LargeIntegerBalanceAndRepeatedSmallPurchasesRemainExact()
        {
            var ledger = new PaymentLedger(long.MaxValue);
            for (int index = 0; index < 100; index++) Assert.That(ledger.TrySpend(Guid.NewGuid(), Guid.NewGuid(), 35), Is.True);
            Assert.That(ledger.BalanceCents, Is.EqualTo(long.MaxValue - 3500));
            Assert.That(ledger.TryRecord(Guid.NewGuid(), Guid.NewGuid(), 3500, true), Is.True);
            Assert.That(ledger.BalanceCents, Is.EqualTo(long.MaxValue));
        }
    }
}
