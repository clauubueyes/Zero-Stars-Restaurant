using UnityEngine;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Presentation
{
    // Reads the original unit. Does not initialize, tick or replace FoodState.
    [DisallowMultipleComponent]
    public sealed class FoodStageVisual : MonoBehaviour
    {
        [SerializeField] private FoodItem _food;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Material _raw, _cooked, _burnt;
        private Material _last;
        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();
        public void Refresh()
        {
            if (_food == null || _renderer == null) return;
            var stage = _food.State?.Cooking?.Stage ?? CookingStage.Raw;
            Material selected = stage == CookingStage.Burnt ? _burnt : stage >= CookingStage.Cooked ? _cooked : _raw;
            if (_last == selected && _renderer.sharedMaterial == selected) return;
            _renderer.sharedMaterial = selected; _last = selected;
        }
    }
}
