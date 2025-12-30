using System.Collections.Generic;
using UnityEngine;

public class ShopEntrance : MonoBehaviour
{
    [Header("Spawn Points for Players in Shop")]
    public List<Transform> shopSpawnPoints;

    private void OnEnable()
    {
        shopSpawnPoints = new List<Transform>();
        for (int i = 1; i <= 4; i++)
        {
            GameObject spawnObj = GameObject.Find($"ShopSpawnPoint{i}");
            if (spawnObj != null)
            {
                shopSpawnPoints.Add(spawnObj.transform);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            int count = Mathf.Min(players.Length, shopSpawnPoints.Count);
            for (int i = 0; i < count; i++)
            {
                players[i].transform.position = shopSpawnPoints[i].position;
                players[i].transform.rotation = shopSpawnPoints[i].rotation;
            }

            // Buscar la cámara de la tienda por tag (el GameObject debe estar activo, pero el componente Camera puede estar deshabilitado)
            Camera shopCam = null;
            GameObject shopCamObj = GameObject.FindWithTag("ShopCamera");
            if (shopCamObj != null)
            {
                shopCam = shopCamObj.GetComponent<Camera>();
            }
            if (shopCam != null)
            {
                // Desactivar la cámara principal solo si está activa y es distinta
                Camera mainCam = Camera.main;
                if (mainCam != null && mainCam != shopCam && mainCam.enabled)
                {
                    mainCam.enabled = false;
                }
                // Activar la cámara de la tienda solo si está desactivada
                if (!shopCam.enabled)
                {
                    shopCam.enabled = true;
                }
            }
        }
    }
}
