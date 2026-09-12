using BurgerShop.Core;
using BurgerShop.Economy;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class NumberPunchTests
    {
        [Test]
        public void PunchScalesUpThenReturnsToOneAfterAdvance()
        {
            var punch = new NumberPunch();
            punch.Play();
            Assert.That(punch.IsActive, Is.True);
            punch.Advance(0.08f);
            Assert.That(punch.Scale, Is.GreaterThan(1.05f));
            punch.Advance(0.4f);
            Assert.That(punch.IsActive, Is.False);
            Assert.That(punch.Scale, Is.EqualTo(1f));
        }

        [Test]
        public void CoinAndSpendClipsAreShortSynthesizedDings()
        {
            AudioClip coin = CoinSfx.CreateCoin();
            AudioClip spend = CoinSfx.CreateSpend();
            try
            {
                Assert.That(coin.samples, Is.GreaterThan(0));
                Assert.That(spend.samples, Is.GreaterThan(0));
                Assert.That(coin.length, Is.GreaterThan(0.079f).And.LessThan(0.151f));
                Assert.That(spend.length, Is.GreaterThan(0.079f).And.LessThan(0.151f));
                Assert.That(spend.length, Is.LessThanOrEqualTo(coin.length));
            }
            finally
            {
                Object.DestroyImmediate(coin);
                Object.DestroyImmediate(spend);
            }
        }
    }
}
