
using UnityEngine;

public class MapManager : MonoBehaviour
{
    [Header("Map Prefabs (ordered)")]
    public GameObject[] mapPrefabs;

    [Header("Player/Coche")]
    public Transform carTransform;

    private GameObject[] mapInstances;
    private Collider[] mapInstanceColliders;
    private int currentMapIndex = 0;




    // ================================================= Methods =================================================




    void Start()
    {
        int count = mapPrefabs.Length;
        mapInstances = new GameObject[count];
        mapInstanceColliders = new Collider[count];
        for (int i = 0; i < count; i++) // Instanciate the first two maps
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
        for (int i = 0; i < mapPrefabs.Length; i++)
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
        for (int i = 0; i < mapPrefabs.Length; i++)
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

    void InstantiateMap(int index) // Method to instantiate a map at a given index
    {
        if (mapPrefabs[index] != null && mapInstances[index] == null)
        {
            mapInstances[index] = Instantiate(mapPrefabs[index]);
            Collider col = mapInstances[index].GetComponentInChildren<Collider>();
            if (col == null)
            {
                Debug.LogWarning($"No se encontró collider en la instancia de {mapPrefabs[index].name}");
            }
            mapInstanceColliders[index] = col;
        }
    }
}

