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
            EditorGUILayout.HelpBox("Development only: advances freshness, temperature and cooking at current placement. " +
                "Player/physics time is unchanged. Grill temperature/power can be edited in its Inspector.", MessageType.Info);
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                foreach (double seconds in new[] { 1.0, 10.0, 60.0 })
                    if (GUILayout.Button("Advance food by " + seconds + " simulated seconds"))
                        ((FoodSimulation)target).Advance(seconds);
            }
        }
    }
}
