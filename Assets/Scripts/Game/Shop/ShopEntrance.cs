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
                shopSpawnPoints.Add(spawnObj.transform);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        int count = Mathf.Min(players.Length, shopSpawnPoints.Count);
        for (int i = 0; i < count; i++)
        {
            players[i].transform.position = shopSpawnPoints[i].position;
            players[i].transform.rotation = shopSpawnPoints[i].rotation;
        }

        Camera shopCam = GameObject.FindWithTag("ShopCamera")?.GetComponent<Camera>();
        if (shopCam == null) return;

        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam != shopCam)
            mainCam.enabled = false;
        shopCam.enabled = true;
    }
}
