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

        // ¡AÑADIR COLLIDERS AL SEGMENTO!
        AddCollidersToSegment(segment);

        segments.Add(segment);
        generatedSegments.Add(segmentIndex);

        Debug.Log($"Segmento generado: {segmentIndex} en Z: {segment.transform.position.z}");
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

        // Edificios izquierdos (posición -24)
        if (Mathf.Abs(posX - (-24f)) < 0.1f)
        {
            AddBuildingCollider(obj);
        }
        // Edificios derechos (posición 24)
        else if (Mathf.Abs(posX - 24f) < 0.1f)
        {
            AddBuildingCollider(obj);
        }
        // Aceras (posición -12 y 12)
        else if (Mathf.Abs(posX - (-12f)) < 0.1f || Mathf.Abs(posX - 12f) < 0.1f)
        {
            AddSidewalkCollider(obj);
        }
        // Carreteras (posición -4, 0, 4)
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
            collider.size = new Vector3(20f, 10f, 20f); // Ancho ajustado a 20m
            collider.center = new Vector3(0, 5f, 0);
            Debug.Log($"Collider añadido a edificio en X: {building.transform.position.x}");
        }
    }

    void AddSidewalkCollider(GameObject sidewalk)
    {
        if (sidewalk.GetComponent<Collider>() == null)
        {
            BoxCollider collider = sidewalk.AddComponent<BoxCollider>();
            collider.size = new Vector3(4f, 0.3f, 20f);
            collider.center = new Vector3(0, 0.15f, 0);
            Debug.Log($"Collider añadido a acera en X: {sidewalk.transform.position.x}");
        }
    }

    void AddRoadCollider(GameObject road)
    {
        if (road.GetComponent<Collider>() == null)
        {
            BoxCollider collider = road.AddComponent<BoxCollider>();
            collider.size = new Vector3(4f, 0.1f, 20f);
            collider.center = new Vector3(0, 0.05f, 0);
            collider.isTrigger = true; // Para caminar sobre ella
            Debug.Log($"Collider añadido a carretera en X: {road.transform.position.x}");
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

    // Métodos públicos para acceder a información
    public int GetActiveSegmentCount()
    {
        return segments.Count;
    }

    public float GetSegmentLength()
    {
        return segmentLength;
    }

    public List<GameObject> GetActiveSegments()
    {
        return new List<GameObject>(segments);
    }

    // Añade esto al final de la clase, antes de la última llave }
    void OnGUI()
    {
        if (segments.Count > 0 && segments[0] != null)
        {
            // Verificar colliders en el primer segmento
            Transform firstSegment = segments[0].transform;
            int colliderCount = 0;

            foreach (Transform child in firstSegment)
            {
                if (child.GetComponent<Collider>() != null)
                {
                    colliderCount++;
                }
            }

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 16;
            style.normal.textColor = Color.yellow;

            string info = $"Segmentos: {segments.Count}\n";
            info += $"Colliders en primer segmento: {colliderCount}/9\n";
            info += $"Player Z: {player?.position.z:F1}";

            GUI.Label(new Rect(10, 50, 300, 100), info, style);
        }
    }

}