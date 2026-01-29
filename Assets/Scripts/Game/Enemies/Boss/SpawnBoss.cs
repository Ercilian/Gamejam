using UnityEngine;

public class SpawnBoss : MonoBehaviour
{
	[Header("Boss Spawn Settings")]
	public GameObject bossPrefab;
	public Transform spawnPoint;

	private void OnTriggerEnter(Collider other)
	{
		//Debug.Log($"[SpawnBoss] Trigger enter by: {other.name} (tag: {other.tag})");
		if (other.CompareTag("Car"))
		{
			Debug.Log("[SpawnBoss] Car detected in trigger.");
			if (bossPrefab != null && spawnPoint != null)
			{
				//Debug.Log("[SpawnBoss] Instantiating boss at spawn point.");
				GameObject boss = Instantiate(bossPrefab, spawnPoint.position, spawnPoint.rotation);
				// Asignar el target de la cámara al boss
				CameraMovement cam = FindObjectOfType<CameraMovement>();
				if (cam != null) cam.SetTarget(boss.transform);
			}
			else
			{
				Debug.LogWarning($"[SpawnBoss] Boss prefab o spawn point no asignado. bossPrefab: {(bossPrefab != null)}, spawnPoint: {(spawnPoint != null)}");
			}
		}
		else
		{
			//Debug.Log("[SpawnBoss] Triggered by non-car object.");
		}
	}
}
