using Microsoft.VisualBasic;
using UnityEngine;

public class CameraTrigger : MonoBehaviour
// Script to change camera offset when the car enters the trigger zone (cinematic camera).
{
    public Vector3 newOffset = new Vector3(0f, 5f, -5f);
    public float delaySeconds = 2f;
    [SerializeField] private CameraMovement cameraMovement;
    public bool disableFuelConsumption = false;

    private void Awake() // Search for CameraMovement in the scene
    {
        cameraMovement = FindFirstObjectByType<CameraMovement>();
    }

    private void OnTriggerEnter(Collider other) // Detect when the car enters the trigger zone and change camera offset
    {
        if (other.CompareTag("Car") && cameraMovement != null)
        {
            cameraMovement.ChangeOffsetWithDelay(newOffset, delaySeconds);

            var enemies = GameObject.FindGameObjectsWithTag("Enemy"); // Destroy all enemies in the scene
            foreach (var enemy in enemies)
            {
                Destroy(enemy);
            }
            var collectibles = GameObject.FindGameObjectsWithTag("Collectible"); // Destroy all collectibles in the scene
            foreach (var collectible in collectibles)
            {
                Destroy(collectible);
            }

            var carFuelSystem = other.GetComponentInChildren<CarFuelSystem>(); // Disable fuel consumption if specified            
            carFuelSystem.SetFuelConsumptionEnabled(!disableFuelConsumption);
            
        }
    }
}
