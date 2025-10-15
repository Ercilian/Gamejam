using UnityEngine;
using Game.Combat;

namespace Game.Enemies
{
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("Configuración de Vida")]
        public int maxHealth = 100;
        public bool showDebugLogs = true;
        public bool isElite = false;
        
        [Header("Resistencias por Tipo de Daño")]
        [Range(0f, 1f)] public float normalResistance = 0f;      // 0 = sin resistencia, 1 = inmune
        [Range(0f, 1f)] public float criticoResistance = 0f;
        [Range(0f, 1f)] public float fuegoResistance = 0f;
        [Range(0f, 1f)] public float hieloResistance = 0f;
        [Range(0f, 1f)] public float electricoResistance = 0f;
        [Range(0f, 1f)] public float perforanteResistance = 0f;
        
        [Header("Estados")]
        public bool canTakeKnockback = true;
        public float stunDuration = 0.5f;
        
        private int currentHealth;
        private bool isStunned = false;
        private float stunTimer = 0f;
        private Rigidbody rb;
        
        // Eventos para efectos visuales/sonoros
        public System.Action<DamageInfo> OnDamageTaken;
        public System.Action<int> OnHealthChanged;
        public System.Action OnDeath;

        
        
        void Awake()
        {
            currentHealth = maxHealth;
            rb = GetComponent<Rigidbody>();
        }
        
        void Update()
        {
            if (isStunned)
            {
                stunTimer -= Time.deltaTime;
                if (stunTimer <= 0f)
                {
                    isStunned = false;
                    if (showDebugLogs) Debug.Log($"[{name}] Stun terminado");
                }
            }
        }
        
        public void TakeDamage(DamageInfo damageInfo)
        {
            if (currentHealth <= 0) return; // Ya está muerto
            
            // Calcular resistencia
            float resistance = GetResistanceFor(damageInfo.damageType);
            float finalDamage = damageInfo.finalDamage * (1f - resistance);
            
            // Aplicar daño
            int damage = Mathf.RoundToInt(finalDamage);
            currentHealth = Mathf.Max(0, currentHealth - damage);
            
            if (showDebugLogs)
            {
                Debug.Log($"[{name}] Recibió {damage} daño ({damageInfo.damageType}) del combo paso {damageInfo.comboStep + 1}. " +
                         $"Vida: {currentHealth}/{maxHealth}. Efectos: {damageInfo.effects}");
            }
            
            // Aplicar efectos
            ApplyEffects(damageInfo);
            
            // Disparar eventos
            OnDamageTaken?.Invoke(damageInfo);
            OnHealthChanged?.Invoke(currentHealth);
            
            // Verificar muerte
            if (currentHealth <= 0)
            {
                Die(damageInfo);
            }
        }
        
        private float GetResistanceFor(DamageType damageType)
        {
            return damageType switch
            {
                DamageType.Normal => normalResistance,
                DamageType.Crítico => criticoResistance,
                DamageType.Fuego => fuegoResistance,
                DamageType.Hielo => hieloResistance,
                DamageType.Eléctrico => electricoResistance,
                DamageType.Perforante => perforanteResistance,
                _ => 0f
            };
        }
        
        private void ApplyEffects(DamageInfo damageInfo)
        {
            // Knockback
            if (damageInfo.effects.HasFlag(DamageEffects.Knockback) && canTakeKnockback && rb != null)
            {
                Vector3 knockbackDir = damageInfo.knockbackDirection.normalized;
                if (knockbackDir == Vector3.zero)
                    knockbackDir = (transform.position - damageInfo.attacker.position).normalized;
                // Proyectar knockback solo en XZ
                knockbackDir.y = 0f;
                knockbackDir = knockbackDir.normalized;
                rb.AddForce(knockbackDir * damageInfo.knockbackForce, ForceMode.Impulse);
                
                if (showDebugLogs)
                    Debug.Log($"[{name}] Knockback aplicado: {knockbackDir} * {damageInfo.knockbackForce}");
            }
            
            // Stun
            if (damageInfo.effects.HasFlag(DamageEffects.Stun))
            {
                isStunned = true;
                stunTimer = stunDuration;
                if (showDebugLogs) Debug.Log($"[{name}] Aturdido por {stunDuration}s");
            }
            
            // Otros efectos se pueden implementar aquí
            if (damageInfo.effects.HasFlag(DamageEffects.Burn))
            {
                // Implementar daño por tiempo
                if (showDebugLogs) Debug.Log($"[{name}] ¡Ardiendo!");
            }
            
            if (damageInfo.effects.HasFlag(DamageEffects.Freeze))
            {
                // Implementar ralentización
                if (showDebugLogs) Debug.Log($"[{name}] ¡Congelado!");
            }
        }
        
        private void Die(DamageInfo finalDamage)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[{name}] Murió por {finalDamage.damageType} del combo paso {finalDamage.comboStep + 1}");
            }

            OnDeath?.Invoke();

            // Dropear items según el tipo de enemigo
            if (isElite)
            {
                ItemDropSystem.Instance.DropFromEliteEnemy(transform.position);
            }
            else
            {
                ItemDropSystem.Instance.DropFromNormalEnemy(transform.position);
            }

            Destroy(gameObject);
            
        }
        
        // Métodos de utilidad
        public bool IsAlive() => currentHealth > 0;
        public bool IsStunned() => isStunned;
        public float HealthPercentage() => (float)currentHealth / maxHealth;
        
        // Método para curar
        public void Heal(int amount)
        {
            if (currentHealth <= 0) return; // No curar si está muerto
            
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth);
            
            if (showDebugLogs)
                Debug.Log($"[{name}] Curado {amount}. Vida: {currentHealth}/{maxHealth}");
        }
        
        #if UNITY_EDITOR
        void OnValidate()
        {
            if (maxHealth < 1) maxHealth = 1;
        }
        #endif
    }
}