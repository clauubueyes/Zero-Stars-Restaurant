using UnityEditor;
using UnityEngine;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Hygiene;

namespace ZeroStarRestaurant.Editor
{
    internal static class FoodSafetyDebugView
    {
        internal static void Draw(ContaminationState state)
        {
            EditorGUILayout.Space(); EditorGUILayout.LabelField("Development: food safety (live session)", EditorStyles.boldLabel);
            if (!Application.isPlaying) { EditorGUILayout.HelpBox("Enter Play to inspect session state.", MessageType.Info); return; }
            if (state == null) return;
            EditorGUILayout.LabelField("Contaminated", state.IsContaminated.ToString());
            EditorGUILayout.LabelField("Intensity", state.Intensity.ToString("P1"));
            foreach (var trace in state.Snapshot())
            {
                EditorGUILayout.LabelField(trace.Kind + " / " + trace.Intensity.ToString("P1"), EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Origin", trace.Origin);
                EditorGUILayout.LabelField("Origin ID", trace.OriginId.ToString());
                EditorGUILayout.LabelField("Original food", trace.FoodDefinitionId ?? "None");
                EditorGUILayout.LabelField("Food category", trace.FoodCategory?.ToString() ?? "None");
                EditorGUILayout.LabelField("Original unit ID", trace.FoodUnitId?.ToString() ?? "None");
                EditorGUILayout.LabelField("Last donor", trace.LastSource);
                EditorGUILayout.LabelField("Last donor ID", trace.LastSourceId.ToString());
            }
            if (state.LastReceived != null)
            {
                EditorGUILayout.LabelField("Last received origin", state.LastReceived.Origin + " / " + state.LastReceived.LastSource);
                EditorGUILayout.LabelField("Last received origin ID", state.LastReceived.OriginId.ToString());
                EditorGUILayout.LabelField("Last received donor ID", state.LastReceived.LastSourceId.ToString());
            }
        }
    }

    [CustomEditor(typeof(CleanableSurface))]
    public sealed class CleanableSurfaceEditor : UnityEditor.Editor
    {
        public override bool RequiresConstantRepaint() => Application.isPlaying;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector(); var surface = (CleanableSurface)target;
            FoodSafetyDebugView.Draw(surface.Contamination);
            var contact = surface.GetComponent<SurfaceFoodContact>();
            if (Application.isPlaying && contact != null)
            {
                EditorGUILayout.LabelField("Contact entries", contact.ContactEntryCount.ToString());
                EditorGUILayout.LabelField("Safety transfer events", contact.SafetyTransferCount.ToString());
            }
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Contaminate Surface (development)")) surface.DevelopmentContaminateSurface();
                if (GUILayout.Button("Sanitize Surface (development)")) surface.DevelopmentSanitizeSurface();
            }
        }
    }

    [CustomEditor(typeof(FoodItem))]
    public sealed class FoodItemSafetyEditor : UnityEditor.Editor
    {
        public override bool RequiresConstantRepaint() => Application.isPlaying;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector(); var food = (FoodItem)target;
            FoodSafetyDebugView.Draw(food.State?.Contamination);
        }
    }
}
