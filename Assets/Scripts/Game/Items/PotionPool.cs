using UnityEngine;
using System.Collections;

public class PotionPool : MonoBehaviour
{
    private PotionData potion;
    private float maxDuration = 10f; // duración máxima en segundos
    private float radius = 1.5f;
    private int totalTicks = 16; // puedes ajustar el número de ticks

    private Coroutine effectCoroutine;

    public void Setup(PotionData potionData)
    {
        potion = potionData;

        var visual = transform.Find("Visual");
        if (visual != null)
        {
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                bool hasHeal = potion.effectType == PotionEffectType.Heal || potion.effectType2 == PotionEffectType.Heal;
                bool hasShield = potion.effectType == PotionEffectType.Shield || potion.effectType2 == PotionEffectType.Shield;

                if (hasHeal && hasShield)
                    renderer.material.color = new Color(0.2f, 0.9f, 0.7f, 0.5f);
                else if (hasShield)
                    renderer.material.color = new Color(0.2f, 0.5f, 1f, 0.5f);
                else if (hasHeal)
                    renderer.material.color = new Color(0.2f, 1f, 0.2f, 0.5f);
            }
        }

        effectCoroutine = StartCoroutine(ApplyEffectsOverTime());
        Invoke(nameof(DestroyPool), maxDuration);
    }

    private IEnumerator ApplyEffectsOverTime()
    {
        float tickInterval = 0.7f;
        float minInterval = 0.15f;
        int tick = 0;

        float totalHeal = 0f, totalShield = 0f;
        if (potion.effectType == PotionEffectType.Heal) totalHeal += potion.effectAmount;
        if (potion.effectType == PotionEffectType.Shield) totalShield += potion.effectAmount;
        if (potion.effectType2 == PotionEffectType.Heal) totalHeal += potion.effectAmount2;
        if (potion.effectType2 == PotionEffectType.Shield) totalShield += potion.effectAmount2;

        while (tick < totalTicks)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, radius, LayerMask.GetMask("Player"));
            int playerCount = hits.Length;
            bool effectApplied = false;

            if (playerCount > 0)
            {
                float healPerPlayer = (totalHeal / totalTicks) / playerCount;
                float shieldPerPlayer = (totalShield / totalTicks) / playerCount;

                foreach (var hit in hits)
                {
                    var stats = hit.GetComponent<EntityStats>();
                    if (stats != null)
                    {
                        // Solo cura si no está a vida máxima
                        if (healPerPlayer > 0 && stats.CurrentHP < stats.MaxHP)
                        {
                            stats.Heal(Mathf.CeilToInt(healPerPlayer));
                            effectApplied = true;
                        }
                        // Solo da escudo si no está a escudo máximo
                        if (shieldPerPlayer > 0 && stats.CurrentShield < stats.MaxShield)
                        {
                            stats.AddShield(Mathf.CeilToInt(shieldPerPlayer));
                            effectApplied = true;
                        }
                    }
                }
            }

            // Solo avanza el tick si al menos un jugador recibe efecto
            if (effectApplied)
                tick++;

            tickInterval = Mathf.Max(minInterval, tickInterval * 0.85f);
            yield return new WaitForSeconds(tickInterval);
        }

        DestroyPool();
    }

    private void DestroyPool()
    {
        if (effectCoroutine != null)
            StopCoroutine(effectCoroutine);
        Destroy(gameObject);
    }
}
