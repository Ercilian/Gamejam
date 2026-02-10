using UnityEngine;
using Game.Combat;

namespace Game.Enemies
{
    public class Enemy : EntityStats
    {
        
        public bool showDebugLogs = true;
        public bool isElite = false;

        [Header("Estados")]
        public bool canTakeKnockback = true;
        public float stunDuration = 0.5f;
        
        [Header("Outliner Damage Feedback")]
        [Tooltip("Material del outliner del enemigo")]
        public Material outlinerMaterial;
        [Tooltip("Color del outliner cuando recibe daño")]
        [ColorUsage(true, true)]
        public Color damageOutlinerColor = Color.red;
        [Tooltip("Duración del cambio de color del outliner")]
        public float outlinerFlashDuration = 0.15f;
        [Tooltip("Nombre de la propiedad de color en el shader")]
        public string colorPropertyName = "_Color";
        
        private Color originalOutlinerColor;
        private bool isFlashingOutliner = false;
        private Material outlinerMaterialInstance;

        private bool isStunned = false;
        private float stunTimer = 0f;
        private Rigidbody rb;
        private AudioSource audioSource;

        // Eventos para efectos visuales/sonoros
        //public System.Action<DamageInfo> OnDamageTaken;
        public System.Action<int> OnHealthChanged;
        public System.Action OnDeath;



        protected override void Awake()
        {
            base.Awake(); // Llamar al Awake de EntityStats para aplicar el ScriptableObject
            rb = GetComponent<Rigidbody>();
            audioSource = GetComponent<AudioSource>();
            
            // Crear instancia única del material del outliner para este enemigo
            if (outlinerMaterial != null)
            {
                outlinerMaterialInstance = new Material(outlinerMaterial);
                
                // Buscar el renderer que usa este material y reemplazarlo con la instancia
                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    Material[] materials = renderer.materials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (materials[i].shader == outlinerMaterial.shader)
                        {
                            materials[i] = outlinerMaterialInstance;
                        }
                    }
                    renderer.materials = materials;
                }
                
                // Guardar el color original
                if (outlinerMaterialInstance.HasProperty(colorPropertyName))
                {
                    originalOutlinerColor = outlinerMaterialInstance.GetColor(colorPropertyName);
                }
            }
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
          
        public override void TakeDamage(int amount)
        {
            base.TakeDamage(amount);
            
            // Aplicar stun al recibir daño
            isStunned = true;
            stunTimer = stunDuration;
            if (showDebugLogs) Debug.Log($"[{name}] Stunned por {stunDuration} segundos");
            
            // Flash del outliner cuando recibe daño
            if (outlinerMaterialInstance != null && !isFlashingOutliner)
            {
                StartCoroutine(FlashOutliner());
            }
        }
        
        private System.Collections.IEnumerator FlashOutliner()
        {
            isFlashingOutliner = true;
            
            // Cambiar a color de daño
            if (outlinerMaterialInstance.HasProperty(colorPropertyName))
            {
                outlinerMaterialInstance.SetColor(colorPropertyName, damageOutlinerColor);
            }
            
            // Esperar
            yield return new WaitForSeconds(outlinerFlashDuration);
            
            // Restaurar color original
            if (outlinerMaterialInstance.HasProperty(colorPropertyName))
            {
                outlinerMaterialInstance.SetColor(colorPropertyName, originalOutlinerColor);
            }
            
            isFlashingOutliner = false;
        }
                  
        public override void OnEntityDeath()
        {
            
            if (isElite)
            {
                ItemDropSystem.Instance.DropFromEliteEnemy(transform.position);
            }
            else
            {
                ItemDropSystem.Instance.DropFromNormalEnemy(transform.position);
            }
            OnDeath?.Invoke();
            Destroy(gameObject);
        }

        // Métodos de utilidad
        public bool IsStunned() => isStunned;

    }
}

