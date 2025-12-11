using UnityEngine;
using System.Collections.Generic;

public class MapManager : MonoBehaviour
{
    [Header("Map Prefabs (ordered)")]
    public GameObject[] mapPrefabs;

    [Header("Colliders de cada mapa (en el mismo orden)")]
    public Collider[] mapColliders; // Asigna manualmente los colliders en el inspector

    [Header("Player/Coche")]
    public Transform carTransform; 

    private int currentMapIndex = 0;

    void Start()
    {
        if (mapPrefabs.Length != mapColliders.Length)
        {
            Debug.LogError("El número de prefabs y colliders asignados no coincide.");
        }
        UpdateMapActivation();
    }

    void Update()
    {
        int newMapIndex = GetCurrentMapIndex();
        if (newMapIndex != currentMapIndex)
        {
            Debug.Log($"Coche ha cambiado de mapa: {currentMapIndex} -> {newMapIndex} ({mapPrefabs[newMapIndex].name})");
            currentMapIndex = newMapIndex;
            UpdateMapActivation();
        }
        else
        {
            Debug.Log($"Coche está en el mapa: {currentMapIndex} ({mapPrefabs[currentMapIndex].name})");
        }
    }

    int GetCurrentMapIndex()
    {
        Vector3 carPos = carTransform.position;
        for (int i = 0; i < mapColliders.Length; i++)
        {
            Collider col = mapColliders[i];
            if (col != null)
            {
                Bounds b = col.bounds;
                Debug.Log($"Mapa {i} ({mapPrefabs[i].name}): Collider min={b.min}, max={b.max}, Car pos={carPos}");
                if (b.Contains(carPos))
                {
                    Debug.Log($"Coche está dentro del mapa {i} ({mapPrefabs[i].name})");
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
            // Mantén activo el anterior, el actual y el siguiente
            if (i == currentMapIndex - 1 || i == currentMapIndex || i == currentMapIndex + 1)
            {
                if (mapPrefabs[i] != null)
                    mapPrefabs[i].SetActive(true);
            }
            // Destruye los mapas que estén dos o más posiciones atrás
            else if (i < currentMapIndex - 1)
            {
                if (mapPrefabs[i] != null)
                {
                    Destroy(mapPrefabs[i]);
                    mapPrefabs[i] = null;
                }
            }
            // Desactiva los mapas que están más adelante
            else
            {
                if (mapPrefabs[i] != null)
                    mapPrefabs[i].SetActive(false);
            }
        }
    }
}
