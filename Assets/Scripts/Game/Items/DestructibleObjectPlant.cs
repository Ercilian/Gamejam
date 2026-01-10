using UnityEngine;

public class DestructibleObjectPlant : EntityStats
{

    public override void OnEntityDeath()
    {
        ItemDropSystem.Instance.DropFromDestructiblePlant(transform.position);
        Destroy(gameObject);
    }

}
