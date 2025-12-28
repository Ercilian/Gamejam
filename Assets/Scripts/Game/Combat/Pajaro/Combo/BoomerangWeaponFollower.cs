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
        
        [Header("Configuración")]
        [Tooltip("Si está activo, crea una copia del arma para el boomerang")]
        public bool cloneWeapon = true;
        
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
        
        // Clon del arma
        private GameObject clonedWeapon;
        private Animator weaponAnimator;
        private Animator clonedAnimator;
        
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
                
                // NO guardar el estado aquí, se guarda en OnBoomerangStarted justo antes de lanzar
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
        }
        
        void OnBoomerangStarted()
        {
            if (weaponObject == null) return;
            
            isFollowingBoomerang = true;
            
            if (cloneWeapon)
            {
                // CRÍTICO: Guardar el estado JUSTO ANTES de lanzar
                originalParent = weaponObject.transform.parent;
                originalLocalPosition = weaponObject.transform.localPosition;
                originalLocalRotation = weaponObject.transform.localRotation;
                originalLocalScale = weaponObject.transform.localScale;
                
                if (showDebugLogs)
                    Debug.Log($"[BoomerangWeaponFollower] Estado guardado ANTES de lanzar: Parent={originalParent?.name}, Pos={originalLocalPosition}, Rot={originalLocalRotation.eulerAngles}, Scale={originalLocalScale}");
                
                // Desactivar animator del arma original
                if (weaponAnimator != null)
                    weaponAnimator.enabled = false;
                
                // Crear clon
                clonedWeapon = Instantiate(weaponObject, weaponObject.transform.position, weaponObject.transform.rotation);
                clonedWeapon.name = weaponObject.name + "_Clone";
                
                clonedAnimator = clonedWeapon.GetComponent<Animator>();
                if (clonedAnimator == null)
                    clonedAnimator = clonedWeapon.GetComponentInChildren<Animator>();
                
                weaponObject.SetActive(false);
                
                if (showDebugLogs)
                    Debug.Log($"[BoomerangWeaponFollower] Clon creado, arma original oculta");
            }
            else
            {
                originalParent = weaponObject.transform.parent;
                originalLocalPosition = weaponObject.transform.localPosition;
                originalLocalRotation = weaponObject.transform.localRotation;
                originalLocalScale = weaponObject.transform.localScale;
                
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
                GameObject activeWeapon = cloneWeapon ? clonedWeapon : weaponObject;
                if (activeWeapon != null)
                {
                    activeWeapon.transform.position = targetPosition;
                    activeWeapon.transform.rotation = targetRotation;
                }
            }
        }
        
        void OnBoomerangEnded()
        {
            isFollowingBoomerang = false;
            
            if (cloneWeapon)
            {
                // Destruir el clon INMEDIATAMENTE
                if (clonedWeapon != null)
                {
                    DestroyImmediate(clonedWeapon);
                    clonedWeapon = null;
                    clonedAnimator = null;
                    
                    if (showDebugLogs)
                        Debug.Log("[BoomerangWeaponFollower] Clon destruido inmediatamente");
                }
                
                if (weaponObject != null && originalParent != null)
                {
                    // Activar primero
                    weaponObject.SetActive(true);
                    
                    // Restaurar jerarquía y transform
                    weaponObject.transform.SetParent(originalParent, false);
                    weaponObject.transform.localPosition = originalLocalPosition;
                    weaponObject.transform.localRotation = originalLocalRotation;
                    weaponObject.transform.localScale = originalLocalScale;
                    
                    // Reactivar el Animator del arma original
                    if (weaponAnimator != null)
                        weaponAnimator.enabled = true;
                    
                    targetPosition = weaponObject.transform.position;
                    targetRotation = weaponObject.transform.rotation;
                    
                    if (showDebugLogs)
                        Debug.Log($"[BoomerangWeaponFollower] Arma restaurada: Parent={originalParent.name}, LocalPos={weaponObject.transform.localPosition}, LocalRot={weaponObject.transform.localRotation.eulerAngles}, LocalScale={weaponObject.transform.localScale}");
                }
            }
            else
            {
                if (weaponObject != null && originalParent != null)
                {
                    weaponObject.transform.SetParent(originalParent, false);
                    weaponObject.transform.localPosition = originalLocalPosition;
                    weaponObject.transform.localRotation = originalLocalRotation;
                    weaponObject.transform.localScale = originalLocalScale;
                    
                    targetPosition = weaponObject.transform.position;
                    targetRotation = weaponObject.transform.rotation;
                    
                    if (weaponAnimator != null)
                        weaponAnimator.enabled = true;
                }
            }
        }
        
        void Update()
        {
            if (!isFollowingBoomerang || !smoothMovement) return;
            
            GameObject activeWeapon = cloneWeapon ? clonedWeapon : weaponObject;
            if (activeWeapon != null)
            {
                activeWeapon.transform.position = Vector3.Lerp(
                    activeWeapon.transform.position,
                    targetPosition,
                    Time.deltaTime * lerpSpeed
                );
                
                activeWeapon.transform.rotation = Quaternion.Slerp(
                    activeWeapon.transform.rotation,
                    targetRotation,
                    Time.deltaTime * lerpSpeed
                );
            }
        }
    }
}
