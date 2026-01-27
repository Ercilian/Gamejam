using UnityEngine;

public class SpawnBoss : MonoBehaviour
{
	[Header("Boss Spawn Settings")]
	public GameObject bossPrefab;
	public Transform spawnPoint;

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Car"))
		{
			if (bossPrefab != null && spawnPoint != null)
			{
				Instantiate(bossPrefab, spawnPoint.position, spawnPoint.rotation);
			}
			else
			{
				Debug.LogWarning("Boss prefab o spawn point no asignado en SpawnBoss");
			}
		}
	}
}
