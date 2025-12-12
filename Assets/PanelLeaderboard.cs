using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class PanelLeaderboard : MonoBehaviour
{
    public SupabaseLeaderboard supabase;
    public GameObject leaderboardPanel;
    public GameObject cuadroUser;
    private List<LeaderboardEntry> datos;

    private Transform Panel;

    void Awake()
    {
        Panel = transform;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    async void Start()
    {
        float offsetY = -12.5f; // separación entre filas
        datos = await supabase.GetScores();   // <- ahora sí funciona
        int rank = 1;          // Aquí ya puedes usar los datos
        foreach (var entry in datos)
        {
            GameObject obj = Instantiate(cuadroUser, Panel);
            obj.SetActive(true);

            // POSICIÓN PERSONALIZADA
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0, offsetY * (rank - 1));

            // Obtener los textos hijos
            TMP_Text[] textos = obj.GetComponentsInChildren<TMP_Text>();

            TMP_Text topText = textos[0];
            TMP_Text userText = textos[1];
            TMP_Text scoreText = textos[2];

            var (valor, unidad) = PasarAUnidades(entry.score);

            // Máximo 2 decimales
            scoreText.text = $"{valor} {unidad}";
            topText.text = "#"+rank.ToString();
            userText.text = entry.name;

            rank++;

            Debug.Log($"{entry.name} → {entry.score}");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Devuelve: (valor formateado, unidad)
    public static (string, string) PasarAUnidades(float puntos)
    {
        float displayScore;
        string unidad;

        if (puntos >= 1000000)
        {
            displayScore = puntos / 1000000f;
            unidad = "M";
        }
        else if (puntos >= 1000)
        {
            displayScore = puntos / 1000f;
            unidad = "k";
        }
        else
        {
            displayScore = puntos;
            unidad = "";
        }

        string displayString = displayScore.ToString("0.##"); // máximo 2 decimales
        return (displayString, unidad);
    }
}
