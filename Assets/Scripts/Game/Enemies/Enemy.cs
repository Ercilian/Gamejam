using UnityEngine;
using Game.Combat;

namespace Game.Enemies
{
    public class Enemy : EntityStats
    {
        
        public bool showDebugLogs = true;

        [Header("Estados")]
        public bool canTakeKnockback = true;
        public float stunDuration = 0.5f;

        private bool isStunned = false;
        private float stunTimer = 0f;
        private Rigidbody rb;

        // Eventos para efectos visuales/sonoros
        //public System.Action<DamageInfo> OnDamageTaken;
        public System.Action<int> OnHealthChanged;
        public System.Action OnDeath;



        protected override void Awake()
        {
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

          public void TakeDamage()
          {

          }
                  


        // Métodos de utilidad
        public bool IsStunned() => isStunned;



#if UNITY_EDITOR
        void OnValidate()
        {
            if (maxHP < 1) maxHP = 1;
        }
#endif
    }
}

