using Microsoft.VisualBasic;
using UnityEngine;


public class CameraTrigger : MonoBehaviour
// Script to change camera offset when the car enters the trigger zone (cinematic camera).
{
    public Vector3 newOffset = new Vector3(0f, 5f, -5f);
    public float delaySeconds = 2f;
    [SerializeField] private CameraMovement cameraMovement;
    public bool disableFuelConsumption = false;
    [Header("Zona de pasillo (activa diálogo)")]
    public bool isPasillo = false;
    private EnemySpawner enemySpawner;
    private Player player;


    private void Awake() // Search for CameraMovement in the scene
    {
        cameraMovement = FindFirstObjectByType<CameraMovement>();
        enemySpawner = FindFirstObjectByType<EnemySpawner>();
        player = FindFirstObjectByType<Player>();
    }

    private void OnTriggerEnter(Collider other) // Detect when the car enters the trigger zone and change camera offset
    {
        if (other.CompareTag("Car"))
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



            if (isPasillo)
            {
                // Desactivar input de todos los jugadores
                var players = FindObjectsOfType<Player>();
                // Asignar offsets según la cantidad de jugadores
                Vector3[] offsets;
                if (players.Length == 1)
                {
                    offsets = new Vector3[] { new Vector3(4f, 0, 0f) }; // centro
                }
                else if (players.Length == 2)
                {
                    offsets = new Vector3[] { new Vector3(0f, 0, -4.5f), new Vector3(0f, 0, 8f) }; // izquierda y derecha
                }
                else // 3 o más
                {
                    offsets = new Vector3[] { new Vector3(4f, 0, 0f), new Vector3(0f, 0, -2f), new Vector3(0f, 0, 2f) }; // izquierda, centro, derecha
                }
                for (int i = 0; i < players.Length; i++)
                {
                    players[i].SetInputEnabled(false);
                    players[i].isCinematicRun = true;
                    Vector3 localOffset = (i < offsets.Length) ? offsets[i] : Vector3.zero;
                    players[i].transform.position = other.transform.position + other.transform.TransformDirection(localOffset);
                    players[i].transform.SetParent(other.transform);

                    // Forzar a mirar hacia la derecha (x positivo)
                    players[i].transform.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);

                    // Forzar animación de correr
                    var animator = players[i].GetComponentInChildren<Animator>();
                    if (animator != null)
                    {
                        animator.SetFloat("Speed", 1f);
                        Debug.Log($"[CameraTrigger] Animator encontrado en {players[i].name}. Speed seteado a 1.");
                    }
                    else
                    {
                        Debug.LogWarning($"[CameraTrigger] Animator NO encontrado en {players[i].name}.");
                    }
                }
                enemySpawner.isSpawning = false;
                UIManager.Instance.DialogueSetup();
                var dialogueSystem = FindFirstObjectByType<DialogueSystem>();
                if (dialogueSystem != null)
                {
                    // Pasar el identificador del pasillo (nombre del suelo) al sistema de diálogos
                    dialogueSystem.ActivateDialogueForPasillo(gameObject.name);
                }
            }
            else
            {
                // Reactivar input y quitar cinemática a todos los jugadores
                var players = FindObjectsOfType<Player>();
                foreach (var player in players)
                {
                    player.SetInputEnabled(true);
                    player.isCinematicRun = false;
                    // Quitar parent del camión
                    player.transform.SetParent(null);
                }
                enemySpawner.isSpawning = true;
                UIManager.Instance.GameUISetup();
            }
        }
    }
}
