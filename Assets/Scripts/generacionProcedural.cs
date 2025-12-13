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
    public GameObject prefabCoche;

    [Header("Colisionadores automáticos")]
    public bool autoGenerateColliders = true;
    public bool preferMeshColliders = true;
    public bool addPerChildMesh = true;
    public bool forceConvexMesh = false;
    public PhysicsMaterial buildingMaterial;
    public PhysicsMaterial sidewalkMaterial;
    public PhysicsMaterial roadMaterial;

    [Header("Fallback por código (sin prefabs)")]
    public Vector3 fallbackBuildingScale = new Vector3(20f, 20f, 20f);
    public Vector3 fallbackSidewalkScale = new Vector3(4f, 0.4f, 20f);
    public Vector3 fallbackRoadScale = new Vector3(4f, 0.3f, 20f);
    public Color fallbackBuildingColor = new Color(0.5f, 0.5f, 0.5f);
    public Color fallbackSidewalkColor = new Color(0.7f, 0.7f, 0.7f);
    public Color fallbackRoadColor = Color.black;

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

        // Generar contenido usando el sistema mejorado con fallback
        GenerateEdificio(segment.transform, 0, edificios);
        GenerateAcera(segment.transform, 1, aceras);
        GenerateRoads(segment.transform, segmentIndex);
        GenerateAcera(segment.transform, 7, aceras);
        GenerateEdificio(segment.transform, 8, edificios);

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
        List<GameObject> instantiatedRoads = new List<GameObject>();

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
            Vector3 position = new Vector3(carrilPositions[3 + i], 0, parent.position.z);
            GameObject prefab = roadsToPlace[i];
            GameObject instance;

            if (prefab != null)
            {
                instance = Instantiate(prefab, position, Quaternion.identity, parent);
            }
            else
            {
                instance = CreatePrimitiveFallback(
                    PrimitiveType.Cube,
                    fallbackRoadScale,
                    fallbackRoadColor,
                    "RoadFallback",
                    parent,
                    position,
                    Quaternion.identity);
            }

            if (IsBadRoad(prefab))
            {
                instance.name = $"{instance.name}_PELIGROSA";
            }

            EnsureCollider(instance, roadMaterial);
            instantiatedRoads.Add(instance);
        }

        // REGLA DE LOS COCHES (del segundo código)
        List<GameObject> safeRoadsInSegment = new List<GameObject>();
        for (int i = 0; i < 3; i++)
        {
            if (!IsBadRoad(roadsToPlace[i]))
            {
                safeRoadsInSegment.Add(instantiatedRoads[i]);
            }
        }

        if (safeRoadsInSegment.Count == 2 && prefabCoche != null)
        {
            GameObject chosenRoad = safeRoadsInSegment[Random.Range(0, safeRoadsInSegment.Count)];
            Vector3 carPos = chosenRoad.transform.position + new Vector3(0, 0.66f, 0);

            GameObject carInstance = Instantiate(prefabCoche, chosenRoad.transform);
            carInstance.transform.localPosition = new Vector3(0, 0.5f, 0);
            carInstance.transform.localRotation = Quaternion.identity;
            carInstance.transform.localScale = new Vector3(100f, 100f, 100f);
        }
    }

    GameObject GetRandomSafeRoad()
    {
        return GetRandomPrefab(carreterasSeguras);
    }

    GameObject GetRandomBadRoad()
    {
        return GetRandomPrefab(carreteras);
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
        UpdateLevelDisplay();
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

    // Métodos del segundo código para fallback y colisiones
    void GenerateEdificio(Transform parent, int laneIndex, GameObject[] prefabs)
    {
        Vector3 position = new Vector3(carrilPositions[laneIndex], 0, parent.position.z);
        Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 4) * 90f, 0);

        GameObject instance = SpawnPrefabOrPrimitive(
            prefabs,
            position,
            rotation,
            parent,
            PrimitiveType.Cube,
            fallbackBuildingScale,
            fallbackBuildingColor,
            "BuildingFallback");
        EnsureCollider(instance, buildingMaterial);
    }

    void GenerateAcera(Transform parent, int laneIndex, GameObject[] prefabs)
    {
        Vector3 position = new Vector3(carrilPositions[laneIndex], 0, parent.position.z);
        GameObject instance = SpawnPrefabOrPrimitive(
            prefabs,
            position,
            Quaternion.identity,
            parent,
            PrimitiveType.Cube,
            fallbackSidewalkScale,
            fallbackSidewalkColor,
            "SidewalkFallback");
        EnsureCollider(instance, sidewalkMaterial);
    }

    private GameObject SpawnPrefabOrPrimitive(
        GameObject[] prefabs,
        Vector3 position,
        Quaternion rotation,
        Transform parent,
        PrimitiveType fallbackPrimitive,
        Vector3 fallbackScale,
        Color fallbackColor,
        string fallbackName)
    {
        GameObject instance = GetRandomPrefabInstance(prefabs, position, rotation, parent);

        if (instance == null)
        {
            instance = CreatePrimitiveFallback(
                fallbackPrimitive,
                fallbackScale,
                fallbackColor,
                fallbackName,
                parent,
                position,
                rotation);
        }

        return instance;
    }

    private GameObject GetRandomPrefabInstance(GameObject[] prefabs, Vector3 position, Quaternion rotation, Transform parent)
    {
        var selected = GetRandomPrefab(prefabs);
        if (selected == null)
            return null;

        return Instantiate(selected, position, rotation, parent);
    }

    private GameObject GetRandomPrefab(GameObject[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0)
            return null;

        return prefabs[Random.Range(0, prefabs.Length)];
    }

    private GameObject CreatePrimitiveFallback(
        PrimitiveType primitiveType,
        Vector3 scale,
        Color color,
        string namePrefix,
        Transform parent,
        Vector3 position,
        Quaternion rotation)
    {
        GameObject primitive = GameObject.CreatePrimitive(primitiveType);
        primitive.name = $"{namePrefix}_{primitiveType}";
        primitive.transform.SetParent(parent, false);
        primitive.transform.SetPositionAndRotation(position, rotation);
        primitive.transform.localScale = scale;

        var renderer = primitive.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            renderer.sharedMaterial = material;
        }

        return primitive;
    }

    private void EnsureCollider(GameObject root, PhysicsMaterial material)
    {
        if (!autoGenerateColliders || root == null)
            return;

        var existing = root.GetComponentInChildren<Collider>();
        if (existing != null)
        {
            ApplyMaterial(existing, material, includeChildren: true, root: root);
            return;
        }

        bool added = false;

        if (preferMeshColliders)
        {
            var meshFilters = addPerChildMesh
                ? root.GetComponentsInChildren<MeshFilter>(true)
                : new[] { root.GetComponent<MeshFilter>() };

            foreach (var meshFilter in meshFilters)
            {
                if (meshFilter == null || meshFilter.sharedMesh == null)
                    continue;

                var target = meshFilter.gameObject;
                var meshCollider = target.GetComponent<MeshCollider>();
                if (meshCollider == null)
                    meshCollider = target.AddComponent<MeshCollider>();

                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.convex = forceConvexMesh;
                if (material != null)
                    meshCollider.sharedMaterial = material;

                added = true;

                if (!addPerChildMesh)
                    break;
            }
        }

        if (!added)
        {
            var renderer = root.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                var box = renderer.gameObject.AddComponent<BoxCollider>();
                if (material != null)
                    box.sharedMaterial = material;
                added = true;
            }
        }

        if (!added)
        {
            var fallback = root.AddComponent<BoxCollider>();
            if (material != null)
                fallback.sharedMaterial = material;
        }
    }

    private void ApplyMaterial(Collider firstCollider, PhysicsMaterial material, bool includeChildren, GameObject root)
    {
        if (material == null)
            return;

        if (!includeChildren)
        {
            firstCollider.sharedMaterial = material;
            return;
        }

        var colliders = root.GetComponentsInChildren<Collider>(true);
        foreach (var col in colliders)
        {
            col.sharedMaterial = material;
        }
    }

    // ========== MÉTODOS PÚBLICOS (del primer código) ==========
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