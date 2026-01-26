using UnityEngine;
using System.Collections;

public class PotionPool : MonoBehaviour
{
    [Header("Potion Pool Settings")]
    private PotionData potion;
    private float maxDuration = 7f;
    private float radius = 1.5f;
    private int totalTicks = 16;

    private Coroutine effectCoroutine;
    private Coroutine scaleCoroutine;
    private Vector3 initialScale;
    private Vector3 finalScale;
    private Vector3 targetScale;




    // ================================================= Unity Methods =================================================




    public void Setup(PotionData potionData) // Initialize the potion pool with the given potion data
    {
        potion = potionData;
        initialScale = transform.localScale;
        finalScale = initialScale * 0.5f;
        targetScale = initialScale;

        effectCoroutine = StartCoroutine(ApplyEffectsOverTime());
        scaleCoroutine = StartCoroutine(SmoothScale());
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
            {
                tick++;
                // Calcula la nueva escala objetivo
                float t = Mathf.Clamp01((float)tick / totalTicks);
                targetScale = Vector3.Lerp(initialScale, finalScale, t);
            }

            tickInterval = Mathf.Max(minInterval, tickInterval * 0.85f);
            yield return new WaitForSeconds(tickInterval);
        }

        DestroyPool();
    }

    private IEnumerator SmoothScale()
    {
        while (true)
        {
            // Interpola suavemente hacia la escala objetivo
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 8f);
            yield return null;
        }
    }

    private void DestroyPool()
    {
        if (effectCoroutine != null)
            StopCoroutine(effectCoroutine);
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);
        Destroy(gameObject);
    }
}
