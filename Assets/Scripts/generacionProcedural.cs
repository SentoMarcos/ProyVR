using System.Collections.Generic;
using UnityEngine;

public class ProceduralGenerator : MonoBehaviour
{
    [Header("Configuraci�n de Generaci�n")]
    public float spawnDistance = 100f;
    public float destroyDistance = 50f;
    public int initialSegments = 10;

    [Header("Prefabs")]
    public GameObject[] edificios;
    public GameObject[] aceras;
    public GameObject[] carreteras;
    public GameObject[] carreterasSeguras;

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
        -24f, // Edificio izquierdo (20m)
        -12f, // Acera izquierda (4m)
        -8f,  // Vac�o izquierdo (4m)
        -4f,  // Carril 1 (4m)
        0f,   // Carril 2 (4m)
        4f,   // Carril 3 (4m)
        8f,   // Vac�o derecho (4m)
        12f,  // Acera derecha (4m)
        24f   // Edificio derecho (20m)
    };

    private Transform player;
    private List<GameObject> segments = new List<GameObject>();
    private float segmentLength = 20f;
    private int lastGeneratedSegment = -1;
    private HashSet<int> generatedSegments = new HashSet<int>();

    void Start()
    {
        player = Camera.main.transform;

        // Generar segmentos iniciales
        for (int i = 0; i < initialSegments; i++)
        {
            if (!generatedSegments.Contains(i))
            {
                GenerateSegment(i);
                generatedSegments.Add(i);
            }
        }
    }

    void Update()
    {
        if (player == null) return;

        float playerZ = player.position.z;
        int currentPlayerSegment = Mathf.FloorToInt(playerZ / segmentLength);

        // Generar nuevos segmentos adelante
        int segmentsToGenerate = Mathf.FloorToInt(spawnDistance / segmentLength);
        int targetSegment = currentPlayerSegment + segmentsToGenerate;

        for (int segmentIndex = currentPlayerSegment; segmentIndex <= targetSegment; segmentIndex++)
        {
            if (!generatedSegments.Contains(segmentIndex))
            {
                GenerateSegment(segmentIndex);
                generatedSegments.Add(segmentIndex);
                lastGeneratedSegment = Mathf.Max(lastGeneratedSegment, segmentIndex);
            }
        }

        // Destruir segmentos atr�s
        CleanupSegments(playerZ);
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
                int segmentIndex = Mathf.FloorToInt(segmentZ / segmentLength);
                generatedSegments.Remove(segmentIndex);

                Destroy(segments[i]);
                segments.RemoveAt(i);
            }
        }
    }

    void GenerateSegment(int segmentIndex)
    {
        // Verificaci�n adicional para evitar duplicados
        if (generatedSegments.Contains(segmentIndex))
        {
            Debug.LogWarning($"Intento de generar segmento duplicado: {segmentIndex}");
            return;
        }

        GameObject segment = new GameObject($"Segment_{segmentIndex}");
        segment.transform.position = new Vector3(0, 0, segmentIndex * segmentLength);

        // Patr�n fijo de elementos
        GenerateEdificio(segment.transform, 0, edificios);
        GenerateAcera(segment.transform, 1, aceras);
        GenerateRoads(segment.transform, segmentIndex);
        GenerateAcera(segment.transform, 7, aceras);
        GenerateEdificio(segment.transform, 8, edificios);

        segments.Add(segment);
        generatedSegments.Add(segmentIndex);

        Debug.Log($"Segmento generado: {segmentIndex} en Z: {segment.transform.position.z}");
    }

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

    void GenerateRoads(Transform parent, int segmentIndex)
    {
        List<GameObject> roadsToPlace = new List<GameObject>();

        // 1. Garantizar al menos una carretera segura
        GameObject safeRoad = GetRandomPrefab(carreterasSeguras);
        roadsToPlace.Add(safeRoad);

        // 2. Garantizar carretera_4
        GameObject carretera4 = null;
        if (carreteras != null && carreteras.Length > 0)
        {
            carretera4 = System.Array.Find(carreteras, road => road != null && road.name.Contains("carretera_4"));
            if (carretera4 == null)
            {
                carretera4 = GetRandomPrefab(carreteras);
            }
        }
        roadsToPlace.Add(carretera4);

        // 3. Tercera carretera aleatoria
        GameObject thirdRoad = GetRandomCombined(carreteras, carreterasSeguras);
        roadsToPlace.Add(thirdRoad);

        // 4. Mezclar aleatoriamente
        for (int i = 0; i < roadsToPlace.Count; i++)
        {
            GameObject temp = roadsToPlace[i];
            int randomIndex = Random.Range(i, roadsToPlace.Count);
            roadsToPlace[i] = roadsToPlace[randomIndex];
            roadsToPlace[randomIndex] = temp;
        }

        // 5. Instanciar en las posiciones
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

            EnsureCollider(instance, roadMaterial);
        }
    }

    // M�todo para depuraci�n: muestra los segmentos actuales
    void DebugSegments()
    {
        string debugInfo = $"Segmentos activos: {segments.Count}\n";
        foreach (var segment in segments)
        {
            if (segment != null)
            {
                debugInfo += $"- {segment.name} at Z: {segment.transform.position.z}\n";
            }
        }
        Debug.Log(debugInfo);
    }

    public void SetSpawnDistance(float distance)
    {
        spawnDistance = distance;
    }

    public void SetDestroyDistance(float distance)
    {
        destroyDistance = distance;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        for (int i = 0; i < carrilPositions.Length; i++)
        {
            Vector3 pos = new Vector3(carrilPositions[i], 0, 0);
            Gizmos.DrawWireCube(pos, new Vector3(4f, 1f, 1f));
        }
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

    private GameObject GetRandomCombined(GameObject[] a, GameObject[] b)
    {
        int countA = a?.Length ?? 0;
        int countB = b?.Length ?? 0;
        int total = countA + countB;
        if (total == 0)
            return null;

        int index = Random.Range(0, total);
        if (index < countA)
        {
            return a[index];
        }
        else
        {
            return b[index - countA];
        }
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
}