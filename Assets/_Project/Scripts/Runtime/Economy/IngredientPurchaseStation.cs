using System;
using System.Globalization;
using UnityEngine;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Economy
{
    [DisallowMultipleComponent]
    public sealed class IngredientPurchaseStation : MonoBehaviour
    {
        [SerializeField] private CustomerServiceLoop _service;
        [SerializeField] private FoodSimulation _simulation;
        [SerializeField] private Transform _output;
        [SerializeField] private BoxCollider _outputClearance;
        [SerializeField] private IngredientProduct[] _products = Array.Empty<IngredientProduct>();
        private FoodItem[] _prefabs;
        private int[] _prices;
        public string LastMessage { get; private set; } = "Look at a product and press E. Collect it from the output.";
        public int ProductCount => _prefabs == null ? 0 : _prefabs.Length;
        public string ProductName(int index) => ValidIndex(index) ? _prefabs[index].Definition.DisplayName : "Unavailable";
        public int PriceCents(int index) => ValidIndex(index) ? _prices[index] : 0;
        public static string FormatCents(long cents) => "€" + (cents / 100) + "." + (cents % 100).ToString("D2", CultureInfo.InvariantCulture);

        private void Awake()
        {
            if (_service == null || _simulation == null || _output == null || _outputClearance == null || !_outputClearance.isTrigger || _products == null || _products.Length == 0)
            { Debug.LogError("Procurement needs the existing service, food clock, output and products.", this); enabled = false; return; }
            _prefabs = new FoodItem[_products.Length]; _prices = new int[_products.Length];
            try
            {
                for (int index = 0; index < _products.Length; index++)
                {
                    if (_products[index] == null) throw new ArgumentException("Missing ingredient product.");
                    _products[index].Validate();
                    _prefabs[index] = _products[index].Prefab; _prices[index] = _products[index].PriceCents;
                }
            }
            catch (ArgumentException exception)
            { Debug.LogError("Invalid procurement configuration: " + exception.Message, this); enabled = false; }
        }

        private bool ValidIndex(int index) => _prefabs != null && index >= 0 && index < _prefabs.Length && _prefabs[index] != null;
        public bool IsAvailable(int index) => isActiveAndEnabled && ValidIndex(index) && _service != null &&
            _service.isActiveAndEnabled && _service.Ledger != null && _simulation != null && _output != null && _outputClearance != null;

        public bool TryPurchase(int index, out FoodItem purchased)
        {
            purchased = null;
            if (!IsAvailable(index)) { LastMessage = "Procurement unavailable."; return false; }
            PaymentLedger ledger = _service.Ledger;
            if (ledger.BalanceCents < _prices[index])
            { LastMessage = "Insufficient funds for " + ProductName(index) + "."; return false; }
            FoodItem prefab = _prefabs[index];
            BoxCollider box = prefab.GetComponent<BoxCollider>();
            if (prefab.gameObject.activeSelf || box == null)
            { LastMessage = "Invalid product prefab."; return false; }
            Physics.SyncTransforms();
            Vector3 scale = prefab.transform.localScale;
            Vector3 center = _output.position + _output.rotation * Vector3.Scale(box.center, scale);
            Vector3 clearanceScale = _outputClearance.transform.lossyScale;
            clearanceScale = new Vector3(Mathf.Abs(clearanceScale.x), Mathf.Abs(clearanceScale.y), Mathf.Abs(clearanceScale.z));
            if (Physics.CheckBox(center, Vector3.Scale(box.size * 0.5f, scale) + Vector3.one * 0.01f,
                _output.rotation, ~0, QueryTriggerInteraction.Ignore) ||
                Physics.CheckBox(_outputClearance.transform.TransformPoint(_outputClearance.center),
                    Vector3.Scale(_outputClearance.size * 0.5f, clearanceScale), _outputClearance.transform.rotation, ~0, QueryTriggerInteraction.Ignore))
            { LastMessage = "Output occupied. Remove the ingredient before buying again."; return false; }

            // Inactive prefab: prepare and register first; no physical object is exposed before payment.
            FoodItem unit = Instantiate(prefab, _output.position, _output.rotation);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(unit.gameObject, gameObject.scene);
            if (!unit.TryInitialize() || !_simulation.Register(unit))
            { Destroy(unit.gameObject); LastMessage = "Ingredient could not be prepared; no charge."; return false; }
            if (!ledger.TrySpend(Guid.NewGuid(), unit.State.InstanceId, _prices[index]))
            {
                _simulation.Unregister(new[] { unit }); Destroy(unit.gameObject);
                LastMessage = "Purchase failed; no charge."; return false;
            }
            unit.gameObject.SetActive(true);
            purchased = unit;
            LastMessage = "Purchased " + ProductName(index) + " for " + FormatCents(_prices[index]) + ". Collect it from the output.";
            return true;
        }

        private void OnGUI()
        {
            if (_service == null || _service.Ledger == null) return;
            GUI.Box(new Rect(12, Screen.height - 82, 470, 70),
                "Ingredient procurement — Balance: " + FormatCents(_service.Ledger.BalanceCents) + "\n" + LastMessage);
        }
    }
}
