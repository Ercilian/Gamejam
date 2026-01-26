using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [Header("Spawn Activation Range")]
    public float activationRange = 15f;

    public void TryActivate(Transform target, System.Action<EnemySpawnPoint> onActivate)
    {
        if (Vector3.Distance(transform.position, target.position) <= activationRange)
        {
            onActivate?.Invoke(this);
        }
    }
}