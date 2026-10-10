#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using ZeroStarRestaurant.Presentation;

namespace ZeroStarRestaurant.Tests
{
    public sealed partial class M19MultiDayRestaurantTests
    {
        [UnityTest]
        public IEnumerator PhysicalLabelsUseDepthWithoutMutatingSharedFontsAndRestoreComparisonMaterials()
        {
            var art = All<RestaurantArtPass>().Single();
            var labels = art.Shells.SelectMany(s => s.GetComponentsInChildren<TextMesh>(true)).ToArray();
            Assert.That(labels.Length, Is.GreaterThan(5));
            var poses = labels.Select(t => (t.transform.localPosition, t.transform.localRotation, t.transform.localScale, t.text, t.color)).ToArray();
            var copies = labels.Select(t => t.GetComponent<Renderer>().sharedMaterial).ToArray();
            for (int i = 0; i < labels.Length; i++)
            {
                Assert.That(copies[i].shader.name, Is.EqualTo("UI/Default"));
                Assert.That(copies[i].GetInt("unity_GUIZTestMode"), Is.EqualTo((int)CompareFunction.LessEqual));
                Assert.That(copies[i], Is.Not.SameAs(labels[i].font.material));
            }
            art.ArtOff();
            var originals = labels.Select(t => t.GetComponent<Renderer>().sharedMaterial).ToArray();
            var shaders = originals.Select(m => m.shader).ToArray();
            Assert.That(originals.Intersect(copies), Is.Empty);
            art.ArtOn(); art.ArtOff(); art.ArtOn();
            Assert.That(labels.Select(t => t.GetComponent<Renderer>().sharedMaterial), Is.EqualTo(copies));
            foreach (var font in labels.Select(t => t.font).Distinct())
            {
                font.RequestCharactersInTexture("Depth atlas rebuild 12345", 64);
                Assert.That(labels.Where(t => t.font == font).Select(t => t.GetComponent<Renderer>().sharedMaterial).Distinct().Count(), Is.EqualTo(1));
            }
            yield return null;
            for (int i = 0; i < labels.Length; i++)
            {
                Assert.That(copies[i].mainTexture, Is.SameAs(labels[i].font.material.mainTexture));
                Assert.That(originals[i].shader, Is.SameAs(shaders[i]));
            }
            Assert.That(labels.Select(t => (t.transform.localPosition, t.transform.localRotation, t.transform.localScale, t.text, t.color)), Is.EqualTo(poses));
            art.enabled = false;
            Assert.That(labels.Select(t => t.GetComponent<Renderer>().sharedMaterial), Is.EqualTo(originals));
            yield return null;
            Assert.That(copies.All(m => m == null), Is.True, "Owned materials are released when the pass is disabled.");
            art.enabled = true;
            Assert.That(labels.All(t => t.GetComponent<Renderer>().sharedMaterial.shader.name == "UI/Default"), Is.True);
            art.ArtOff();
            Assert.That(labels.Select(t => t.GetComponent<Renderer>().sharedMaterial), Is.EqualTo(originals));
        }
    }
}
#endif
