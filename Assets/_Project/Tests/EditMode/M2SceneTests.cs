using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Editor;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M2SceneTests
    {
        [Test]
        public void PrototypeHasFourDistinctPhysicalPickupsAndWiredInteraction()
        {
            Scene scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
            try
            {
                // M2's fixture remains four boxes; subsequent milestones can add other pickups.
                Pickup[] pickups = scene.GetRootGameObjects().Single(root => root.name == "PhysicalTestObjects")
                    .GetComponentsInChildren<Pickup>();
                Assert.That(pickups, Has.Length.EqualTo(4));
                Assert.That(pickups.Select(p => p.Body.mass).Distinct().Count(), Is.EqualTo(4));
                Assert.That(pickups.Select(p => p.GetComponent<MeshRenderer>().sharedMaterial).Distinct().Count(), Is.EqualTo(4));
                foreach (Pickup pickup in pickups)
                {
                    Assert.That(pickup.Body.isKinematic, Is.False);
                    Assert.That(pickup.Body.useGravity, Is.True);
                    Assert.That(pickup.Body.collisionDetectionMode, Is.EqualTo(CollisionDetectionMode.ContinuousDynamic));
                }
                PlayerInteraction player = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerInteraction>()).Single();
                var data = new SerializedObject(player);
                foreach (string field in new[] { "_detector", "_carry", "_player" })
                    Assert.That(data.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
                var input = new SerializedObject(player.GetComponent<InteractionInput>());
                Assert.That(input.FindProperty("_interaction").objectReferenceValue, Is.EqualTo(player));
                Assert.That(AssetDatabase.GetAssetPath(input.FindProperty("_inputActions").objectReferenceValue),
                    Is.EqualTo("Assets/InputSystem_Actions.inputactions"));
                Assert.That(player.GetComponent<InteractionFeedback>(), Is.Not.Null);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void InteractIsAPressAndDropThrowBindingsExist()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            Assert.That(asset.FindAction("Player/Interact", true).interactions, Is.Null.Or.Empty);
            Assert.That(asset.FindAction("Player/Interact", true).bindings.Any(b => b.path == "<Keyboard>/e"), Is.True);
            Assert.That(asset.FindAction("Player/Drop", true).bindings.Any(b => b.path == "<Keyboard>/g"), Is.True);
            Assert.That(asset.FindAction("Player/Throw", true).bindings.Any(b => b.path == "<Mouse>/rightButton"), Is.True);
        }
    }
}
