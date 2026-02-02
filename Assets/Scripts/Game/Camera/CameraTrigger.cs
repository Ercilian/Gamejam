using Microsoft.VisualBasic;
using UnityEngine;


public class CameraTrigger : MonoBehaviour
// Script to change camera offset when the car enters the trigger zone (cinematic camera).
{
    public float newOffsetX = 0f;
    public float delaySeconds = 2f;
    [SerializeField] private CameraMovement cameraMovement;
    public bool disableFuelConsumption = false;
    [Header("Zona de pasillo (activa diálogo)")]
    public bool isPasillo = false;
    public bool isBossRoom = false;
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
            // Elegir modo de cámara según el trigger y notificar a CameraMovement
            if (isBossRoom)
            {
                cameraMovement.SetCameraMode(CameraMovement.CameraMode.BossCinematic);
                cameraMovement.ChangeOffsetWithDelay(newOffsetX, delaySeconds);
                enemySpawner.enabled = false;
                // Eliminar todos los enemigos con el tag "Enemy"
                var bossRoomEnemies = GameObject.FindGameObjectsWithTag("Enemy");
                foreach (var enemy in bossRoomEnemies)
                {
                    Destroy(enemy);
                }
                // Reactivar Dash en BossRoom
                var allPlayersBoss = FindObjectsOfType<Player>();
                foreach (var p in allPlayersBoss)
                {
                    var dash = p.GetComponent<Dash>();
                    if (dash != null) dash.enabled = true;
                }
                return;
            }
            else if (isPasillo)
            {
                cameraMovement.SetCameraMode(CameraMovement.CameraMode.Pasillo);
                cameraMovement.ChangeOffsetWithDelay(newOffsetX, delaySeconds);
            }
            else
            {
                cameraMovement.SetCameraMode(CameraMovement.CameraMode.Normal);
                cameraMovement.ChangeOffsetWithDelay(newOffsetX, delaySeconds);
            }

            // ...existing code...
            var enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (var enemy in enemies) Destroy(enemy);
            var collectibles = GameObject.FindGameObjectsWithTag("Collectible");
            foreach (var collectible in collectibles) Destroy(collectible);
            var carFuelSystem = other.GetComponentInChildren<CarFuelSystem>();
            carFuelSystem.SetFuelConsumptionEnabled(!disableFuelConsumption);
            // ...existing code for pasillo and input management...
            var players = FindObjectsOfType<Player>();
            if (isPasillo)
            {
                Vector3[] offsets;
                if (players.Length == 1) offsets = new Vector3[] { new Vector3(5.5f, 0, 0f) };
                else if (players.Length == 2) offsets = new Vector3[] { new Vector3(3f, 0, -4.5f), new Vector3(3f, 0, 5f) };
                else offsets = new Vector3[] { new Vector3(5.5f, 0, 0f), new Vector3(3f, 0, -4.5f), new Vector3(3f, 0, 5f) };
                for (int i = 0; i < players.Length; i++)
                {
                    players[i].SetInputEnabled(false);
                    players[i].isCinematicRun = true;
                    Vector3 localOffset = (i < offsets.Length) ? offsets[i] : Vector3.zero;
                    players[i].transform.position = other.transform.position + other.transform.TransformDirection(localOffset);
                    players[i].transform.SetParent(other.transform);
                    players[i].transform.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
                    var animator = players[i].GetComponentInChildren<Animator>();
                    if (animator != null) animator.SetFloat("Speed", 1f);
                    // Desactivar Dash en pasillo
                    var dash = players[i].GetComponent<Dash>();
                    if (dash != null) dash.enabled = false;
                }
                enemySpawner.isSpawning = false;
                UIManager.Instance.DialogueSetup();
                var dialogueSystem = FindFirstObjectByType<DialogueSystem>();
                if (dialogueSystem != null) dialogueSystem.ActivateDialogueForPasillo(gameObject.name);
            }
            else
            {
                foreach (var player in players)
                {
                    player.SetInputEnabled(true);
                    player.isCinematicRun = false;
                    player.transform.SetParent(null);
                    // Reactivar Dash fuera de pasillo
                    var dash = player.GetComponent<Dash>();
                    if (dash != null) dash.enabled = true;
                }
                enemySpawner.isSpawning = true;
                UIManager.Instance.GameUISetup();
            }
        }
    }
}
