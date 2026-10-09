using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ZeroStarRestaurant.Editor;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Hygiene;
using ZeroStarRestaurant.Presentation;

namespace ZeroStarRestaurant.Tests
{
    public sealed class VisualPolishSceneTests
    {
        [Test]
        public void IncrementalPolishPreservesManualTransformsPhysicsRulesAndLaterArtEdits()
        {
            var scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
            try
            {
                Component[] All() => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Component>(true)).ToArray();
                foreach (var component in All().OfType<BurgerIngredientVisual>().ToArray()) Object.DestroyImmediate(component);
                foreach (var view in All().OfType<DirtSurfaceView>())
                { var data = new SerializedObject(view); data.FindProperty("_organicOverlay").boolValue = false; data.ApplyModifiedPropertiesWithoutUndo(); }
                var procurement = All().OfType<Transform>().Single(t => t.name == "IngredientProcurementStation"); procurement.position += new Vector3(.17f, 0, -.12f);
                var originalShell = All().OfType<FoodItem>().First().transform.Find("Visual_VP1BC"); originalShell.localPosition += Vector3.forward * .006f;
                var poses = All().OfType<Transform>().ToDictionary(t => t, t => EditorJsonUtility.ToJson(t));
                var protectedComponents = All().Where(c => c is Collider || c is Rigidbody || c is Light || c is MonoBehaviour && !(c is DirtSurfaceView)).ToDictionary(c => c, EditorJsonUtility.ToJson);
                VisualPolishBuilder.ConfigureScene(scene);
                foreach (var pair in poses) Assert.That(EditorJsonUtility.ToJson(pair.Key), Is.EqualTo(pair.Value), pair.Key.name);
                foreach (var pair in protectedComponents) Assert.That(EditorJsonUtility.ToJson(pair.Key), Is.EqualTo(pair.Value), pair.Key.name);
                var residue = All().OfType<Renderer>().First(r => r.name.StartsWith("Residue "));
                residue.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(M17HygieneBuilder.MaterialPath);
                residue.transform.localRotation = Quaternion.Euler(0, 17, 0);
                var manual = EditorJsonUtility.ToJson(residue.transform); var count = All().Length;
                VisualPolishBuilder.ConfigureScene(scene); VisualPolishBuilder.ConfigureScene(scene);
                Assert.That(All().Length, Is.EqualTo(count)); Assert.That(EditorJsonUtility.ToJson(residue.transform), Is.EqualTo(manual));
                Assert.That(AssetDatabase.GetAssetPath(residue.sharedMaterial), Is.EqualTo(M17HygieneBuilder.MaterialPath));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
