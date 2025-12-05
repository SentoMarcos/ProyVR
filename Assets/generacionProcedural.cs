using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProceduralGenerator : MonoBehaviour
{
    [Header("Configuración de Generación")]
    public float spawnDistance = 100f;
    public float destroyDistance = 50f;
    public int initialSegments = 10;
    public int maxActiveSegments = 50;

    [Header("Prefabs")]
    public GameObject[] edificios;
    public GameObject[] aceras;
    public GameObject[] carreteras;
    public GameObject[] carreterasSeguras;

    [Header("Posiciones de Carril")]
    public float[] carrilPositions = {
        -24f, -12f, -8f, -4f, 0f, 4f, 8f, 12f, 24f
    };

    [Header("Sistema de Puntuación")]
    public Text scoreText;
    public Text levelText;
    public int pointsPerSegment = 100;
    public float scoreUpdateInterval = 0.1f;

    [Header("Sistema de Niveles")]
    public int pointsPerLevel = 1000;
    public int maxLevel = 10;

    [Header("Configuración Dificultad")]
    public AnimationCurve difficultyCurve;
    public float speedMultiplierPerLevel = 1.1f;

    [System.Serializable]
    public class LevelConfig
    {
        public int level;
        [Range(0, 3)] public int minGoodRoads = 1;
        [Range(0, 3)] public int maxGoodRoads = 3;
        [Range(0f, 1f)] public float badRoadChance = 0f;
    }

    [Header("Configuración por Nivel")]
    public LevelConfig[] levelConfigs = {
        new LevelConfig { level = 1, minGoodRoads = 3, maxGoodRoads = 3, badRoadChance = 0.0f },
        new LevelConfig { level = 2, minGoodRoads = 2, maxGoodRoads = 3, badRoadChance = 0.1f },
        new LevelConfig { level = 3, minGoodRoads = 2, maxGoodRoads = 3, badRoadChance = 0.2f },
        new LevelConfig { level = 4, minGoodRoads = 2, maxGoodRoads = 3, badRoadChance = 0.3f },
        new LevelConfig { level = 5, minGoodRoads = 2, maxGoodRoads = 3, badRoadChance = 0.4f },
        new LevelConfig { level = 6, minGoodRoads = 1, maxGoodRoads = 2, badRoadChance = 0.6f },
        new LevelConfig { level = 7, minGoodRoads = 1, maxGoodRoads = 2, badRoadChance = 0.7f },
        new LevelConfig { level = 8, minGoodRoads = 1, maxGoodRoads = 2, badRoadChance = 0.8f },
        new LevelConfig { level = 9, minGoodRoads = 1, maxGoodRoads = 1, badRoadChance = 0.9f },
        new LevelConfig { level = 10, minGoodRoads = 1, maxGoodRoads = 1, badRoadChance = 1.0f }
    };

    private Transform player;
    private List<GameObject> segments = new List<GameObject>();
    private float segmentLength = 20f;

    // Pooling
    private Queue<GameObject> segmentPool = new Queue<GameObject>();
    private const int POOL_SIZE = 60;

    // Variables de juego
    private int currentScore = 0;
    private int highestSegmentReached = 0;
    private float lastScoreUpdateTime = 0f;
    private int currentLevel = 1;
    private int nextLevelThreshold = 1000;
    private float currentSpeedMultiplier = 1f;

    // Debug
    private string debugInfo = "";
    private int totalSegmentsGenerated = 0;
    private int goodRoadsCount = 0;
    private int badRoadsCount = 0;
    private int lastDebugSegmentCount = 0;

    void Start()
    {
        player = Camera.main?.transform;

        if (player == null)
        {
            Debug.LogError("No se encontró Main Camera!");
            GameObject tempCam = new GameObject("TempCamera");
            tempCam.AddComponent<Camera>();
            tempCam.tag = "MainCamera";
            player = tempCam.transform;
        }

        InitializeUI();
        InitializeSegmentPool();

        Debug.Log($"=== GENERADOR INICIALIZADO ===");
        Debug.Log($"Segmentos iniciales: {initialSegments}");

        // Generar segmentos iniciales
        for (int i = 0; i < initialSegments; i++)
        {
            GenerateSegment(i);
        }
    }

    void InitializeSegmentPool()
    {
        for (int i = 0; i < POOL_SIZE; i++)
        {
            GameObject segment = new GameObject($"PoolSegment_{i}");
            segment.SetActive(false);
            segmentPool.Enqueue(segment);
        }
    }

    void Update()
    {
        if (player == null) return;

        float playerZ = player.position.z;
        int currentPlayerSegment = Mathf.FloorToInt(playerZ / segmentLength);

        UpdateScore(currentPlayerSegment);
        CheckLevelUp();

        // Control de generación con límites
        if (segments.Count < maxActiveSegments)
        {
            int segmentsAhead = Mathf.FloorToInt(spawnDistance / segmentLength);
            segmentsAhead = Mathf.Min(segmentsAhead, 10);

            int maxActiveSegment = currentPlayerSegment + segmentsAhead;

            for (int segmentIndex = currentPlayerSegment; segmentIndex <= maxActiveSegment; segmentIndex++)
            {
                if (segmentIndex < 0) continue;

                if (!IsSegmentActive(segmentIndex))
                {
                    GenerateSegment(segmentIndex);

                    if (segments.Count >= maxActiveSegments)
                        break;
                }
            }
        }

        // Limpieza
        CleanupSegments(playerZ);

        // Debug periódico
        if (Time.frameCount % 30 == 0 && segments.Count != lastDebugSegmentCount)
        {
            UpdateDebugInfo();
            lastDebugSegmentCount = segments.Count;
        }
    }

    void UpdateDebugInfo()
    {
        LevelConfig config = GetCurrentLevelConfig();

        debugInfo = $"=== DEBUG INFO ===\n";
        debugInfo += $"Segmentos activos: {segments.Count}/{maxActiveSegments}\n";
        debugInfo += $"Nivel: {currentLevel} ({config.minGoodRoads}-{config.maxGoodRoads} buenas)\n";
        debugInfo += $"Prob. mala: {config.badRoadChance:P0}\n";
        debugInfo += $"Puntuación: {currentScore} / {nextLevelThreshold}\n";
        debugInfo += $"Player Z: {player?.position.z:F1}\n";
        debugInfo += $"Multiplicador: {currentSpeedMultiplier:F2}x";

        Debug.Log(debugInfo);
    }

    bool IsSegmentActive(int segmentIndex)
    {
        foreach (var seg in segments)
        {
            if (seg == null) continue;

            float segZ = seg.transform.position.z;
            int segIndex = Mathf.FloorToInt(segZ / segmentLength);
            if (segIndex == segmentIndex)
                return true;
        }
        return false;
    }

    void CleanupSegments(float playerZ)
    {
        for (int i = segments.Count - 1; i >= 0; i--)
        {
            if (segments[i] == null)
            {
                segments.RemoveAt(i);
                continue;
            }

            float segmentZ = segments[i].transform.position.z;
            if (segmentZ < playerZ - destroyDistance)
            {
                ReturnSegmentToPool(segments[i]);
                segments.RemoveAt(i);
            }
        }
    }

    void ReturnSegmentToPool(GameObject segment)
    {
        if (segment == null) return;

        segment.SetActive(false);

        // Limpiar hijos
        Transform[] children = new Transform[segment.transform.childCount];
        for (int i = 0; i < segment.transform.childCount; i++)
            children[i] = segment.transform.GetChild(i);

        foreach (Transform child in children)
            Destroy(child.gameObject);

        segmentPool.Enqueue(segment);
    }

    GameObject GetSegmentFromPool()
    {
        if (segmentPool.Count > 0)
            return segmentPool.Dequeue();

        Debug.LogWarning("Pool vacío, creando nuevo segmento");
        return new GameObject("DynamicSegment");
    }

    void GenerateSegment(int segmentIndex)
    {
        if (segmentIndex < 0) return;

        GameObject segment = GetSegmentFromPool();
        segment.name = $"Segment_{segmentIndex}";
        segment.transform.position = new Vector3(0, 0, segmentIndex * segmentLength);
        segment.SetActive(true);

        // Limpiar hijos previos
        foreach (Transform child in segment.transform)
            Destroy(child.gameObject);

        // Generar contenido usando tus métodos originales
        GenerateEdificio(segment.transform, 0, edificios);
        GenerateAcera(segment.transform, 1, aceras);
        GenerateRoads(segment.transform, segmentIndex);
        GenerateAcera(segment.transform, 7, aceras);
        GenerateEdificio(segment.transform, 8, edificios);

        AddCollidersToSegment(segment);

        segments.Add(segment);
        totalSegmentsGenerated++;
    }

    void GenerateRoads(Transform parent, int segmentIndex)
    {
        LevelConfig config = GetCurrentLevelConfig();

        int minGoodRoads = Mathf.Clamp(config.minGoodRoads, 1, 3);
        int maxGoodRoads = Mathf.Clamp(config.maxGoodRoads, minGoodRoads, 3);
        int targetGoodRoads = Random.Range(minGoodRoads, maxGoodRoads + 1);

        List<GameObject> roadsToPlace = new List<GameObject>();

        // Añadir carreteras buenas
        for (int i = 0; i < targetGoodRoads; i++)
        {
            GameObject safeRoad = GetRandomSafeRoad();
            if (safeRoad != null)
            {
                roadsToPlace.Add(safeRoad);
                goodRoadsCount++;
            }
        }

        // Llenar espacios restantes
        int remainingSlots = 3 - targetGoodRoads;
        for (int i = 0; i < remainingSlots; i++)
        {
            if (Random.value <= config.badRoadChance && carreteras.Length > 0)
            {
                GameObject badRoad = GetRandomBadRoad();
                if (badRoad != null)
                {
                    roadsToPlace.Add(badRoad);
                    badRoadsCount++;
                }
                else
                {
                    GameObject safeRoad = GetRandomSafeRoad();
                    if (safeRoad != null)
                    {
                        roadsToPlace.Add(safeRoad);
                        goodRoadsCount++;
                    }
                }
            }
            else
            {
                GameObject safeRoad = GetRandomSafeRoad();
                if (safeRoad != null)
                {
                    roadsToPlace.Add(safeRoad);
                    goodRoadsCount++;
                }
            }
        }

        // Mezclar
        ShuffleList(roadsToPlace);

        // Instanciar
        for (int i = 0; i < 3; i++)
        {
            if (i < roadsToPlace.Count && roadsToPlace[i] != null)
            {
                Vector3 position = new Vector3(carrilPositions[3 + i], 0, parent.position.z);
                GameObject road = Instantiate(roadsToPlace[i], position, Quaternion.identity, parent);

                if (IsBadRoad(roadsToPlace[i]))
                {
                    road.name = $"{road.name}_PELIGROSA";
                }
            }
        }
    }

    GameObject GetRandomSafeRoad()
    {
        if (carreterasSeguras == null || carreterasSeguras.Length == 0)
        {
            Debug.LogError("No hay carreteras seguras!");
            return null;
        }
        return carreterasSeguras[Random.Range(0, carreterasSeguras.Length)];
    }

    GameObject GetRandomBadRoad()
    {
        if (carreteras == null || carreteras.Length == 0)
        {
            Debug.LogWarning("No hay carreteras peligrosas");
            return GetRandomSafeRoad();
        }
        return carreteras[Random.Range(0, carreteras.Length)];
    }

    bool IsBadRoad(GameObject roadPrefab)
    {
        if (carreteras == null || roadPrefab == null) return false;
        foreach (var badRoad in carreteras)
        {
            if (badRoad != null && roadPrefab.name.Contains(badRoad.name))
                return true;
        }
        return false;
    }

    LevelConfig GetCurrentLevelConfig()
    {
        foreach (var config in levelConfigs)
        {
            if (config.level == currentLevel)
                return config;
        }

        float progress = Mathf.Clamp01((float)(currentLevel - 1) / (maxLevel - 1));
        return new LevelConfig
        {
            level = currentLevel,
            minGoodRoads = Mathf.Max(1, 3 - Mathf.FloorToInt(progress * 2)),
            maxGoodRoads = 3 - Mathf.FloorToInt(progress * 1),
            badRoadChance = progress
        };
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    void UpdateScore(int currentSegment)
    {
        if (Time.time - lastScoreUpdateTime < scoreUpdateInterval)
            return;

        lastScoreUpdateTime = Time.time;

        if (currentSegment > highestSegmentReached)
        {
            int segmentsGained = currentSegment - highestSegmentReached;
            currentScore += segmentsGained * pointsPerSegment;
            highestSegmentReached = currentSegment;
            UpdateScoreDisplay();
        }
    }

    void CheckLevelUp()
    {
        if (currentScore >= nextLevelThreshold && currentLevel < maxLevel)
        {
            currentLevel++;
            nextLevelThreshold = currentLevel * pointsPerLevel;

            float progress = Mathf.Clamp01((float)(currentLevel - 1) / (maxLevel - 1));
            currentSpeedMultiplier = 1f + (difficultyCurve.Evaluate(progress) * (speedMultiplierPerLevel - 1f));

            UpdateLevelDisplay();

            Debug.Log($"¡NIVEL {currentLevel} ALCANZADO!");
        }
    }

    void InitializeUI()
    {
        if (scoreText == null)
            scoreText = GameObject.Find("ScoreText")?.GetComponent<Text>();

        if (levelText == null)
        {
            levelText = GameObject.Find("LevelText")?.GetComponent<Text>();
            if (levelText == null && scoreText != null)
            {
                GameObject levelObj = new GameObject("LevelText");
                levelObj.transform.SetParent(scoreText.transform.parent);
                levelText = levelObj.AddComponent<Text>();
                levelText.font = scoreText.font;
                levelText.fontSize = 24;
                levelText.alignment = TextAnchor.UpperLeft;
                levelText.color = Color.yellow;

                RectTransform rect = levelText.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(20, -80);
                rect.sizeDelta = new Vector2(300, 40);

                Shadow shadow = levelObj.AddComponent<Shadow>();
                shadow.effectColor = Color.black;
                shadow.effectDistance = new Vector2(2, -2);
            }
        }

        UpdateScoreDisplay();
    }

    void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Puntuación: {currentScore:N0}";
            float progress = (float)currentScore / nextLevelThreshold;
            scoreText.color = Color.Lerp(Color.white, Color.yellow, progress);
        }
    }

    void UpdateLevelDisplay()
    {
        if (levelText != null)
        {
            levelText.text = $"Nivel: {currentLevel}\nSiguiente: {nextLevelThreshold:N0} pts";
            levelText.color = currentLevel >= 5 ? Color.red :
                             currentLevel >= 3 ? Color.yellow : Color.green;
        }
    }

    void AddCollidersToSegment(GameObject segment)
    {
        foreach (Transform child in segment.transform)
        {
            AddColliderBasedOnPosition(child.gameObject);
        }
    }

    void AddColliderBasedOnPosition(GameObject obj)
    {
        float posX = obj.transform.position.x;

        if (Mathf.Abs(posX - (-24f)) < 0.1f || Mathf.Abs(posX - 24f) < 0.1f)
        {
            AddBuildingCollider(obj);
        }
        else if (Mathf.Abs(posX - (-12f)) < 0.1f || Mathf.Abs(posX - 12f) < 0.1f)
        {
            AddSidewalkCollider(obj);
        }
        else if (Mathf.Abs(posX - (-4f)) < 0.1f ||
                 Mathf.Abs(posX - 0f) < 0.1f ||
                 Mathf.Abs(posX - 4f) < 0.1f)
        {
            AddRoadCollider(obj);
        }
    }

    void AddBuildingCollider(GameObject building)
    {
        if (building.GetComponent<Collider>() == null)
        {
            BoxCollider collider = building.AddComponent<BoxCollider>();
            collider.size = new Vector3(20f, 10f, 20f);
            collider.center = new Vector3(0, 5f, 0);
        }
    }

    void AddSidewalkCollider(GameObject sidewalk)
    {
        if (sidewalk.GetComponent<Collider>() == null)
        {
            BoxCollider collider = sidewalk.AddComponent<BoxCollider>();
            collider.size = new Vector3(4f, 0.3f, 20f);
            collider.center = new Vector3(0, 0.15f, 0);
        }
    }

    void AddRoadCollider(GameObject road)
    {
        if (road.GetComponent<Collider>() == null)
        {
            BoxCollider collider = road.AddComponent<BoxCollider>();
            collider.size = new Vector3(4f, 0.1f, 20f);
            collider.center = new Vector3(0, 0.05f, 0);
            collider.isTrigger = true;
        }
    }

    void GenerateEdificio(Transform parent, int laneIndex, GameObject[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0) return;

        GameObject selectedPrefab = prefabs[Random.Range(0, prefabs.Length)];
        if (selectedPrefab == null) return;

        Vector3 position = new Vector3(carrilPositions[laneIndex], 0, parent.position.z);
        Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 4) * 90f, 0);
        Instantiate(selectedPrefab, position, rotation, parent);
    }

    void GenerateAcera(Transform parent, int laneIndex, GameObject[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0) return;

        GameObject selectedPrefab = prefabs[Random.Range(0, prefabs.Length)];
        if (selectedPrefab != null)
        {
            Vector3 position = new Vector3(carrilPositions[laneIndex], 0, parent.position.z);
            Instantiate(selectedPrefab, position, Quaternion.identity, parent);
        }
    }

    // ========== MÉTODOS PÚBLICOS QUE NECESITA TestingController ==========

    public void SetSpawnDistance(float distance)
    {
        spawnDistance = Mathf.Clamp(distance, 50f, 500f);
        Debug.Log($"Spawn Distance actualizado: {spawnDistance}");
    }

    public void SetDestroyDistance(float distance)
    {
        destroyDistance = Mathf.Clamp(distance, 30f, 250f);
        Debug.Log($"Destroy Distance actualizado: {destroyDistance}");
    }

    public float GetCurrentSpeedMultiplier()
    {
        return currentSpeedMultiplier;
    }

    public void AddPoints(int points)
    {
        currentScore += points;
        UpdateScoreDisplay();
        CheckLevelUp();
    }

    public int GetCurrentScore() => currentScore;

    public int GetCurrentLevel() => currentLevel;

    public float GetCurrentDifficulty() => (float)(currentLevel - 1) / (maxLevel - 1);

    public int GetNextLevelPoints() => nextLevelThreshold - currentScore;

    public void ResetScore()
    {
        currentScore = 0;
        highestSegmentReached = 0;
        currentLevel = 1;
        nextLevelThreshold = pointsPerLevel;
        currentSpeedMultiplier = 1f;
        goodRoadsCount = 0;
        badRoadsCount = 0;
        UpdateScoreDisplay();
        UpdateLevelDisplay();
        Debug.Log("Puntuación reseteada");
    }

    public float GetDistanceTraveled() => highestSegmentReached * segmentLength;

    public int GetActiveSegmentCount() => segments.Count;

    public float GetSegmentLength() => segmentLength;

    public List<GameObject> GetActiveSegments() => new List<GameObject>(segments);

    // ========== DEBUG Y GIZMOS ==========

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        for (int i = 0; i < carrilPositions.Length; i++)
        {
            Vector3 pos = new Vector3(carrilPositions[i], 0, 0);
            Gizmos.DrawWireCube(pos, new Vector3(4f, 1f, 1f));
        }
    }

    void OnGUI()
    {
        if (!string.IsNullOrEmpty(debugInfo))
        {
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 12;
            style.normal.textColor = Color.green;
            style.alignment = TextAnchor.UpperLeft;
            style.padding = new RectOffset(10, 10, 10, 10);

            GUI.Box(new Rect(10, 100, 400, 250), debugInfo, style);
        }

        // Info básica
        GUIStyle basicStyle = new GUIStyle(GUI.skin.label);
        basicStyle.fontSize = 16;
        basicStyle.normal.textColor = Color.yellow;

        string basicInfo = $"Puntuación: {currentScore}\n";
        basicInfo += $"Nivel: {currentLevel}\n";
        basicInfo += $"Multiplicador: {currentSpeedMultiplier:F2}x";

        GUI.Label(new Rect(10, 10, 200, 80), basicInfo, basicStyle);
    }
}