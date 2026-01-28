
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
// Script to control camera movement and positioning, targeting a specific object (Car).

    public Transform target;
    [Header("Modo Normal")]
    public float offsetXNormal = -2f;
    public float offsetYNormal = 0f;
    public float offsetZNormal = 0f;

    [Header("Modo Pasillo")]
    public float offsetXPasillo = 0f;
    public float offsetYPasillo = 0f;
    public float offsetZPasillo = 0f;

    [Header("Modo Boss Cinematic")]
    public float offsetXBossCinematic = 0f;
    public float offsetYBossCinematic = 0f;
    public float offsetZBossCinematic = 0f;

    [Header("Ajustes Cinemática Boss")]
    public float offsetPitchBossCinematic = 5f;
    
    [Header("Modo Boss Combat")]
    public float offsetXBossCombat = 0f;
    public float offsetYBossCombat = 0f;
    public float offsetZBossCombat = 0f;

    
    
    // ...existing code...
    private float offsetX; // Offset actual usado internamente
    public float smoothSpeed = 5f;

    public enum CameraMode { Normal, Pasillo, BossCinematic, BossCombat }
    public CameraMode currentMode = CameraMode.Normal;

    // Freeze flags y valores específicos
    private bool freezeY = true;
    private bool freezeZ = true;
    private float fixedYValue;
    private float fixedZValue;

    private float currentYaw = 8f;
    public float yawSmoothSpeed = 5f;

    void Start()
    {
        offsetX = offsetXNormal;
        fixedYValue = target != null ? target.position.y + offsetYNormal : 0f;
        fixedZValue = target != null ? target.position.z + offsetZNormal : 0f;
        currentYaw = 8f;
    }

    void LateUpdate()
    {
        switch (currentMode)
        {
            case CameraMode.Normal:
                offsetX = offsetXNormal;
                float offsetYNorm = offsetYNormal;
                float offsetZNorm = offsetZNormal;
                Vector3 pos = transform.position;
                float targetX = target.position.x + offsetX;
                float targetY = freezeY ? fixedYValue : (target.position.y + offsetYNormal);
                float targetZ = freezeZ ? fixedZValue : (target.position.z + offsetZNormal);
                Vector3 desiredPosition = new Vector3(targetX, targetY, targetZ);
                transform.position = Vector3.Lerp(pos, desiredPosition, smoothSpeed * Time.deltaTime);

                float targetYaw = 8f;
                currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, yawSmoothSpeed * Time.deltaTime);
                float dx = target.position.x - transform.position.x;
                float dy = target.position.y - transform.position.y;
                float dz = target.position.z - transform.position.z;
                float distance = Mathf.Sqrt(dx * dx + dz * dz);
                float pitch = -Mathf.Atan2(dy, distance) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(pitch, currentYaw, 0f);
                break;
            case CameraMode.Pasillo:
                offsetX = offsetXPasillo;
                float offsetY = offsetYPasillo;
                float offsetZ = offsetZPasillo;
                Vector3 desiredPosPasillo = target.position + new Vector3(offsetX, offsetY, offsetZ);
                transform.position = Vector3.Lerp(transform.position, desiredPosPasillo, smoothSpeed * Time.deltaTime);
                transform.LookAt(target);
                break;
            case CameraMode.BossCinematic:
                offsetX = offsetXBossCinematic;
                float offsetYBossCine = offsetYBossCinematic;
                float offsetZBossCine = offsetZBossCinematic;
                Vector3 desiredPosBossCine = target.position + new Vector3(offsetX, offsetYBossCine, offsetZBossCine);
                transform.position = Vector3.Lerp(transform.position, desiredPosBossCine, smoothSpeed * Time.deltaTime);
                // Calcular dirección hacia el target
                Vector3 dir = target.position - transform.position;
                float bossCineDistance = Mathf.Sqrt(dir.x * dir.x + dir.z * dir.z);
                float bossCinePitch = -Mathf.Atan2(dir.y, bossCineDistance) * Mathf.Rad2Deg + offsetPitchBossCinematic;
                float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(bossCinePitch, yaw, 0f);
                break;
            case CameraMode.BossCombat:
                offsetX = offsetXBossCombat;
                Vector3 desiredPosBossCombat = target.position + new Vector3(offsetX, 0, 0);
                transform.position = Vector3.Lerp(transform.position, desiredPosBossCombat, smoothSpeed * Time.deltaTime);
                transform.LookAt(target);
                break;
        }
    }

    // Cambiar el modo de cámara
    public void SetCameraMode(CameraMode mode, float transitionTime = 0.5f)
    {
        // Guardar el modo anterior
        CameraMode previousMode = currentMode;
        currentMode = mode;
        if (mode == CameraMode.Normal && previousMode != CameraMode.Normal)
        {
            // Al cambiar a normal desde otro modo, hacer transición suave
            // Inicializar currentYaw desde la rotación actual de la cámara
            currentYaw = transform.eulerAngles.y;
            TransitionPasilloToNormal(transitionTime);
        }
        else if (mode == CameraMode.Normal)
        {
            // Si ya está en normal, solo fijar freeze
            freezeY = true;
            freezeZ = true;
            if (target != null)
            {
                fixedYValue = target.position.y + offsetYNormal;
                fixedZValue = target.position.z + offsetZNormal;
            }
            // Inicializar currentYaw desde la rotación actual de la cámara
            currentYaw = transform.eulerAngles.y;
        }
        else
        {
            // En otros modos, desactivar freeze
            freezeY = false;
            freezeZ = false;
        }
    }

    // Llamar esto para transición suave desde pasillo a normal
    public void TransitionPasilloToNormal(float transitionTime)
    {
        StartCoroutine(TransitionFreezeCoroutine(transitionTime));
    }

    private System.Collections.IEnumerator TransitionFreezeCoroutine(float transitionTime)
    {
        // Desbloquear freeze durante la transición
        freezeY = false;
        freezeZ = false;
        float elapsed = 0f;
        float threshold = 0.05f; // Distancia mínima para considerar "llegado"
        while (elapsed < transitionTime)
        {
            float targetY = target.position.y + offsetYNormal;
            float targetZ = target.position.z + offsetZNormal;
            float currentY = transform.position.y;
            float currentZ = transform.position.z;
            if (Mathf.Abs(currentY - targetY) < threshold && Mathf.Abs(currentZ - targetZ) < threshold)
            {
                break; // Ya está suficientemente cerca
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Al terminar, fijar freeze en la posición actual de la cámara
        fixedYValue = transform.position.y;
        fixedZValue = transform.position.z;
        freezeY = true;
        freezeZ = true;
    }

    // Transición de offset con delay (puede usarse para cinemáticas)
    public void ChangeOffsetWithDelay(float newOffsetX, float delaySeconds)
    {
        StartCoroutine(ChangeOffsetCoroutine(newOffsetX, delaySeconds)); // Esto solo afecta el offsetX actual, útil para transiciones puntuales
    }

    private System.Collections.IEnumerator ChangeOffsetCoroutine(float newOffsetX, float delaySeconds)
    {
        smoothSpeed = 2; // Aumenta la velocidad de suavizado temporalmente
        yield return new WaitForSeconds(delaySeconds);
        offsetX = newOffsetX;
    }
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
