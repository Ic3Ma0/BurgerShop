using System.Linq;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class ShopBgmTests
    {
        [Test]
        public void LoopIsAQuietFiniteClipWithAudibleEnergy()
        {
            var clip = ShopBgm.CreateLoop();
            try
            {
                Assert.That(clip.length, Is.EqualTo(ShopBgm.LoopSeconds).Within(0.05f));
                Assert.That(clip.channels, Is.EqualTo(1));
                Assert.That(clip.frequency, Is.EqualTo(ShopBgm.SampleRate));
                var samples = new float[clip.samples];
                Assert.That(clip.GetData(samples, 0), Is.True);
                float peak = samples.Max(s => Mathf.Abs(s));
                float energy = samples.Sum(s => s * s) / samples.Length;
                Assert.That(peak, Is.GreaterThan(0.02f).And.LessThan(0.2f));
                Assert.That(energy, Is.GreaterThan(0.00001f));
            }
            finally { Object.DestroyImmediate(clip); }
        }

        [Test]
        public void ShouldPlayFollowsSoundDecorationsPauseAndTimeScale()
        {
            Assert.That(ShopBgm.ShouldPlay(true, true, false, 1f), Is.True);
            Assert.That(ShopBgm.ShouldPlay(false, true, false, 1f), Is.False);
            Assert.That(ShopBgm.ShouldPlay(true, false, false, 1f), Is.False);
            Assert.That(ShopBgm.ShouldPlay(true, true, true, 1f), Is.False);
            Assert.That(ShopBgm.ShouldPlay(true, true, false, 0f), Is.False);
        }

        [Test]
        public void FeedbackDirectorLoopsMusicWithTheExistingSoundToggle()
        {
            int previous = PlayerPrefs.GetInt(FeedbackDirector.SoundPreference, 1);
            var root = new GameObject("BgmHud", typeof(RectTransform), typeof(Canvas));
            var actor = new GameObject("Actor").AddComponent<BurgerInventory>();
            actor.Configure();
            try
            {
                PlayerPrefs.SetInt(FeedbackDirector.SoundPreference, 1);
                var hud = FeedbackDirector.Build(root.transform, actor);
                var sources = hud.GetComponents<AudioSource>();
                var music = sources.Single(s => s.clip != null && s.clip.name == "ShopBgmLoop");
                Assert.That(music.loop, Is.True);
                Assert.That(hud.SoundEnabled, Is.True);
                hud.SetSound(false);
                Assert.That(hud.SoundEnabled, Is.False);
                Assert.That(music.isPlaying, Is.False);
            }
            finally
            {
                PlayerPrefs.SetInt(FeedbackDirector.SoundPreference, previous);
                PlayerPrefs.Save();
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(actor.gameObject);
            }
        }
    }
}
