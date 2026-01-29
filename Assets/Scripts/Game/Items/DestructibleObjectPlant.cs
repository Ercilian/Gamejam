using UnityEngine;

public class DestructibleObjectPlant : EntityStats
{

    public override void TakeDamage(int amount)
    {
        base.TakeDamage(amount);
        StartCoroutine(Shake(0.15f, 0.2f)); // duration, magnitude
    }

    private System.Collections.IEnumerator Shake(float duration, float magnitude)
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            transform.localPosition = originalPos + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = originalPos;
    }
    public override void OnEntityDeath()
    {
        ItemDropSystem.Instance.DropFromDestructiblePlant(transform.position);
        Destroy(gameObject);
    }

}
