using UnityEngine;
using System.Collections.Generic;

public class MapManager : MonoBehaviour
{
    [Header("Map Prefabs")]
    public GameObject[] mapPrefabs;

    [Header("Colliders")]
    public Collider[] mapColliders;

    [Header("Car")]
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
            currentMapIndex = newMapIndex;
            UpdateMapActivation();
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
            if (i == currentMapIndex - 1 || i == currentMapIndex || i == currentMapIndex + 1)
            {
                if (mapPrefabs[i] != null)
                    mapPrefabs[i].SetActive(true);
            }
            else if (i < currentMapIndex - 1)
            {
                if (mapPrefabs[i] != null)
                {
                    Destroy(mapPrefabs[i]);
                    mapPrefabs[i] = null;
                }
            }
            else
            {
                if (mapPrefabs[i] != null)
                    mapPrefabs[i].SetActive(false);
            }
        }
    }
}
