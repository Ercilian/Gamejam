using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public Transform target; // Reference to the car
    public Vector3 offset = new Vector3(-10f, 10f, -10f); // Camera position relative to the car
    public float smoothSpeed = 5f; // Smoothing speed

    void LateUpdate()
    {
        if (target == null) return;

        // Desired position based on the car + offset
        Vector3 desiredPosition = target.position + offset;

        // Smooth interpolation
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Keep the camera looking at the car
        transform.LookAt(target);
    }
}
