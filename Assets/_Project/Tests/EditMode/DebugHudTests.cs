using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Editor;
using ZeroStarRestaurant.Presentation;

namespace ZeroStarRestaurant.Tests
{
    public sealed class DebugHudTests
    {
        [TestCase(1920, 1080)] [TestCase(1280, 720)] [TestCase(960, 640)]
        [TestCase(640, 480)] [TestCase(320, 200)] [TestCase(1024, 768)] [TestCase(3440, 1440)]
        public void AllSimultaneousPanelsAreDisjointAndInsideTheScreen(float width, float height)
        {
            var layout = new DebugHudLayout(width, height);
            Rect[] persistent = { layout.Day, layout.Balance, layout.Electricity, layout.Service, layout.Messages };
            AssertDisjoint(persistent.Concat(new[] { layout.Crosshair, layout.Prompt, layout.Context }).ToArray(), layout);
            AssertDisjoint(persistent.Concat(new[] { layout.Summary }).ToArray(), layout);
            Assert.That(layout.Width * layout.Scale, Is.EqualTo(width).Within(.01f));
            Assert.That(layout.Height * layout.Scale, Is.EqualTo(height).Within(.01f));
        }

        private static void AssertDisjoint(Rect[] rects, DebugHudLayout layout)
        {
            for (int i = 0; i < rects.Length; i++)
            {
                Assert.That(rects[i].width, Is.GreaterThan(0)); Assert.That(rects[i].height, Is.GreaterThan(0));
                Assert.That(rects[i].xMin, Is.GreaterThanOrEqualTo(0)); Assert.That(rects[i].yMin, Is.GreaterThanOrEqualTo(0));
                Assert.That(rects[i].xMax, Is.LessThanOrEqualTo(layout.Width)); Assert.That(rects[i].yMax, Is.LessThanOrEqualTo(layout.Height));
                for (int j = i + 1; j < rects.Length; j++) Assert.That(rects[i].Overlaps(rects[j]), Is.False, $"Panel {i} overlaps {j}");
            }
        }

        [Test]
        public void AutomaticGreyboxGenerationCannotOverwriteAnExistingScene()
        {
            string before = System.IO.File.ReadAllText(M1GreyboxBuilder.ScenePath);
            Assert.Throws<InvalidOperationException>(M1GreyboxBuilder.GenerateScene);
            Assert.That(System.IO.File.ReadAllText(M1GreyboxBuilder.ScenePath), Is.EqualTo(before));
        }

        [Test]
        public void SceneHasOnePresenterAndProvidersDoNotDrawIndependently()
        {
            var scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
            try
            {
                MonoBehaviour[] behaviours = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
                Assert.That(behaviours.OfType<DebugHudPresenter>().Count(), Is.EqualTo(1));
                Assert.DoesNotThrow(behaviours.OfType<DebugHudPresenter>().Single().Validate);
                Assert.That(behaviours.Where(item => item.GetType().GetMethod("OnGUI", BindingFlags.Instance | BindingFlags.NonPublic) != null),
                    Is.EquivalentTo(behaviours.OfType<DebugHudPresenter>()));
                var service = behaviours.OfType<CustomerServiceLoop>().Single();
                Assert.That(service.DeliveryZone.Service, Is.SameAs(service)); Assert.That(service.DeliveryZone.Support.name, Is.EqualTo("DeliveryPad"));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void IncrementalInstallationPreservesUserObjectsTransformsMaterialsAndReferences()
        {
            var scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
            GameObject added = new GameObject("User added object"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(added, scene);
            try
            {
                added.transform.SetPositionAndRotation(new Vector3(17, 3, 12), Quaternion.Euler(10, 25, 45)); added.transform.localScale = new Vector3(2, 3, 4);
                Component[] originals = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
                var poses = originals.OfType<Transform>().ToDictionary(item => item, item => (item.position, item.rotation, item.localScale));
                var materials = originals.OfType<Renderer>().ToDictionary(item => item, item => item.sharedMaterials);
                var references = originals.OfType<MonoBehaviour>().ToDictionary(item => item, EditorJsonUtility.ToJson);
                DeliveryHudBillingBuilder.ConfigureScene(scene); DeliveryHudBillingBuilder.ConfigureScene(scene);
                Assert.That(scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)), Is.EquivalentTo(originals));
                foreach (var pair in poses) Assert.That((pair.Key.position, pair.Key.rotation, pair.Key.localScale), Is.EqualTo(pair.Value));
                foreach (var pair in materials) Assert.That(pair.Key.sharedMaterials, Is.EqualTo(pair.Value));
                foreach (var pair in references) Assert.That(EditorJsonUtility.ToJson(pair.Key), Is.EqualTo(pair.Value));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
