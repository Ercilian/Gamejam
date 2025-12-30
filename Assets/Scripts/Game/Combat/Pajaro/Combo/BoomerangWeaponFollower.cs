using UnityEngine;
using Game.Combat;

namespace Game.Combat
{
    /// <summary>
    /// Hace que el arma siga la hitbox del boomerang durante el ataque.
    /// </summary>
    public class BoomerangWeaponFollower : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("El GameObject del arma que seguirá el boomerang")]
        public GameObject weaponObject;
        
        [Tooltip("Referencia al ComboHitboxController del jugador")]
        public ComboHitboxController comboController;
        
        [Header("Posición Manual (OPCIONAL)")]
        [Tooltip("Si está activado, usa los valores manuales en lugar de detectar automáticamente")]
        public bool useManualPosition = false;
        
        [Tooltip("Posición local manual del arma cuando está en la mano")]
        public Vector3 manualLocalPosition = Vector3.zero;
        
        [Tooltip("Rotación local manual del arma cuando está en la mano")]
        public Vector3 manualLocalRotation = Vector3.zero;
        
        [Tooltip("Escala local manual del arma cuando está en la mano")]
        public Vector3 manualLocalScale = Vector3.one;
        
        [Header("Configuración")]
        [Tooltip("Si está activo, crea una copia del arma para el boomerang")]
        public bool cloneWeapon = true; // True por defecto - necesario para la animación
        
        [Tooltip("Suavizar el movimiento del arma")]
        public bool smoothMovement = false;
        
        [Tooltip("Velocidad de interpolación si smoothMovement está activo")]
        public float lerpSpeed = 20f;
        
        [Tooltip("Offset adicional de posición respecto a la hitbox")]
        public Vector3 positionOffset = Vector3.zero;
        
        [Header("Debug")]
        public bool showDebugLogs = false;
        
        // Estado interno
        private bool isFollowingBoomerang = false;
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        
        // Estado original del arma
        private Transform originalParent;
        private Vector3 originalLocalPosition;
        private Quaternion originalLocalRotation;
        private Vector3 originalLocalScale;
        private Vector3 originalWorldPosition;
        private Quaternion originalWorldRotation;
        
        // Clon del arma y wrapper
        private GameObject clonedWeapon;
        private GameObject weaponWrapper; // Contenedor para manejar el pivote correctamente
        private Animator weaponAnimator;
        private Animator clonedAnimator;
        private bool isRestoring = false; // Flag para forzar posición en LateUpdate
        
        void Start()
        {
            if (comboController == null)
                comboController = GetComponent<ComboHitboxController>();
            
            if (comboController != null)
            {
                comboController.OnBoomerangStarted += OnBoomerangStarted;
                comboController.OnBoomerangTick += OnBoomerangTick;
                comboController.OnBoomerangEnded += OnBoomerangEnded;
            }
            else
            {
                Debug.LogError("[BoomerangWeaponFollower] No se encontró ComboHitboxController");
            }
            
            if (weaponObject != null)
            {
                weaponAnimator = weaponObject.GetComponent<Animator>();
                if (weaponAnimator == null)
                    weaponAnimator = weaponObject.GetComponentInChildren<Animator>();
                
                // Guardar posición de referencia inicial
                StartCoroutine(SaveInitialPosition());
            }
        }
        
        System.Collections.IEnumerator SaveInitialPosition()
        {
            // Esperar 2 frames para que todo se inicialice
            yield return null;
            yield return null;
            
            if (weaponObject != null)
            {
                originalParent = weaponObject.transform.parent;
                
                // Usar valores manuales si están activados
                if (useManualPosition)
                {
                    originalLocalPosition = manualLocalPosition;
                    originalLocalRotation = Quaternion.Euler(manualLocalRotation);
                    originalLocalScale = manualLocalScale;
                    
                    Debug.Log($"[BoomerangWeaponFollower] Usando posición MANUAL como referencia:\n" +
                             $"LocalPos={originalLocalPosition}\n" +
                             $"LocalRot={manualLocalRotation}\n" +
                             $"LocalScale={originalLocalScale}");
                }
                else
                {
                    originalLocalPosition = weaponObject.transform.localPosition;
                    originalLocalRotation = weaponObject.transform.localRotation;
                    originalLocalScale = weaponObject.transform.localScale;
                    
                    Debug.Log($"[BoomerangWeaponFollower] Posición AUTOMÁTICA guardada en Start:\n" +
                             $"Parent={originalParent?.name}\n" +
                             $"LocalPos={originalLocalPosition}\n" +
                             $"LocalRot={originalLocalRotation.eulerAngles}\n" +
                             $"LocalScale={originalLocalScale}");
                }
            }
        }
        
        void OnDestroy()
        {
            if (comboController != null)
            {
                comboController.OnBoomerangStarted -= OnBoomerangStarted;
                comboController.OnBoomerangTick -= OnBoomerangTick;
                comboController.OnBoomerangEnded -= OnBoomerangEnded;
            }
            
            if (clonedWeapon != null)
                Destroy(clonedWeapon);
            
            if (weaponWrapper != null)
                Destroy(weaponWrapper);
        }
        
        void OnBoomerangStarted()
        {
            if (weaponObject == null) return;
            
            isFollowingBoomerang = true;
            
            // NO guardar aquí - usamos la posición de referencia guardada en Start
            Debug.Log($"[BoomerangWeaponFollower] BOOMERANG INICIADO\n" +
                     $"Posición ACTUAL del arma: LocalPos={weaponObject.transform.localPosition}\n" +
                     $"Posición de REFERENCIA (la que restauraremos): LocalPos={originalLocalPosition}\n" +
                     $"¿Son diferentes? {Vector3.Distance(weaponObject.transform.localPosition, originalLocalPosition) > 0.01f}");
            
            // Desactivar animator SIEMPRE para evitar que interfiera
            if (weaponAnimator != null)
                weaponAnimator.enabled = false;
            
            if (cloneWeapon)
            {
                // MODO CLON: Crear wrapper y clon
                weaponWrapper = new GameObject("WeaponWrapper_Temp");
                if (originalParent != null)
                {
                    weaponWrapper.transform.position = originalParent.position;
                    weaponWrapper.transform.rotation = originalParent.rotation;
                }
                else
                {
                    weaponWrapper.transform.position = originalWorldPosition;
                    weaponWrapper.transform.rotation = originalWorldRotation;
                }
                
                clonedWeapon = Instantiate(weaponObject);
                clonedWeapon.name = weaponObject.name + "_Clone";
                clonedWeapon.transform.SetParent(weaponWrapper.transform, false);
                clonedWeapon.transform.localPosition = originalLocalPosition;
                clonedWeapon.transform.localRotation = originalLocalRotation;
                clonedWeapon.transform.localScale = originalLocalScale;
                
                clonedAnimator = clonedWeapon.GetComponent<Animator>();
                if (clonedAnimator == null)
                    clonedAnimator = clonedWeapon.GetComponentInChildren<Animator>();
                
                weaponObject.SetActive(false);
            }
            else
            {
                // MODO DIRECTO: Mover el arma original
                weaponObject.transform.SetParent(null);
            }
        }
        
        void OnBoomerangTick(Vector3 position, Quaternion rotation, int step)
        {
            if (!isFollowingBoomerang) return;
            
            targetPosition = position + rotation * positionOffset;
            targetRotation = rotation;
            
            if (!smoothMovement)
            {
                if (cloneWeapon && weaponWrapper != null)
                {
                    // Mover el wrapper, no el clon directamente
                    weaponWrapper.transform.position = targetPosition;
                    weaponWrapper.transform.rotation = targetRotation;
                }
                else if (!cloneWeapon && weaponObject != null)
                {
                    weaponObject.transform.position = targetPosition;
                    weaponObject.transform.rotation = targetRotation;
                }
            }
        }
        
        void OnBoomerangEnded()
        {
            isFollowingBoomerang = false;
            
            if (cloneWeapon)
            {
                // MODO CLON: Destruir wrapper/clon y reactivar original
                if (weaponWrapper != null)
                {
                    DestroyImmediate(weaponWrapper);
                    weaponWrapper = null;
                    clonedWeapon = null;
                    clonedAnimator = null;
                }
                
                // Iniciar restauración forzada
                StartCoroutine(ForceRestoreWeapon());
            }
            else
            {
                // MODO DIRECTO: Reparentar el arma original
                if (weaponObject != null && originalParent != null)
                {
                    weaponObject.transform.SetParent(originalParent, false);
                    weaponObject.transform.localPosition = originalLocalPosition;
                    weaponObject.transform.localRotation = originalLocalRotation;
                    weaponObject.transform.localScale = originalLocalScale;
                    
                    if (weaponAnimator != null)
                        weaponAnimator.enabled = true;
                }
            }
        }
        
        System.Collections.IEnumerator ForceRestoreWeapon()
        {
            if (weaponObject == null) yield break;
            
            isRestoring = true;
            
            Debug.Log($"[BoomerangWeaponFollower] INICIO RESTAURACIÓN\n" +
                     $"Restaurando a: LocalPos={originalLocalPosition}, LocalRot={originalLocalRotation.eulerAngles}\n" +
                     $"Modo: {(useManualPosition ? "MANUAL" : "AUTOMÁTICO")}");
            
            // FRAME 0: Forzar posición mientras está desactivado
            weaponObject.transform.localPosition = originalLocalPosition;
            weaponObject.transform.localRotation = originalLocalRotation;
            weaponObject.transform.localScale = originalLocalScale;
            
            // Activar el arma
            weaponObject.SetActive(true);
            
            Debug.Log($"[BoomerangWeaponFollower] Arma activada - LocalPos actual: {weaponObject.transform.localPosition} vs esperado: {originalLocalPosition}");
            
            // FRAME 1: Esperar y forzar de nuevo
            yield return null;
            
            weaponObject.transform.localPosition = originalLocalPosition;
            weaponObject.transform.localRotation = originalLocalRotation;
            weaponObject.transform.localScale = originalLocalScale;
            
            // FRAME 2: Una vez más
            yield return null;
            
            weaponObject.transform.localPosition = originalLocalPosition;
            weaponObject.transform.localRotation = originalLocalRotation;
            weaponObject.transform.localScale = originalLocalScale;
            
            // Reactivar animator DESPUÉS de forzar posición
            if (weaponAnimator != null)
                weaponAnimator.enabled = true;
            
            Debug.Log($"[BoomerangWeaponFollower] FIN RESTAURACIÓN\n" +
                     $"LocalPos final: {weaponObject.transform.localPosition}\n" +
                     $"Diferencia: {Vector3.Distance(weaponObject.transform.localPosition, originalLocalPosition):F4}\n" +
                     $"WorldPos final: {weaponObject.transform.position}");
            
            // Mantener flag activo 5 frames más para LateUpdate
            for (int i = 0; i < 5; i++)
                yield return null;
            
            isRestoring = false;
        }
        void Update()
        {
            if (!isFollowingBoomerang || !smoothMovement) return;
            
            if (cloneWeapon && weaponWrapper != null)
            {
                // Mover el wrapper suavemente
                weaponWrapper.transform.position = Vector3.Lerp(
                    weaponWrapper.transform.position,
                    targetPosition,
                    Time.deltaTime * lerpSpeed
                );
                
                weaponWrapper.transform.rotation = Quaternion.Slerp(
                    weaponWrapper.transform.rotation,
                    targetRotation,
                    Time.deltaTime * lerpSpeed
                );
            }
            else if (!cloneWeapon && weaponObject != null)
            {
                weaponObject.transform.position = Vector3.Lerp(
                    weaponObject.transform.position,
                    targetPosition,
                    Time.deltaTime * lerpSpeed
                );
                
                weaponObject.transform.rotation = Quaternion.Slerp(
                    weaponObject.transform.rotation,
                    targetRotation,
                    Time.deltaTime * lerpSpeed
                );
            }
        }
        
        void LateUpdate()
        {
            // Durante la restauración, forzar posición después de todos los updates
            if (isRestoring && weaponObject != null)
            {
                weaponObject.transform.localPosition = originalLocalPosition;
                weaponObject.transform.localRotation = originalLocalRotation;
                weaponObject.transform.localScale = originalLocalScale;
            }
        }
    }
}