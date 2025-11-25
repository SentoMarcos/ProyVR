using System.Collections.Generic;
using UnityEngine;

public class ProceduralGenerator : MonoBehaviour
{
    [Header("Configuración de Generación")]
    public float spawnDistance = 100f;
    public float destroyDistance = 50f;
    public int initialSegments = 10;

    [Header("Prefabs")]
    public GameObject[] edificios;
    public GameObject[] aceras;
    public GameObject[] carreteras;
    public GameObject[] carreterasSeguras;

    [Header("Posiciones de Carril")]
    public float[] carrilPositions = {
        -24f, // Edificio izquierdo (20m)
        -12f, // Acera izquierda (4m)
        -8f,  // Vacío izquierdo (4m)
        -4f,  // Carril 1 (4m)
        0f,   // Carril 2 (4m)
        4f,   // Carril 3 (4m)
        8f,   // Vacío derecho (4m)
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

        // Destruir segmentos atrás
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
        // Verificación adicional para evitar duplicados
        if (generatedSegments.Contains(segmentIndex))
        {
            Debug.LogWarning($"Intento de generar segmento duplicado: {segmentIndex}");
            return;
        }

        GameObject segment = new GameObject($"Segment_{segmentIndex}");
        segment.transform.position = new Vector3(0, 0, segmentIndex * segmentLength);

        // Patrón fijo de elementos
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

    void GenerateRoads(Transform parent, int segmentIndex)
    {
        List<GameObject> roadsToPlace = new List<GameObject>();

        // 1. Garantizar al menos una carretera segura
        GameObject safeRoad = carreterasSeguras[Random.Range(0, carreterasSeguras.Length)];
        roadsToPlace.Add(safeRoad);

        // 2. Garantizar carretera_4
        GameObject carretera4 = System.Array.Find(carreteras, road => road.name.Contains("carretera_4"));
        if (carretera4 == null)
        {
            carretera4 = carreteras[Random.Range(0, carreteras.Length)];
        }
        roadsToPlace.Add(carretera4);

        // 3. Tercera carretera aleatoria
        GameObject[] allRoads = new GameObject[carreteras.Length + carreterasSeguras.Length];
        carreteras.CopyTo(allRoads, 0);
        carreterasSeguras.CopyTo(allRoads, carreteras.Length);

        GameObject thirdRoad = allRoads[Random.Range(0, allRoads.Length)];
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
            if (roadsToPlace[i] != null)
            {
                Vector3 position = new Vector3(carrilPositions[3 + i], 0, parent.position.z);
                Instantiate(roadsToPlace[i], position, Quaternion.identity, parent);
            }
        }
    }

    // Método para depuración: muestra los segmentos actuales
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
}