using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using ZeroStarRestaurant.Editor;
using ZeroStarRestaurant.Presentation;
using ZeroStarRestaurant.Utilities;

namespace ZeroStarRestaurant.Tests
{
    public sealed class VP1ALightingSceneTests
    {
        [Test]
        public void VisualPassHasBoundedRealtimeLightsExplicitSupplyAndNoPhysics()
        {
            var scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
            try
            {
                var components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
                var atmosphere = components.OfType<RestaurantAtmosphere>().Single(); atmosphere.Validate();
                var fixtures = components.OfType<PoweredLightFixture>().ToArray();
                Assert.That(fixtures, Has.Length.EqualTo(7));
                var supply = components.OfType<RestaurantElectricity>().Single(); supply.Validate();
                Assert.That(supply.Appliances.Count, Is.EqualTo(3), "Presentation must not invent thermal lighting appliances or alter billing.");
                foreach (var fixture in fixtures)
                {
                    fixture.Validate(); Assert.That(fixture.Supply, Is.SameAs(supply));
                    foreach (var tube in fixture.GetComponentsInChildren<Renderer>().Where(r => r.name == "Tube"))
                        Assert.That(tube.sharedMaterial.IsKeywordEnabled("_EMISSION"), Is.True, "URP must retain emission on the visible source.");
                }
                var lights = atmosphere.Visuals.GetComponentsInChildren<Light>(true);
                Assert.That(lights, Has.Length.EqualTo(9));
                Assert.That(lights.All(light => light.type == LightType.Spot && light.lightmapBakeType == LightmapBakeType.Realtime), Is.True);
                Assert.That(lights.Count(light => light.shadows != LightShadows.None), Is.EqualTo(4));
                Assert.That(atmosphere.Visuals.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(atmosphere.Visuals.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
                Assert.That(atmosphere.Volume.sharedProfile.TryGet<Bloom>(out var bloom), Is.True);
                Assert.That(bloom.intensity.value, Is.InRange(0, .2f));
                Assert.That(atmosphere.Volume.sharedProfile.TryGet<ChromaticAberration>(out _), Is.False);
                Assert.That(atmosphere.Volume.sharedProfile.TryGet<FilmGrain>(out _), Is.False);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void RepeatedInstallationPreservesManualGameplayAndLightingEdits()
        {
            var scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
            try
            {
                Component[] All() => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
                var fixture = All().OfType<PoweredLightFixture>().First(); fixture.transform.position += Vector3.right * .37f;
                fixture.Light.intensity = 17.3f;
                var wall = All().OfType<Transform>().Single(t => t.name == "WallNorth"); wall.position += Vector3.forward * .2f;
                var components = All(); var poses = components.OfType<Transform>().ToDictionary(t => t, t => (t.localPosition, t.localRotation, t.localScale));
                var serialized = components.OfType<MonoBehaviour>().ToDictionary(c => c, EditorJsonUtility.ToJson);
                VP1ALightingBuilder.ConfigureScene(scene); VP1ALightingBuilder.ConfigureScene(scene);
                Assert.That(All(), Is.EquivalentTo(components));
                foreach (var pair in poses) Assert.That((pair.Key.localPosition, pair.Key.localRotation, pair.Key.localScale), Is.EqualTo(pair.Value));
                foreach (var pair in serialized) Assert.That(EditorJsonUtility.ToJson(pair.Key), Is.EqualTo(pair.Value));
                Assert.That(fixture.Light.intensity, Is.EqualTo(17.3f));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void AddingMissingPassPreservesExistingTransformsCollidersAndMaterials()
        {
            var scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
            try
            {
                Component[] All() => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
                var atmosphere = All().OfType<RestaurantAtmosphere>().Single(); atmosphere.AtmosphereOff();
                Object.DestroyImmediate(atmosphere.gameObject);
                var poses = All().OfType<Transform>().ToDictionary(t => t, t => (t.localPosition, t.localRotation, t.localScale));
                var colliders = All().OfType<Collider>().ToDictionary(c => c, EditorJsonUtility.ToJson);
                var materials = All().OfType<Renderer>().ToDictionary(r => r, r => r.sharedMaterials);
                VP1ALightingBuilder.ConfigureScene(scene);
                foreach (var pair in poses) Assert.That((pair.Key.localPosition, pair.Key.localRotation, pair.Key.localScale), Is.EqualTo(pair.Value));
                foreach (var pair in colliders) Assert.That(EditorJsonUtility.ToJson(pair.Key), Is.EqualTo(pair.Value));
                foreach (var pair in materials) Assert.That(pair.Key.sharedMaterials, Is.EqualTo(pair.Value));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
