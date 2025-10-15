using Game.Player.Combat.Pajaro.Habilidad;
using UnityEngine;

public class HabilidadPajaro : MonoBehaviour
{
    [Header("Habilidad Especial: Giro")] 
    public float duracionGiro = 2f;
    public float intervaloTick = 0.2f;
    public float radioDaño = 2f;
    [Header("Configuración de Daño")] 
    public HabilidadDamageConfig damageConfig = new HabilidadDamageConfig();
    private bool girando = false;

    // Update is called once per frame
    void Update()
    {
        if (!girando && Input.GetKeyDown(damageConfig.teclaHabilidad))
        {
            StartCoroutine(GiroEspecial());
        }
    }

    private System.Collections.IEnumerator GiroEspecial()
    {
        girando = true;
        float tiempo = 0f;
        while (tiempo < duracionGiro)
        {
            // Sin giro visual
            HacerTickDaño();
            float tickRestante = Mathf.Min(intervaloTick, duracionGiro - tiempo);
            yield return new WaitForSeconds(tickRestante);
            tiempo += tickRestante;
        }
        girando = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radioDaño);
    }

    private void HacerTickDaño()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radioDaño, damageConfig.layerEnemigos);
        foreach (var col in hits)
        {
            var damageable = col.GetComponentInParent<Game.Combat.IDamageable>() ?? col.GetComponent<Game.Combat.IDamageable>();
            if (damageable != null)
            {
                Vector3 hitPoint = col.ClosestPoint(transform.position);
                Vector3 hitDir = (col.transform.position - transform.position).normalized;
                hitDir.y = 0f;
                hitDir = hitDir.normalized;
                // Mapear tipo elemental a DamageType
                Game.Combat.DamageType tipo = (Game.Combat.DamageType)damageConfig.damageType;
                var config = new Game.Combat.HitboxConfig {
                    damageMultiplier = 1f,
                    damageType = tipo,
                    effects = Game.Combat.DamageEffects.Knockback,
                    knockbackForce = damageConfig.knockbackForce,
                    knockbackDirection = hitDir
                };
                var info = Game.Combat.DamageInfo.Create(damageConfig.dañoPorTick, config, hitPoint, hitDir, transform, 0);
                damageable.TakeDamage(info);
            }
        }
    }
}
