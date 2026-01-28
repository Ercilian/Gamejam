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

    [Header("Modo Boss Combat")]
    public float offsetXBossCombat = 0f;
    public float offsetYBossCombat = 0f;
    public float offsetZBossCombat = 0f;

    
    
    // ...existing code...
    private float offsetX; // Offset actual usado internamente
    public float smoothSpeed = 5f;

    public enum CameraMode { Normal, Pasillo, BossCinematic, BossCombat }
    public CameraMode currentMode = CameraMode.Normal;

    private float fixedY;
    private float fixedZ;

    void Start()
    {
        offsetX = offsetXNormal;
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
                float targetY = target.position.y + offsetYNorm;
                float targetZ = target.position.z + offsetZNorm;
                Vector3 desiredPosition = new Vector3(targetX, targetY, targetZ);
                transform.position = Vector3.Lerp(pos, desiredPosition, smoothSpeed * Time.deltaTime);

                float yaw = 8f; // Y fija
                float dx = target.position.x - transform.position.x;
                float dy = target.position.y - transform.position.y;
                float dz = target.position.z - transform.position.z;
                float distance = Mathf.Sqrt(dx * dx + dz * dz);
                float pitch = -Mathf.Atan2(dy, distance) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
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
                Vector3 desiredPosBossCine = target.position + new Vector3(offsetX, 0, 0);
                transform.position = Vector3.Lerp(transform.position, desiredPosBossCine, smoothSpeed * Time.deltaTime);
                transform.LookAt(target);
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
    public void SetCameraMode(CameraMode mode)
    {
        currentMode = mode;
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
