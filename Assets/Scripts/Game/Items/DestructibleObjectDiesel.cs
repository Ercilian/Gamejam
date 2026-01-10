using UnityEngine;

public class DestructibleObjectDiesel : EntityStats
{

public override void OnEntityDeath()
    {
        ItemDropSystem.Instance.DropFromDestructibleDiesel(transform.position);
        Destroy(gameObject);
    }
}
