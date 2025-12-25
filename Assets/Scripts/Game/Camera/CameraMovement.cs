using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(-10f, 10f, -10f);
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        transform.LookAt(target);
    }

    public void ChangeOffsetWithDelay(Vector3 newOffset, float delaySeconds)
    {
        StartCoroutine(ChangeOffsetCoroutine(newOffset, delaySeconds));
    }

    private System.Collections.IEnumerator ChangeOffsetCoroutine(Vector3 newOffset, float delaySeconds)
    {
        smoothSpeed = 2; // Aumenta la velocidad de suavizado temporalmente
        yield return new WaitForSeconds(delaySeconds);
        offset = newOffset;
    }
}
