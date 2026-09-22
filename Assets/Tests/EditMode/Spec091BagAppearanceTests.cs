using System.Linq;
using BurgerShop.Building;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests
{
    public sealed class Spec091BagAppearanceTests
    {
        [TestCase(FacilityKind.BagTable, "bag-table")]
        [TestCase(FacilityKind.BagCounter, "bag-counter")]
        public void CatalogAndRuntimeKeepSameModelThroughRestoredUpgrades(FacilityKind kind, string suffix)
        {
            var owner = new GameObject("AppearanceTest");
            try
            {
                var growth = owner.AddComponent<GrowthUpgrades>();
                var catalog = BagLine.CreateFacility(owner.transform, kind, "photo", null, null, null, null, null, null);
                var runtime = BagLine.CreateFacility(owner.transform, kind, "custom:test", null, null, growth, null, null, null);
                var expected = kind == FacilityKind.BagTable ? catalog.WorkRoot : catalog.CounterRoot;
                var actual = kind == FacilityKind.BagTable ? runtime.WorkRoot : runtime.CounterRoot;
                foreach (int level in new[] { 1, 2, 3, 3 })
                {
                    growth.RestorePurchasedLevel("custom:test:" + suffix, level);
                    Assert.That(actual.GetComponentsInChildren<CounterTierVisual>(true), Is.Empty,
                        "Growth initialization must not replace the catalog model with a generic register.");
                    foreach (string name in kind == FacilityKind.BagTable ? new[] { "Base", "InputDivider" } : new[] { "Base" })
                    {
                        var a = actual.Find(name); var e = expected.Find(name);
                        Assert.That(a.localPosition, Is.EqualTo(e.localPosition));
                        Assert.That(a.localRotation, Is.EqualTo(e.localRotation));
                        Assert.That(a.localScale, Is.EqualTo(e.localScale));
                        Assert.That(a.GetComponent<MeshFilter>().sharedMesh, Is.EqualTo(e.GetComponent<MeshFilter>().sharedMesh));
                        Assert.That(a.GetComponent<Renderer>().sharedMaterial.color, Is.EqualTo(e.GetComponent<Renderer>().sharedMaterial.color));
                    }
                    Assert.That(actual.GetComponentsInChildren<MeshRenderer>().Count(r => r.name == "Trim"), Is.EqualTo(level - 1));
                    Assert.That(actual.Find("InputDivider") != null, Is.EqualTo(kind == FacilityKind.BagTable));
                    Assert.That(kind == FacilityKind.BagTable ? runtime.TableLevel : runtime.CounterLevel, Is.EqualTo(level));
                }
            }
            finally { Object.DestroyImmediate(owner); }
        }
    }
}
