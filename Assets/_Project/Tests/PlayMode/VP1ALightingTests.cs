#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Hygiene;
using ZeroStarRestaurant.Player;
using ZeroStarRestaurant.Presentation;
using ZeroStarRestaurant.Restaurant;
using ZeroStarRestaurant.Utilities;

namespace ZeroStarRestaurant.Tests
{
    public sealed class VP1ALightingTests
    {
        private Scene _scene, _previous;
        private RestaurantElectricity _supply;
        private RestaurantAtmosphere _atmosphere;
        private PoweredLightFixture[] _fixtures;
        private T[] All<T>() where T : Component => _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        private static void Manual(Component component)
        { var data = new SerializedObject(component); data.FindProperty("_advanceAutomatically").boolValue = false; data.ApplyModifiedPropertiesWithoutUndo(); }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _previous = SceneManager.GetActiveScene();
            _scene = EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/PrototypeRestaurant.unity", new LoadSceneParameters(LoadSceneMode.Additive));
            yield return null; SceneManager.SetActiveScene(_scene);
            All<FirstPersonController>().Single().enabled = false;
            _supply = All<RestaurantElectricity>().Single(); Manual(_supply);
            Manual(All<CustomerServiceLoop>().Single()); Manual(All<RestaurantDayController>().Single());
            All<FoodSimulation>().Single().enabled = false;
            _atmosphere = All<RestaurantAtmosphere>().Single(); _fixtures = All<PoweredLightFixture>();
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        { SceneManager.SetActiveScene(_previous); if (_scene.IsValid()) yield return SceneManager.UnloadSceneAsync(_scene); }

        private void AssertLightAndEmission(bool on)
        {
            var block = new MaterialPropertyBlock();
            foreach (var fixture in _fixtures)
            {
                Assert.That(fixture.Light.enabled, Is.EqualTo(on));
                foreach (var tube in fixture.GetComponentsInChildren<Renderer>().Where(r => r.name == "Tube"))
                {
                    tube.GetPropertyBlock(block);
                    Assert.That(block.GetColor("_EmissionColor").maxColorComponent > 0, Is.EqualTo(on));
                }
            }
        }

        [UnityTest]
        public IEnumerator GeneralSupplyControlsEveryLampAndEmissionAcrossRepeatedCuts()
        {
            Assert.That(_supply.IsOn, Is.False); AssertLightAndEmission(false);
            var emission = _fixtures[0].GetComponentsInChildren<Renderer>().First(r => r.name == "Tube").sharedMaterial;
            string materialBefore = EditorJsonUtility.ToJson(emission);
            for (int cut = 0; cut < 3; cut++)
            {
                _supply.PowerOn(); yield return null; AssertLightAndEmission(true);
                _supply.PowerOff(); yield return null; AssertLightAndEmission(false);
            }
            Assert.That(RenderSettings.ambientMode, Is.EqualTo(AmbientMode.Trilight));
            Assert.That(RenderSettings.ambientSkyColor.maxColorComponent, Is.GreaterThan(0), "Power cuts retain a plausible residual environment.");
            Assert.That(EditorJsonUtility.ToJson(emission), Is.EqualTo(materialBefore));
            Assert.That(_supply.State.ConsumedKilowattHours, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ComparisonKeepsM12SelectionMetersFoodHygieneLedgerAndClock()
        {
            _supply.PowerOn(); _supply.Appliances[1].SetOn(false); _supply.Advance(60);
            var service = All<CustomerServiceLoop>().Single(); var ledger = service.Ledger;
            Assert.That(All<IngredientPurchaseStation>().Single().TryPurchase(0, out var bun), Is.True);
            var state = bun.State; state.Contaminate(); double age = state.AgeSeconds;
            var surface = All<CleanableSurface>().First(); surface.DevelopmentMakeFilthy(); surface.DevelopmentContaminateSurface();
            var dirt = surface.State; var contamination = surface.Contamination;
            var day = All<RestaurantDayController>().Single().State; double clock = day.Clock.SecondsOfDay;
            var selections = _supply.Appliances.Select(a => a.IsOn).ToArray();
            double energy = _supply.State.ConsumedKilowattHours; long money = ledger.BalanceCents;
            for (int repeat = 0; repeat < 3; repeat++)
            {
                _atmosphere.AtmosphereOff(); yield return null;
                Assert.That(_atmosphere.Visuals.activeSelf, Is.False); Assert.That(_atmosphere.Volume.enabled, Is.False);
                Assert.That(_supply.IsOn, Is.True);
                _atmosphere.AtmosphereOn(); yield return null; AssertLightAndEmission(true);
            }
            _atmosphere.AtmosphereOff(); _supply.PowerOff(); _atmosphere.AtmosphereOn(); yield return null; AssertLightAndEmission(false);
            Assert.That(_supply.Appliances.Select(a => a.IsOn), Is.EqualTo(selections));
            Assert.That(_supply.State.ConsumedKilowattHours, Is.EqualTo(energy));
            Assert.That(service.Ledger, Is.SameAs(ledger)); Assert.That(ledger.BalanceCents, Is.EqualTo(money));
            Assert.That(bun.State, Is.SameAs(state)); Assert.That(state.AgeSeconds, Is.EqualTo(age)); Assert.That(state.IsContaminated, Is.True);
            Assert.That(surface.State, Is.SameAs(dirt)); Assert.That(surface.State.Amount, Is.EqualTo(1));
            Assert.That(surface.Contamination, Is.SameAs(contamination)); Assert.That(surface.Contamination.IsContaminated, Is.True);
            Assert.That(day.Clock.SecondsOfDay, Is.EqualTo(clock));
        }

        [UnityTest]
        public IEnumerator DisabledSupplyAndDisabledFixtureCannotEmitElectricLight()
        {
            _supply.PowerOn(); yield return null; AssertLightAndEmission(true);
            var state = _supply.State; _supply.enabled = false; yield return null; AssertLightAndEmission(false);
            _supply.enabled = true; yield return null; Assert.That(_supply.State, Is.SameAs(state)); AssertLightAndEmission(true);
            var fixture = _fixtures[0]; fixture.enabled = false; yield return null;
            Assert.That(fixture.Light.enabled, Is.False); Assert.That(_fixtures.Skip(1).All(f => f.Light.enabled), Is.True);
            fixture.enabled = true; yield return null; AssertLightAndEmission(true);
        }

        [UnityTest]
        public IEnumerator DisablingPresentationRestoresReferenceAndReenableRespectsPowerCut()
        {
            var reference = All<Light>().Single(light => light.type == LightType.Directional);
            Assert.That(reference.enabled, Is.False);
            _atmosphere.enabled = false; yield return null;
            Assert.That(reference.enabled, Is.True); Assert.That(_atmosphere.Volume.enabled, Is.False);
            Assert.That(RenderSettings.ambientMode, Is.EqualTo(AmbientMode.Flat));
            _atmosphere.enabled = true; yield return null;
            Assert.That(reference.enabled, Is.False); Assert.That(RenderSettings.ambientMode, Is.EqualTo(AmbientMode.Trilight));
            AssertLightAndEmission(false); Assert.That(_supply.IsOn, Is.False);
        }
    }
}
#endif
