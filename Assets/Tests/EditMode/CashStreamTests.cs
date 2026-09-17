using System.Collections.Generic;
using System.Linq;
using BurgerShop.Economy;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public class CashStreamTests
    {
        GameObject root;
        CashFloor cash;
        RestaurantWallet wallet;
        Transform player;
        readonly List<int> arrivals=new List<int>();

        [SetUp] public void SetUp()
        {
            root=new GameObject("CashStreamTest");
            player=new GameObject("Collector").transform;player.SetParent(root.transform);
            wallet=root.AddComponent<RestaurantWallet>();
            wallet.SaleRecorded+=arrivals.Add;arrivals.Clear();
            cash=root.AddComponent<CashFloor>();cash.Configure(wallet,player,Vector3.zero);
        }
        [TearDown] public void TearDown()=>Object.DestroyImmediate(root);

        [TestCase(1)] [TestCase(10)] [TestCase(11)] [TestCase(37)] [TestCase(999)] [TestCase(int.MaxValue)]
        public void ExactAmountArrivesInBoundedPackets(int amount)
        {
            cash.DropAtCounter(amount);
            cash.Advance(.02f);
            Assert.That(wallet.Coins,Is.Zero,"Credit only when a packet arrives");
            Assert.That(cash.GroundValue,Is.EqualTo(amount));
            cash.Advance(2.5f);
            Assert.That(wallet.Coins,Is.EqualTo((long)amount));
            Assert.That(cash.GroundValue,Is.Zero);
            Assert.That(cash.PileCount,Is.Zero);
            Assert.That(arrivals.Count,Is.InRange(1,CashCollectionFeel.MaximumPackets));
            if(amount>10)Assert.That(arrivals.Count,Is.GreaterThan(1));
            cash.Advance(2);Assert.That(wallet.Coins,Is.EqualTo((long)amount));
        }

        float GroundTop()=>cash.GetComponentsInChildren<CashPickup>()
            .Where(p=>!p.IsCollecting).Select(p=>p.transform.position.y+p.Visual.localScale.y*CashPickup.BaseStackHeight)
            .DefaultIfEmpty(0).Max();

        [TestCase(false)] [TestCase(true)]
        public void BigStackShrinksAndStreamsInsteadOfDisappearing(bool manySmallSales)
        {
            int amount=manySmallSales?2000:1000;
            if(manySmallSales)for(int i=0;i<200;i++)cash.DropAtCounter();else cash.DropAtCounter(amount);
            float top=GroundTop();
            cash.Advance(.5f);
            Assert.That(wallet.Coins,Is.GreaterThan(0).And.LessThan(amount));
            Assert.That(GroundTop(),Is.GreaterThan(0).And.LessThan(top-.1f),"Even compressed stacks visibly shrink");
            Assert.That(cash.GroundValue+wallet.Coins,Is.EqualTo(amount));
            var flights=Object.FindObjectsByType<CashPickup>(FindObjectsSortMode.None).Where(p=>p.IsCollecting).ToArray();
            Assert.That(flights.Length,Is.InRange(2,6));
            foreach(var flight in flights)
            {
                Assert.That(flight.Visual.localScale.y,Is.LessThan(1),"Only a thin bundle flies, never the whole tower");
                Assert.That(flight.GetComponentsInChildren<MeshRenderer>().Length,Is.LessThanOrEqualTo(4));
            }
            cash.Advance(2);
            Assert.That(wallet.Coins,Is.EqualTo(amount));
            Assert.That(arrivals.Count,Is.InRange(2,32));
            Assert.That(cash.GroundValue,Is.Zero);
        }

        [Test] public void LeavingStopsNewPacketsButFlightsFinishAndReturnContinues()
        {
            cash.DropAtCounter(157);
            cash.Advance(.24f);
            player.position=Vector3.right*20;
            cash.Advance(.5f);
            long collected=wallet.Coins;int remaining=cash.GroundValue;
            Assert.That(collected,Is.GreaterThan(0).And.LessThan(157));
            Assert.That(remaining+collected,Is.EqualTo(157));
            cash.Advance(1);
            Assert.That(wallet.Coins,Is.EqualTo(collected));Assert.That(cash.GroundValue,Is.EqualTo(remaining));
            player.position=Vector3.zero;
            cash.Advance(0);Assert.That(wallet.Coins,Is.EqualTo(collected));
            cash.Advance(2.5f);Assert.That(wallet.Coins,Is.EqualTo(157));
        }

        [Test] public void OtherDropLocationsAreNotSweptIntoTheStream()
        {
            cash.DropAtCounter(333);cash.DropAt(Vector3.right*10,777);
            cash.Advance(3);
            Assert.That(wallet.Coins,Is.EqualTo(333));Assert.That(cash.GroundValue,Is.EqualTo(777));
        }

        [Test] public void WalletLimitReservesFlightsAndLeavesExactRemainder()
        {
            wallet.RestoreProgress(long.MaxValue-15,0);cash.DropAtCounter(37);
            cash.Advance(2);
            Assert.That(wallet.Coins,Is.EqualTo(long.MaxValue));Assert.That(cash.GroundValue,Is.EqualTo(22));
        }

        [Test] public void WalletFilledDuringFlightReturnsTheUncreditedMoney()
        {
            cash.DropAtCounter(10);cash.Advance(.02f);
            wallet.RestoreProgress(long.MaxValue,0);cash.Advance(1);
            Assert.That(cash.GroundValue,Is.EqualTo(10));Assert.That(wallet.Coins,Is.EqualTo(long.MaxValue));
            wallet.RestoreProgress(long.MaxValue-10,0);cash.Advance(1);
            Assert.That(cash.GroundValue,Is.Zero);Assert.That(wallet.Coins,Is.EqualTo(long.MaxValue));
        }

        [Test] public void StreamAudioAcceptsEachPulseRejectsDuplicatesAndCapsPitch()
        {
            var budget=new FeedbackBudget();var sequence=new CashSoundSequence();float first=0,last=0;
            for(int i=0;i<50;i++)
            {
                float time=i*CashCollectionFeel.Interval;
                Assert.That(budget.Accept(FeedbackSound.Cash,time),Is.True);
                Assert.That(budget.Accept(FeedbackSound.Cash,time),Is.False);
                last=sequence.Next(time);if(i==0)first=last;
                Assert.That(last,Is.InRange(1f,CashCollectionFeel.MaximumPitch));
            }
            Assert.That(last,Is.GreaterThan(first));
            Assert.That(sequence.Next(10),Is.EqualTo(first));
            var clip=CoinSfx.CreateCoin();
            try
            {
                var pcm=new float[clip.samples];Assert.That(clip.GetData(pcm,0),Is.True);
                Assert.That(pcm.Max(x=>Mathf.Abs(x)),Is.InRange(.05f,1f));
                Assert.That(clip.length,Is.InRange(.08f,.15f));
            }
            finally{Object.DestroyImmediate(clip);}
        }
    }
}
