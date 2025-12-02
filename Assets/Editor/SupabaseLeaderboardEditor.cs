using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SupabaseLeaderboard))]
public class SupabaseLeaderboardEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Herramientas Supabase", EditorStyles.boldLabel);

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Entra en Play Mode para usar estos botones.", MessageType.Info);

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            var leaderboard = (SupabaseLeaderboard)target;

            if (GUILayout.Button("Probar conexión Supabase"))
                leaderboard.CheckConnection();

            if (GUILayout.Button("Leer Leaderboard"))
                leaderboard.ReadScores();

            if (GUILayout.Button("Insertar score de prueba"))
                leaderboard.AddTestScore();
        }
    }
}
