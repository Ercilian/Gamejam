
using UnityEngine;

public class MapManager : MonoBehaviour
{
    [Header("Map Prefabs (ordered)")]
    public GameObject[] mapPrefabs; // Prefabs de los mapas, asigna en el inspector

    [Header("Player/Coche")]
    public Transform carTransform;

    // Instancias activas de los mapas
    private GameObject[] mapInstances;
    // Colliders de las instancias activas
    private Collider[] mapInstanceColliders;

    private int currentMapIndex = 0;

    void Start()
    {
        int count = mapPrefabs.Length;
        mapInstances = new GameObject[count];
        mapInstanceColliders = new Collider[count];
        // Instancia solo el primer mapa y el siguiente
        for (int i = 0; i < count; i++)
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

    int GetCurrentMapIndex()
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

    void UpdateMapActivation()
    {
        for (int i = 0; i < mapPrefabs.Length; i++)
        {
            // Instancia el anterior, actual y siguiente si no existen
            if ((i == currentMapIndex - 1 || i == currentMapIndex || i == currentMapIndex + 1) && mapInstances[i] == null)
            {
                InstantiateMap(i);
            }
            // Activa el anterior, actual y siguiente
            if (i == currentMapIndex - 1 || i == currentMapIndex || i == currentMapIndex + 1)
            {
                if (mapInstances[i] != null)
                    mapInstances[i].SetActive(true);
            }
            // Destruye los mapas que estén dos o más posiciones atrás
            else if (i < currentMapIndex - 1)
            {
                if (mapInstances[i] != null)
                {
                    Destroy(mapInstances[i]);
                    mapInstances[i] = null;
                    mapInstanceColliders[i] = null;
                }
            }
            // Desactiva los mapas que están más adelante
            else
            {
                if (mapInstances[i] != null)
                    mapInstances[i].SetActive(false);
            }
        }
    }

    void InstantiateMap(int index)
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

