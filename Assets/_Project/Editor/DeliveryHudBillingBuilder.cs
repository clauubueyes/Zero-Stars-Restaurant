using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;
using ZeroStarRestaurant.Orders;
using ZeroStarRestaurant.Presentation;
using ZeroStarRestaurant.Restaurant;
using ZeroStarRestaurant.Utilities;

namespace ZeroStarRestaurant.Editor
{
    public static class DeliveryHudBillingBuilder
    {
        [MenuItem("Zero Star Restaurant/Prototype/Install Delivery HUD and Billing Fix")]
        public static void InstallInExistingScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetSceneByPath(M1GreyboxBuilder.ScenePath).IsValid())
                throw new InvalidOperationException("Save edits, stop Play and close PrototypeRestaurant before incremental installation.");
            if (Application.isBatchMode && string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(M1GreyboxBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene); ConfigureScene(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save incremental fix.");
            }
            finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true); }
        }

        public static void ConfigureScene(Scene scene)
        {
            T One<T>() where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();
            M12ElectricityBuilder.ConfigureScene(scene);
            M13OperatingCostsBuilder.ConfigureScene(scene);
            CustomerServiceLoop service = One<CustomerServiceLoop>(); DeliveryZone delivery = One<DeliveryZone>();
            if (delivery.Service != service || (service.DeliveryZone != null && service.DeliveryZone != delivery))
                throw new InvalidOperationException("Delivery references conflict; preserve and inspect current user configuration.");
            Wire(service, "_deliveryZone", delivery);
            Wire(One<ElectricityFeedback>(), "_costs", One<RestaurantOperatingCosts>());
            DebugHudPresenter hud = service.GetComponent<DebugHudPresenter>();
            if (hud == null) hud = service.gameObject.AddComponent<DebugHudPresenter>();
            Wire(hud, "_day", One<RestaurantDayFeedback>()); Wire(hud, "_procurement", One<IngredientPurchaseStation>());
            Wire(hud, "_electricity", One<ElectricityFeedback>()); Wire(hud, "_service", One<OrderFeedback>());
            Wire(hud, "_summary", One<OperatingCostsFeedback>()); Wire(hud, "_costs", One<RestaurantOperatingCosts>());
            Wire(hud, "_interaction", One<InteractionFeedback>()); Wire(hud, "_food", One<FoodInspectionFeedback>());
            Wire(hud, "_dish", One<DishInspectionFeedback>());
            hud.Validate(); EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void Wire(Component component, string field, UnityEngine.Object value)
        {
            var data = new SerializedObject(component); SerializedProperty property = data.FindProperty(field);
            if (property.objectReferenceValue != null && property.objectReferenceValue != value)
                throw new InvalidOperationException("Existing reference conflicts: " + field);
            property.objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
