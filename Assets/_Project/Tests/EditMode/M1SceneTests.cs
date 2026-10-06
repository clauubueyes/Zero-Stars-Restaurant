using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Editor;
using ZeroStarRestaurant.Player;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M1SceneTests
    {
        private Scene _scene;

        [SetUp]
        public void SetUp()
        {
            // Preview scenes avoid touching the user's open or unsaved scenes.
            _scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid())
                EditorSceneManager.ClosePreviewScene(_scene);
        }

        private T[] Components<T>() where T : Component
        {
            return _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        }

        [Test]
        public void PlayerHasWiredCameraAndExistingInputAsset()
        {
            Assert.That(Components<FirstPersonController>(), Has.Length.EqualTo(1));
            Assert.That(Components<FirstPersonMotor>(), Has.Length.EqualTo(1));
            Assert.That(Components<CharacterController>(), Has.Length.EqualTo(1));
            Assert.That(Components<Camera>(), Has.Length.EqualTo(1));
            Assert.That(Components<AudioListener>(), Has.Length.EqualTo(1));
            var controller = new SerializedObject(Components<FirstPersonController>()[0]);
            Transform view = (Transform)controller.FindProperty("_viewTransform").objectReferenceValue;
            Assert.That(view, Is.EqualTo(Components<Camera>()[0].transform));
            Assert.That(view.IsChildOf(controller.targetObject is Component component ? component.transform : null), Is.True);
            Assert.That(AssetDatabase.GetAssetPath(controller.FindProperty("_inputActions").objectReferenceValue),
                Is.EqualTo("Assets/InputSystem_Actions.inputactions"));
        }

        [Test]
        public void SpawnIsClearOfBlockingGeometryAndFloorIsBelowFeet()
        {
            CharacterController player = Components<CharacterController>()[0];
            BoxCollider[] blockers = Components<BoxCollider>();
            Assert.That(blockers.Length, Is.GreaterThanOrEqualTo(5));
            foreach (BoxCollider blocker in blockers)
            {
                Assert.That(blocker.isTrigger, Is.False, blocker.name);
                bool overlapping = Physics.ComputePenetration(player, player.transform.position, player.transform.rotation,
                    blocker, blocker.transform.position, blocker.transform.rotation, out _, out float depth);
                Assert.That(overlapping && depth > player.skinWidth, Is.False, "Spawn overlaps " + blocker.name);
            }
            BoxCollider floor = blockers.Single(box => box.name == "Floor");
            Assert.That(floor.bounds.max.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(player.transform.position.y, Is.GreaterThanOrEqualTo(floor.bounds.max.y));
            Assert.That(floor.bounds.Contains(new Vector3(player.transform.position.x, -0.1f, player.transform.position.z)), Is.True);
        }

        [Test]
        public void GreyboxUsesOnlyBuiltInCubeMeshesAndSimpleUrpMaterials()
        {
            foreach (MeshFilter mesh in Components<MeshFilter>())
            {
                Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh.sharedMesh, out string guid, out long fileId), Is.True);
                Assert.That(guid, Is.EqualTo("0000000000000000e000000000000000"), mesh.name);
                Assert.That(fileId, Is.EqualTo(10202), mesh.name);
                Material material = mesh.GetComponent<MeshRenderer>().sharedMaterial;
                Assert.That(material, Is.Not.Null, mesh.name);
                Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
                Assert.That(material.GetTexture("_BaseMap"), Is.Null, mesh.name);
            }
        }

        [Test]
        public void InputAssetIncludesWasdMouseAndSpace()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            string[] paths = asset.FindAction("Player/Move", true).bindings.Select(binding => binding.path).ToArray();
            foreach (string key in new[] { "<Keyboard>/w", "<Keyboard>/a", "<Keyboard>/s", "<Keyboard>/d" })
                Assert.That(paths, Does.Contain(key));
            Assert.That(asset.FindAction("Player/Look", true).bindings.Any(binding => binding.path == "<Pointer>/delta"), Is.True);
            Assert.That(asset.FindAction("Player/Jump", true).bindings.Any(binding => binding.path == "<Keyboard>/space"), Is.True);
        }
    }
}
