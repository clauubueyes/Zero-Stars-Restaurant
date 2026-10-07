using System;
using System.Globalization;
using UnityEngine;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Dishes;
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
        [SerializeField] private PlateProduct _plateProduct;
        private FoodItem[] _prefabs;
        private int[] _prices;
        private PlateItem _platePrefab;
        private int _platePrice;
        public string LastMessage { get; private set; } = "Look at a product and press E. Collect it from the output.";
        public int ProductCount => _prefabs == null ? 0 : _prefabs.Length + (_platePrefab != null ? 1 : 0);
        public string ProductName(int index) => IsPlateIndex(index) ? "Plate" : ValidIndex(index) ? _prefabs[index].Definition.DisplayName : "Unavailable";
        public int PriceCents(int index) => IsPlateIndex(index) ? _platePrice : ValidIndex(index) ? _prices[index] : 0;
        public static string FormatCents(long cents) => (cents < 0 ? "-€" : "€") +
            (Math.Abs((decimal)cents) / 100m).ToString("0.00", CultureInfo.InvariantCulture);

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
                if (_plateProduct != null)
                { _plateProduct.Validate(); _platePrefab = _plateProduct.Prefab; _platePrice = _plateProduct.PriceCents; }
            }
            catch (ArgumentException exception)
            { Debug.LogError("Invalid procurement configuration: " + exception.Message, this); enabled = false; }
        }

        private bool ValidIndex(int index) => _prefabs != null && index >= 0 && index < _prefabs.Length && _prefabs[index] != null;
        private bool IsPlateIndex(int index) => _platePrefab != null && _prefabs != null && index == _prefabs.Length;
        public bool IsAvailable(int index) => isActiveAndEnabled && (ValidIndex(index) || IsPlateIndex(index)) && _service != null &&
            _service.isActiveAndEnabled && _service.Ledger != null && _simulation != null && _output != null && _outputClearance != null;

        public bool TryPurchase(int index, out FoodItem purchased)
        {
            purchased = null;
            if (!IsAvailable(index) || !ValidIndex(index)) { LastMessage = "Procurement unavailable."; return false; }
            PaymentLedger ledger = _service.Ledger;
            if (ledger.IsDaySettled) { LastMessage = "Day settled. Start Next Day before purchasing."; return false; }
            if (ledger.BalanceCents < _prices[index])
            { LastMessage = "Insufficient funds for " + ProductName(index) + "."; return false; }
            FoodItem prefab = _prefabs[index];
            BoxCollider box = prefab.GetComponent<BoxCollider>();
            if (prefab.gameObject.activeSelf || box == null)
            { LastMessage = "Invalid product prefab."; return false; }
            if (!IsOutputClear(box))
            { LastMessage = "Output occupied. Remove the item before buying again."; return false; }

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
            PurchasedMessage(index);
            return true;
        }

        // Buttons dispatch to the appropriate physical product; the food API stays food-only.
        public bool TryPurchase(int index) => IsPlateIndex(index) ? TryPurchasePlate(out _) : TryPurchase(index, out _);

        public bool TryPurchasePlate(out PlateItem purchased)
        {
            purchased = null;
            int index = _prefabs == null ? -1 : _prefabs.Length;
            if (!IsPlateIndex(index) || !IsAvailable(index)) { LastMessage = "Plate unavailable."; return false; }
            if (_service.Ledger.IsDaySettled) { LastMessage = "Day settled. Start Next Day before purchasing."; return false; }
            if (_service.Ledger.BalanceCents < _platePrice) { LastMessage = "Insufficient funds for Plate."; return false; }
            BoxCollider box = _platePrefab.GetComponent<BoxCollider>();
            if (_platePrefab.gameObject.activeSelf || box == null)
            { LastMessage = "Invalid Plate prefab."; return false; }
            if (!IsOutputClear(box))
            { LastMessage = "Output occupied. Remove the item before buying again."; return false; }
            PlateItem unit = Instantiate(_platePrefab, _output.position, _output.rotation);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(unit.gameObject, gameObject.scene);
            unit.Initialize();
            if (!_service.Ledger.TrySpend(Guid.NewGuid(), unit.InstanceId, _platePrice))
            { Destroy(unit.gameObject); LastMessage = "Purchase failed; no charge."; return false; }
            unit.gameObject.SetActive(true); purchased = unit; PurchasedMessage(index); return true;
        }

        private bool IsOutputClear(BoxCollider box)
        {
            Physics.SyncTransforms();
            Transform prefab = box.transform;
            Vector3 scale = prefab.localScale;
            Vector3 center = _output.position + _output.rotation * Vector3.Scale(box.center, scale);
            Vector3 clearanceScale = _outputClearance.transform.lossyScale;
            clearanceScale = new Vector3(Mathf.Abs(clearanceScale.x), Mathf.Abs(clearanceScale.y), Mathf.Abs(clearanceScale.z));
            return !Physics.CheckBox(center, Vector3.Scale(box.size * 0.5f, scale) + Vector3.one * 0.01f,
                _output.rotation, ~0, QueryTriggerInteraction.Ignore) &&
                !Physics.CheckBox(_outputClearance.transform.TransformPoint(_outputClearance.center),
                    Vector3.Scale(_outputClearance.size * 0.5f, clearanceScale), _outputClearance.transform.rotation, ~0, QueryTriggerInteraction.Ignore);
        }

        private void PurchasedMessage(int index) => LastMessage = "Purchased " + ProductName(index) + " for " +
            FormatCents(PriceCents(index)) + ". Collect it from the output.";

        private void OnGUI()
        {
            if (_service == null || _service.Ledger == null) return;
            GUI.Box(new Rect(12, Screen.height - 82, 470, 70),
                "Procurement — Balance: " + FormatCents(_service.Ledger.BalanceCents) + "\n" + LastMessage);
        }
    }
}
