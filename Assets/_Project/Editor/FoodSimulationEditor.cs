using UnityEditor;
using UnityEngine;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Editor
{
    [CustomEditor(typeof(FoodSimulation))]
    public sealed class FoodSimulationEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Development controls affect only food simulation. No cooking or refrigeration is implemented.", MessageType.Info);
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
                if (GUILayout.Button("Advance food by 60 simulated seconds"))
                    ((FoodSimulation)target).Advance(60.0);
        }
    }
}
