using UnityEngine;

namespace ZeroStarRestaurant.Interaction
{
    public sealed class InteractionFeedback : MonoBehaviour
    {
        [SerializeField] private PlayerInteraction _interaction;
        [SerializeField] private InteractionInput _input;
        private GUIStyle _style;

        private void OnGUI()
        {
            if (_interaction == null || _input == null || !_interaction.HasControl)
                return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 18 };
                _style.normal.textColor = Color.white;
            }
            GUI.Label(new Rect(Screen.width / 2f - 10f, Screen.height / 2f - 12f, 20f, 24f), "+");
            string prompt = string.Empty;
            if (_interaction.HasHeldObject)
                prompt = _interaction.HeldName + "   " + (_input.AssistedPlacementPrompt ??
                    (_input.IsMouseHolding ? "Release LMB to let go" : "Hold/release LMB or [" + _input.DropBinding + "] Drop"));
            else if (_interaction.FocusedTarget != null)
                prompt = _interaction.FocusedTarget is Pickup ? "Hold LMB to grab " + _interaction.FocusedTarget.DisplayName :
                    "[" + _input.InteractBinding + "] " + _interaction.FocusedTarget.PromptLabel;
            if (prompt.Length > 0)
                GUI.Box(new Rect(Screen.width / 2f - 260f, Screen.height / 2f + 32f, 520f, 42f), prompt, _style);
        }
    }
}
