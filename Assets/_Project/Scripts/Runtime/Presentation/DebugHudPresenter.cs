using System;
using UnityEngine;
using UnityEngine.InputSystem;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;
using ZeroStarRestaurant.Orders;
using ZeroStarRestaurant.Restaurant;
using ZeroStarRestaurant.Utilities;

namespace ZeroStarRestaurant.Presentation
{
    [DisallowMultipleComponent]
    public sealed class DebugHudPresenter : MonoBehaviour
    {
        [SerializeField] private RestaurantDayFeedback _day;
        [SerializeField] private IngredientPurchaseStation _procurement;
        [SerializeField] private ElectricityFeedback _electricity;
        [SerializeField] private OrderFeedback _service;
        [SerializeField] private OperatingCostsFeedback _summary;
        [SerializeField] private RestaurantOperatingCosts _costs;
        [SerializeField] private InteractionFeedback _interaction;
        [SerializeField] private FoodInspectionFeedback _food;
        [SerializeField] private DishInspectionFeedback _dish;
        private readonly Vector2[] _scroll = new Vector2[8];
        private GUIStyle _style;
        private int _purchaseRevision, _billRevision, _deliveryRevision, _reputationRevision;
        private int _summaryDay;
        private string _message = "";
        private double _messageUntil;

        public void Validate()
        {
            if (_day == null || _procurement == null || _electricity == null || _service == null ||
                _summary == null || _costs == null || _interaction == null || _food == null || _dish == null)
                throw new ArgumentException("Debug HUD requires explicit existing feedback providers.");
        }

        private void Awake()
        {
            try { Validate(); _purchaseRevision = _procurement.MessageRevision; _billRevision = _costs.BillMessageRevision; _deliveryRevision = _service.ResultRevision; }
            catch (ArgumentException exception) { Debug.LogError(exception.Message, this); enabled = false; }
        }

        private void Update()
        {
            // Provisional gameplay action, independent of the developer Inspector commands.
            if (Keyboard.current == null || !Keyboard.current.enterKey.wasPressedThisFrame || _day.Day == null) return;
            switch (_day.Day.State?.Stage)
            {
                case RestaurantDayStage.Preparation: _day.Day.OpenRestaurant(); break;
                case RestaurantDayStage.Closed: _day.Day.EndCurrentDay(); break;
                case RestaurantDayStage.EndOfDay: _day.Day.StartNextDay(); break;
            }
        }

        private void ObserveMessages()
        {
            string text = "";
            if (_purchaseRevision != _procurement.MessageRevision)
            { _purchaseRevision = _procurement.MessageRevision; text = _procurement.LastMessage; }
            if (_billRevision != _costs.BillMessageRevision)
            { _billRevision = _costs.BillMessageRevision; text += (text.Length == 0 ? "" : "\n") + _costs.LastBillMessage; }
            if (_deliveryRevision != _service.ResultRevision)
            { _deliveryRevision = _service.ResultRevision; text += (text.Length == 0 ? "" : "\n") + _service.LastDeliveryMessage; }
            if (_service.Reputation != null && _reputationRevision != _service.Reputation.ResolutionRevision)
            { _reputationRevision = _service.Reputation.ResolutionRevision; text += (text.Length == 0 ? "" : "\n") + _service.Reputation.LastResolutionMessage; }
            if (text.Length == 0) return;
            _message = text; _messageUntil = Time.unscaledTimeAsDouble + 6;
        }

        private void Panel(Rect rect, string text, int index)
        {
            if (string.IsNullOrEmpty(text)) return;
            // A fixed content width prevents long receipts from widening the horizontal scroll view.
            GUI.Box(rect, GUIContent.none);
            var viewport = new Rect(rect.x + 8, rect.y + 6, rect.width - 16, rect.height - 12);
            float width = viewport.width - 20; // Reserve the vertical scrollbar before wrapping.
            float height = Mathf.Max(viewport.height, _style.CalcHeight(new GUIContent(text), width));
            _scroll[index] = GUI.BeginScrollView(viewport, _scroll[index], new Rect(0, 0, width, height));
            GUI.Label(new Rect(0, 0, width, height), text, _style);
            GUI.EndScrollView();
        }

        private void OnGUI()
        {
            ObserveMessages();
            if (_style == null) _style = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            DebugHudLayout layout = new DebugHudLayout(Screen.width, Screen.height);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * layout.Scale);
            try
            {
                Panel(layout.Day, _day.Text, 0);
                Panel(layout.Balance, _procurement.BalanceText, 1);
                Panel(layout.Electricity, _electricity.Text, 2);
                Panel(layout.Service, _service.Text, 3);
                string summary = _summary.Text;
                if (!string.IsNullOrEmpty(summary))
                {
                    if (_summaryDay != _day.Day.Calendar.CurrentDay)
                    { _summaryDay = _day.Day.Calendar.CurrentDay; _scroll[4] = Vector2.zero; }
                    Panel(layout.Summary, summary, 4);
                }
                else if (_interaction.HasControl)
                {
                    GUI.Label(layout.Crosshair, "+");
                    Panel(layout.Prompt, _interaction.Text, 5);
                    string dish = _dish.Text;
                    Panel(layout.Context, string.IsNullOrEmpty(dish) ? _food.Text : dish, 6);
                }
                if (Time.unscaledTimeAsDouble < _messageUntil) Panel(layout.Messages, _message, 7);
            }
            finally { GUI.matrix = previous; }
        }
    }
}
