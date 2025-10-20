using UnityEngine;

public class Instakill : MonoBehaviour
{
    [Header("Instakill AOE")]
    public float radius = 5f;
    [Range(0f,180f)] public float angle = 60f; // cone angle
    public int damageAmount = 999999;
    public KeyCode activateKey = KeyCode.K;
    public LayerMask hitLayer = -1; // assign the Enemy layer(s) here

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(activateKey))
        {
            DoInstakill();
        }
    }

    void DoInstakill()
    {
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;

        Collider[] hits = Physics.OverlapSphere(origin, radius, hitLayer);
        foreach (var c in hits)
        {
            if (c == null) continue;
            // OverlapSphere already filtered by hitLayer; proceed to damage any collider found

            Vector3 dir = (c.transform.position - origin);
            dir.y = 0f; // ignore vertical
            float d = dir.magnitude;
            if (d <= 0.01f) d = 0.01f;
            Vector3 dirNorm = dir.normalized;

            float a = Vector3.Angle(forward, dirNorm);
            if (a > angle * 0.5f) continue; // outside cone

            var dmg = c.GetComponentInParent<Game.Combat.IDamageable>() ?? c.GetComponent<Game.Combat.IDamageable>();
            if (dmg != null)
            {
                // build minimal HitboxConfig
                var cfg = new Game.Combat.HitboxConfig
                {
                    damageMultiplier = 1f,
                    damageType = Game.Combat.DamageType.Normal,
                    effects = Game.Combat.DamageEffects.None,
                    knockbackForce = 0f,
                    knockbackDirection = Vector3.zero
                };

                var info = Game.Combat.DamageInfo.Create(damageAmount, cfg, c.bounds.center, dirNorm, transform, 0);
                dmg.TakeDamage(info);
            }
            else
            {
                // fallback: destroy object if not damageable
                Destroy(c.gameObject);
            }
        }
    }
}
