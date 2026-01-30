
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class MapManager : MonoBehaviour
{
    [Header("Random Map Pool")]
    public GameObject[] randomMapPool; // Solo mapas aleatorios

    [Header("Fixed Maps (pasillos, bosses, etc.)")]
    public GameObject[] fixedMaps; // Mapas fijos

    [Header("Map Sequence (-1=random, >=0=fijo)")]
    public int[] mapSequence; // -1 = random, >=0 = índice de fixedMaps

    [Header("Player/Coche")]
    public Transform carTransform;

    private GameObject[] mapInstances;
    private Collider[] mapInstanceColliders;
    private int currentMapIndex = 0;


    // Lista final de prefabs a instanciar según la secuencia
    private List<GameObject> finalMapList;

    // Lista global de checkpoints en orden
    public List<Transform> globalCheckpoints = new List<Transform>();

    // ================================================= Methods =================================================

    void Start()
    {
        int count = mapSequence.Length;
        mapInstances = new GameObject[count];
        mapInstanceColliders = new Collider[count];

        finalMapList = BuildFinalMapList();

        for (int i = 0; i < count; i++) // Instanciar los dos primeros mapas
        {
            if (i == 0 || i == 1)
            {
                InstantiateMap(i);
            }
        }
        UpdateMapActivation();
    }

    void Update()
    {
        int newMapIndex = GetCurrentMapIndex();
        if (newMapIndex != currentMapIndex)
        {
            currentMapIndex = newMapIndex;
            UpdateMapActivation();
        }
    }

    int GetCurrentMapIndex() // Method to determine which map the car is currently in
    {
        Vector3 carPos = carTransform.position;
        for (int i = 0; i < finalMapList.Count; i++)
        {
            Collider col = mapInstanceColliders[i];
            if (col != null)
            {
                Bounds b = col.bounds;
                if (b.Contains(carPos))
                {
                    return i;
                }
            }
        }
        return currentMapIndex;
    }

    void UpdateMapActivation() // Activate/deactivate maps based on current index
    {
        for (int i = 0; i < finalMapList.Count; i++)
        {
            if ((i == currentMapIndex - 1 || i == currentMapIndex || i == currentMapIndex + 1) && mapInstances[i] == null) // Instantiate nearby maps
            {
                InstantiateMap(i);
            }
            if (i == currentMapIndex - 1 || i == currentMapIndex || i == currentMapIndex + 1)
            {
                if (mapInstances[i] != null)
                    mapInstances[i].SetActive(true);
            }
            else if (i < currentMapIndex - 1) // Destroy the maps that are behind
            {
                if (mapInstances[i] != null)
                {
                    // Proteger al boss de ser destruido junto con el mapa
                    FrogCombat bossCombat = mapInstances[i].GetComponentInChildren<FrogCombat>();
                    if (bossCombat != null)
                    {
                        bossCombat.transform.parent = null; // Desparentar el boss antes de destruir el mapa
                    }
                    Destroy(mapInstances[i]);
                    mapInstances[i] = null;
                    mapInstanceColliders[i] = null;
                }
            }
            else // Deactivate the maps that are ahead
            {
                if (mapInstances[i] != null)
                    mapInstances[i].SetActive(false);
            }
        }
    }

    void InstantiateMap(int index) // Instancia el mapa alineando Entry y Exit y recoge checkpoints
    {
        if (finalMapList[index] != null && mapInstances[index] == null)
        {
            GameObject newMap;
            if (index == 0)
            {
                // Instanciar el primer mapa en una posición provisional
                Vector3 provisionalPos = new Vector3(0f, -0.5f, 0f);
                newMap = Instantiate(finalMapList[index], provisionalPos, Quaternion.identity);
                // Buscar el Entry del primer mapa
                Transform entry = newMap.transform.Find("Entry");
                if (entry != null)
                {
                    // Queremos que el Entry esté en (10, -0.5, 0)
                    Vector3 desiredEntryPos = new Vector3(-10f, -0.5f, 0f);
                    Vector3 offset = desiredEntryPos - entry.position;
                    newMap.transform.position += offset;
                }
                else
                {
                    Debug.LogWarning($"No se encontró 'Entry' en el primer mapa al instanciar");
                }
            }
            else
            {
                Vector3 spawnPos = new Vector3(0f, -0.5f, 0f);
                newMap = Instantiate(finalMapList[index], spawnPos, Quaternion.identity);
                // Calcular la posición correcta
                if (mapInstances[index - 1] != null)
                {
                    // Buscar el Exit del mapa anterior
                    Transform prevExit = mapInstances[index - 1].transform.Find("Exit");
                    // Buscar el Entry del nuevo mapa
                    Transform newEntry = newMap.transform.Find("Entry");
                    if (prevExit != null && newEntry != null)
                    {
                        // Offset necesario para alinear Entry con Exit
                        Vector3 offset = prevExit.position - newEntry.position;
                        newMap.transform.position += offset;
                    }
                    else
                    {
                        Debug.LogWarning($"No se encontró 'Entry' o 'Exit' en los mapas al instanciar el índice {index}");
                    }
                }
            }
            // Guardar referencia
            mapInstances[index] = newMap;
            Collider col = newMap.GetComponentInChildren<Collider>();
            if (col == null)
            {
                Debug.LogWarning($"No se encontró collider en la instancia de {finalMapList[index].name}");
            }
            mapInstanceColliders[index] = col;

            // Recoger checkpoints de este mapa y añadirlos a la lista global
            Transform checkpointsParent = newMap.transform.Find("Checkpoints");
            if (checkpointsParent != null)
            {
                for (int i = 0; i < checkpointsParent.childCount; i++)
                {
                    globalCheckpoints.Add(checkpointsParent.GetChild(i));
                }
            }

            // Refrescar anchors de cámara tras instanciar el mapa
            CameraMovement cam = FindObjectOfType<CameraMovement>();
            cam.RefreshZAnchors();
            
        }
    }

    // Construye la lista final de mapas a instanciar según la secuencia
    private List<GameObject> BuildFinalMapList()
    {
        List<GameObject> result = new List<GameObject>(mapSequence.Length);
        List<int> usedRandomIndices = new List<int>();
        for (int i = 0; i < mapSequence.Length; i++)
        {
            if (mapSequence[i] == -1)
            {
                // Elegir un random que no se haya usado (si hay suficientes)
                int idx;
                int tries = 0;
                do {
                    idx = Random.Range(0, randomMapPool.Length);
                    tries++;
                } while (usedRandomIndices.Contains(idx) && usedRandomIndices.Count < randomMapPool.Length && tries < 100);
                usedRandomIndices.Add(idx);
                result.Add(randomMapPool[idx]);
            }
            else
            {
                if (mapSequence[i] >= 0 && mapSequence[i] < fixedMaps.Length)
                    result.Add(fixedMaps[mapSequence[i]]);
                else
                    result.Add(null);
            }
        }
        return result;
    }

    // Utilidad para generar un array de índices aleatorios
    private int[] GenerateRandomOrder(int count)
    {
        List<int> indices = new List<int>(count);
        for (int i = 0; i < count; i++) indices.Add(i);
        for (int i = 0; i < count; i++)
        {
            int swap = Random.Range(i, count);
            int temp = indices[i];
            indices[i] = indices[swap];
            indices[swap] = temp;
        }
        return indices.ToArray();
    }
}

