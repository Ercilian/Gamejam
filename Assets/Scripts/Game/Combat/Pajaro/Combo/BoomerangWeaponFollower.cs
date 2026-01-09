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
                
                // Guardar estado original
                originalParent = weaponObject.transform.parent;
                originalLocalPosition = weaponObject.transform.localPosition;
                originalLocalRotation = weaponObject.transform.localRotation;
                originalLocalScale = weaponObject.transform.localScale;
                
                if (showDebugLogs)
                    Debug.Log($"[BoomerangWeaponFollower] Estado original guardado: Pos={originalLocalPosition}, Rot={originalLocalRotation.eulerAngles}");
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
            
            if (showDebugLogs)
                Debug.Log("[BoomerangWeaponFollower] Boomerang iniciado - creando clon del arma");
            
            // Desactivar animator del arma original
            if (weaponAnimator != null)
                weaponAnimator.enabled = false;
            
            // Crear clon del arma
            clonedWeapon = Instantiate(weaponObject);
            clonedWeapon.name = weaponObject.name + "_Clone";
            clonedWeapon.transform.position = weaponObject.transform.position;
            clonedWeapon.transform.rotation = weaponObject.transform.rotation;
            clonedWeapon.transform.localScale = weaponObject.transform.lossyScale;
            
            clonedAnimator = clonedWeapon.GetComponent<Animator>();
            if (clonedAnimator == null)
                clonedAnimator = clonedWeapon.GetComponentInChildren<Animator>();
            
            // Desactivar el arma original
            weaponObject.SetActive(false);
        }
        
        void OnBoomerangTick(Vector3 position, Quaternion rotation, int step)
        {
            if (!isFollowingBoomerang || clonedWeapon == null) return;
            
            targetPosition = position + rotation * positionOffset;
            targetRotation = rotation;
            
            if (!smoothMovement)
            {
                clonedWeapon.transform.position = targetPosition;
                clonedWeapon.transform.rotation = targetRotation;
            }
        }
        
        void OnBoomerangEnded()
        {
            isFollowingBoomerang = false;
            
            // Destruir el clon
            if (clonedWeapon != null)
            {
                DestroyImmediate(clonedWeapon);
                clonedWeapon = null;
                clonedAnimator = null;
            }
            
            // Restaurar el arma original
            if (weaponObject != null)
            {
                weaponObject.SetActive(true);
                weaponObject.transform.localPosition = originalLocalPosition;
                weaponObject.transform.localRotation = originalLocalRotation;
                weaponObject.transform.localScale = originalLocalScale;
                
                if (weaponAnimator != null)
                    weaponAnimator.enabled = true;
                
                if (showDebugLogs)
                    Debug.Log("[BoomerangWeaponFollower] Arma restaurada a su posición original");
            }
        }
        void Update()
        {
            if (!isFollowingBoomerang || !smoothMovement || clonedWeapon == null) return;
            
            clonedWeapon.transform.position = Vector3.Lerp(
                clonedWeapon.transform.position,
                targetPosition,
                Time.deltaTime * lerpSpeed
            );
            
            clonedWeapon.transform.rotation = Quaternion.Slerp(
                clonedWeapon.transform.rotation,
                targetRotation,
                Time.deltaTime * lerpSpeed
            );
        }
    }
}