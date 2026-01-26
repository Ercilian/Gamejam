using UnityEngine;
using System.Collections;

/// <summary>
/// Componente que anima un proyectil de mortero de forma independiente.
/// Se añade dinámicamente al proyectil para que continúe su animación aunque el enemigo muera.
/// </summary>
public class MortarProjectileAnimator : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 endPos;
    private float duration;
    private float maxHeight;
    private bool isInitialized = false;

    public void Initialize(Vector3 start, Vector3 end, float animDuration, float arcHeight)
    {
        startPos = start;
        endPos = end;
        duration = animDuration;
        maxHeight = arcHeight;
        isInitialized = true;
        
        // Iniciar la animación
        StartCoroutine(AnimateProjectile());
    }

    private IEnumerator AnimateProjectile()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[MortarProjectileAnimator] No se inicializó correctamente");
            Destroy(gameObject);
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;

            // Interpolación horizontal
            Vector3 horizontalPos = Vector3.Lerp(startPos, endPos, t);

            // Parábola vertical (sube y baja)
            float arcHeight = Mathf.Sin(t * Mathf.PI) * maxHeight;

            transform.position = horizontalPos + Vector3.up * arcHeight;

            // Rotar hacia la dirección de movimiento
            Vector3 direction = (endPos - startPos).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Asegurar que llegue a la posición final
        transform.position = endPos;

        // Destruir el proyectil al terminar la trayectoria
        Destroy(gameObject);
    }
}
