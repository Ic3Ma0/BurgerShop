using BurgerShop.Core;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class RuntimeMaterialsTests
    {
        [TestCase(false, "Universal Render Pipeline/Lit")]
        [TestCase(true, "Universal Render Pipeline/Unlit")]
        public void RuntimeGeometryUsesPackagedOpaqueDepthWritingMaterial(bool unlit, string shader)
        {
            Material material = RuntimeMaterials.Create(Color.cyan, unlit);
            try
            {
                Assert.That(material.shader.name, Is.EqualTo(shader));
                Assert.That(material.GetFloat("_Surface"), Is.Zero);
                Assert.That(material.GetFloat("_ZWrite"), Is.EqualTo(1f));
                Assert.That(material.renderQueue, Is.LessThan(2500));
                Assert.That(material.GetColor("_BaseColor"), Is.EqualTo(Color.cyan));
            }
            finally { Object.DestroyImmediate(material); }
        }
    }
}
