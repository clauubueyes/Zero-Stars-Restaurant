using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ZeroStarRestaurant.Editor;

namespace ZeroStarRestaurant.Tests
{
    public sealed class FoodVisualPlacementSceneTests
    {
        [Test]
        public void IncrementalMergePreservesAuthoredTransformsRulesAndExistingPresentationTuning()
        {
            string saved = File.ReadAllText(M1GreyboxBuilder.ScenePath);
            string authored = saved.Replace("  _initialAgeSeconds: 0", "  _initialAgeSeconds: 4.25");
            string regenerated = Regex.Replace(saved, @"(?m)^  m_LocalPosition:.*$", "  m_LocalPosition: {x: 999, y: 888, z: 777}");
            regenerated = Regex.Replace(regenerated, @"(?m)^  _heightScale:.*$", "  _heightScale: 0.123");
            Assert.That(FoodVisualPlacementBuilder.Merge(authored, regenerated), Is.EqualTo(authored), "Installed fields and manual edits are authoritative.");
            string old = Regex.Replace(authored, @"(?m)^  _(heightScale|supportingHeightScale|diameterScale):[^\r\n]*\r?\n", "");
            string migrated = FoodVisualPlacementBuilder.Merge(old, regenerated);
            Assert.That(Regex.Replace(migrated, @"(?m)^  _(heightScale|supportingHeightScale|diameterScale):[^\r\n]*\r?\n", ""), Is.EqualTo(old));
            Assert.That(migrated, Does.Contain("  _heightScale: 0.123"));
            Assert.That(migrated, Does.Not.Contain("m_LocalPosition: {x: 999"));
        }
    }
}
