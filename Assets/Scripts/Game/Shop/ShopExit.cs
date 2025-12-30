using UnityEngine;

public class ShopExit : MonoBehaviour
{


    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        var exitPoints = new System.Collections.Generic.List<Transform>();
        for (int i = 1; i <= 4; i++)
        {
            GameObject exitObj = GameObject.Find($"ExitShopSpawn{i}");
            if (exitObj != null)
                exitPoints.Add(exitObj.transform);
        }
        int count = Mathf.Min(players.Length, exitPoints.Count);
        for (int i = 0; i < count; i++)
        {
            players[i].transform.position = exitPoints[i].position;
            players[i].transform.rotation = exitPoints[i].rotation;
        }

        Camera shopCam = GameObject.FindWithTag("ShopCamera")?.GetComponent<Camera>();
        if (shopCam != null)
            shopCam.enabled = false;

        Camera mainCam = GameObject.FindWithTag("MainCamera")?.GetComponent<Camera>();
        if (mainCam != null)
            mainCam.enabled = true;
    }
}
