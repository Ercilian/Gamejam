using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// El arma sigue la trayectoria del boomerang.
    /// La animación loopea mientras el boomerang está activo.
    /// </summary>
    public class BoomerangWeaponFollower : MonoBehaviour
    {
        [SerializeField] private GameObject weaponObject;
        [SerializeField] private ComboHitboxController comboController;
        [SerializeField] private Vector3 positionOffset = Vector3.zero;

        private bool isBoomerangActive;
        private Vector3 weaponOriginalPosition;
        private Quaternion weaponOriginalRotation;
        private Animator weaponAnimator;

        private void Start()
        {
            if (comboController == null)
                comboController = GetComponent<ComboHitboxController>();

            if (comboController == null)
            {
                Debug.LogError("[BoomerangWeaponFollower] ComboHitboxController no encontrado");
                return;
            }

            if (weaponObject != null)
            {
                weaponAnimator = weaponObject.GetComponent<Animator>();
                if (weaponAnimator == null)
                    weaponAnimator = weaponObject.GetComponentInChildren<Animator>();

                weaponOriginalPosition = weaponObject.transform.position;
                weaponOriginalRotation = weaponObject.transform.rotation;
            }

            comboController.OnBoomerangStarted += HandleBoomerangStart;
            comboController.OnBoomerangTick += HandleBoomerangTick;
            comboController.OnBoomerangEnded += HandleBoomerangEnd;
        }

        private void OnDestroy()
        {
            if (comboController != null)
            {
                comboController.OnBoomerangStarted -= HandleBoomerangStart;
                comboController.OnBoomerangTick -= HandleBoomerangTick;
                comboController.OnBoomerangEnded -= HandleBoomerangEnd;
            }
        }

        private void HandleBoomerangStart()
        {
            if (weaponObject == null)
                return;

            isBoomerangActive = true;
            weaponOriginalPosition = weaponObject.transform.position;
            weaponOriginalRotation = weaponObject.transform.rotation;

            // Activar animación en loop
            if (weaponAnimator != null)
            {
                weaponAnimator.SetBool("IsBoomerangActive", true);
            }
        }

        private void HandleBoomerangTick(Vector3 position, Quaternion rotation, int step)
        {
            if (!isBoomerangActive || weaponObject == null)
                return;

            // Mover el arma a la posición del boomerang
            weaponObject.transform.position = position + positionOffset;
            weaponObject.transform.rotation = rotation;
        }

        private void HandleBoomerangEnd()
        {
            if (!isBoomerangActive || weaponObject == null)
                return;

            isBoomerangActive = false;

            // Detener animación
            if (weaponAnimator != null)
            {
                weaponAnimator.SetBool("IsBoomerangActive", false);
            }

            // Restaurar posición original
            weaponObject.transform.position = weaponOriginalPosition;
            weaponObject.transform.rotation = weaponOriginalRotation;
        }
    }
}