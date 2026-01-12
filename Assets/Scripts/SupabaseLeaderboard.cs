/*
============================================================
SUPABASE LEADERBOARD PARA UNITY
------------------------------------------------------------
Script hecho por: SentoMarcos

DESCRIPCIÓN:
Este script permite conectar Unity con Supabase usando
la API REST para leer y escribir datos en la tabla:

    leaderboard
    - name  (varchar)
    - score (int8 / bigint)

Soporta:
- Insertar puntuaciones.
- Leer leaderboard ordenado descendente.
- Verificación de conexión.
- Uso 100% async sin bloquear el juego.

------------------------------------------------------------
REQUISITOS:

1. TENER UNA CUENTA DE SUPABASE:
   - Crear proyecto en supabase.com
   - Crear tabla "leaderboard" con columnas:
        name  TEXT
        score BIGINT

2. CONFIGURAR RLS EN SUPABASE:
   Activar Row Level Security
   Crear la siguiente policy:

   ----------------------------------------------------------
   create policy "Public leaderboard"
   on "public"."leaderboard"
   as permissive
   for all
   to public
   using (true)
   with check (true);
   ----------------------------------------------------------

3. UNITY PACKAGE MANAGER:
   ----------------------------------------------------------
   INSTALAR Newtonsoft JSON:
     - Window > Package Manager
     - + > Add package by name
     - com.unity.nuget.newtonsoft-json

   (RECOMENDADO) INSTALAR UniTask:
     - + > Add by git url
     - https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask

4. PROYECT SETTINGS:
   ----------------------------------------------------------
   - Player Settings > Managed Stripping Level: LOW o DISABLED
   - Player Settings > Internet Access: REQUIRED

5. CONFIGURACIÓN DEL SCRIPT:

   1. Crear GameObject vacío en la escena
   2. Ponerle nombre: SupabaseManager
   3. Añadir este script al objeto
   4. Rellenar en el Inspector:
      - SupabaseUrl
      - SupabaseAnonKey

------------------------------------------------------------
USO RÁPIDO:

Click derecho sobre el componente en Unity:

- "Insertar Test"
    Inserta un dato de prueba.

- "Probar Conexión Supabase"
    Verifica si el API responde.

- "Leer Leaderboard"
    Descarga y muestra resultados en la consola.

------------------------------------------------------------
OUTPUT DE EJEMPLO:
    ✅ Supabase conectado
    #1 Player1 → 6500
    #2 Player2 → 3400
    #3 Player3 → 1200

============================================================
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

#region Model

[Serializable]
public class LeaderboardEntry
{
    public string name;
    public long score;

    public LeaderboardEntry() { }

    public LeaderboardEntry(string name, long score)
    {
        this.name = name;
        this.score = score;
    }

    public string Name => name;
    public long Score => score;
}

#endregion

public class SupabaseLeaderboard : MonoBehaviour
{
    [Header("Credenciales")]
    public string SupabaseUrl = "https://emptgkxfyfohysoeuasu.supabase.co";
    public string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImVtcHRna3hmeWZvaHlzb2V1YXN1Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjQ2NzQwNjMsImV4cCI6MjA4MDI1MDA2M30.JJc5e17fq8L_ljsD-p-ejTDRyrvqDPN9sJlGVdyMpFo";
    [Tooltip("Nombre exacto de la tabla REST en Supabase")]
    public string TableName = "leaderboard";

    public static SupabaseLeaderboard Instance;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // LEER SCORES
    public async Task<List<LeaderboardEntry>> GetScores(int limit = 10)
    {
        if (limit < 1)
            limit = 1;

        if (!EnsureCredentials("GetScores"))
            return new List<LeaderboardEntry>();

        var endpoint = BuildRestUrl($"select=*&order=score.desc&limit={limit}");

        using var request = UnityWebRequest.Get(endpoint);
        ApplyAuthHeaders(request);

        await WaitForRequestAsync(request.SendWebRequest());

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Failed to read leaderboard: {request.error}\n{request.downloadHandler.text}");
            return new List<LeaderboardEntry>();
        }

        var entries = ParseLeaderboardList(request.downloadHandler.text);
        LogLeaderboard(entries);
        return entries;
    }

    // INSERTAR SCORE
    public async Task<bool> AddScore(string nombre, long puntos)
    {
        if (!EnsureCredentials("AddScore"))
            return false;

        var endpoint = BuildRestUrl();
        var payload = JsonUtility.ToJson(new LeaderboardEntry(nombre, puntos));
        var body = Encoding.UTF8.GetBytes(payload);

        using var request = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST)
        {
            uploadHandler = new UploadHandlerRaw(body),
            downloadHandler = new DownloadHandlerBuffer()
        };

        ApplyAuthHeaders(request);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Prefer", "return=minimal");

        await WaitForRequestAsync(request.SendWebRequest());

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Failed to insert score: {request.error}\n{request.downloadHandler.text}");
            return false;
        }

        Debug.Log($"✅ Score enviado ({nombre} → {puntos})");
        return true;
    }

    // MÉTODOS RÁPIDOS PARA PROBAR
    [ContextMenu("Insertar Test")]
    public async void AddTestScore()
    {
        await AddScore("Jugador de prueba", UnityEngine.Random.Range(0, 9999));
    }

    [ContextMenu("Probar Conexión Supabase")]
    public async void CheckConnection()
    {
        await CheckConnectionAsync();
    }

    [ContextMenu("Leer Leaderboard")]
    public async void ReadScores()
    {
        var scores = await GetScores();

        foreach (var s in scores)
            Debug.Log($"{s.Name} → {s.Score}");
    }

    public async Task<bool> CheckConnectionAsync()
    {
        if (!EnsureCredentials("CheckConnection"))
            return false;

        var endpoint = BuildRestUrl("select=score&limit=1");

        using var request = UnityWebRequest.Get(endpoint);
        ApplyAuthHeaders(request);

        await WaitForRequestAsync(request.SendWebRequest());

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"❌ Supabase ping falló: {request.error}\n{request.downloadHandler.text}");
            return false;
        }

        Debug.Log("✅ Supabase respondió correctamente (ping)");
        return true;
    }

    bool EnsureCredentials(string caller)
    {
        if (string.IsNullOrWhiteSpace(SupabaseUrl) || string.IsNullOrWhiteSpace(SupabaseAnonKey))
        {
            Debug.LogError($"Supabase credentials missing. Cannot execute {caller}.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(TableName))
        {
            Debug.LogError($"TableName no puede estar vacío.");
            return false;
        }

        return true;
    }

    string BuildRestUrl(string query = null)
    {
        var baseUrl = SupabaseUrl?.TrimEnd('/');
        var endpoint = $"{baseUrl}/rest/v1/{TableName}";
        return string.IsNullOrEmpty(query) ? endpoint : $"{endpoint}?{query}";
    }

    void ApplyAuthHeaders(UnityWebRequest request)
    {
        request.SetRequestHeader("apikey", SupabaseAnonKey);
        request.SetRequestHeader("Authorization", $"Bearer {SupabaseAnonKey}");
        request.SetRequestHeader("Accept", "application/json");
    }

    async Task WaitForRequestAsync(UnityWebRequestAsyncOperation operation)
    {
        while (!operation.isDone)
            await Task.Yield();
    }

    List<LeaderboardEntry> ParseLeaderboardList(string json)
    {
        if (string.IsNullOrEmpty(json))
            return new List<LeaderboardEntry>();

        var wrapped = $"{{\"items\":{json}}}"; // JsonUtility solo acepta objetos/arrays envueltos.

        try
        {
            var parsed = JsonUtility.FromJson<LeaderboardEntryWrapper>(wrapped);
            return parsed?.items != null ? new List<LeaderboardEntry>(parsed.items) : new List<LeaderboardEntry>();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to parse leaderboard JSON: {ex.Message}\n{json}");
            return new List<LeaderboardEntry>();
        }
    }

    [Serializable]
    class LeaderboardEntryWrapper
    {
        public LeaderboardEntry[] items = Array.Empty<LeaderboardEntry>();
    }

    void LogLeaderboard(List<LeaderboardEntry> entries)
    {
        if (entries == null)
        {
            Debug.LogError("Leaderboard entries list is null after parsing.");
            return;
        }

        if (entries.Count == 0)
        {
            Debug.Log("✅ Supabase conectado, pero la tabla está vacía.");
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("✅ Supabase conectado. Resultados de leaderboard:");
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            builder.AppendLine($"#{i + 1} {entry.Name} → {entry.Score}");
        }

        Debug.Log(builder.ToString());
    }
}
