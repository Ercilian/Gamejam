
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public class ShopBonfire : MonoBehaviour
{
    [Header("Bonfire Seats (Assign in Inspector)")]
    public List<Transform> seatPoints;
    [Header("Bonfire Duration")]
    public float bonfireCinematicDuration = 5f;
    private List<PlayerInput> playersInZone = new List<PlayerInput>();
    private HashSet<PlayerInput> playersInteracted = new HashSet<PlayerInput>();
    private Dictionary<PlayerInput, Vector3> originalPositions = new Dictionary<PlayerInput, Vector3>();
    private Dictionary<PlayerInput, Quaternion> originalRotations = new Dictionary<PlayerInput, Quaternion>();
    private HashSet<PlayerInput> playersSeated = new HashSet<PlayerInput>();
    private Dictionary<PlayerInput, bool> playerWasKinematic = new Dictionary<PlayerInput, bool>();
    private CameraMovement cameraMovement;
    private bool bonfireCinematicActive = false;
    
    
        void Start()
    {
        cameraMovement = FindFirstObjectByType<CameraMovement>();
    }
    void Update()
    {
        // Buscar todos los jugadores en la escena (máximo 3)
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        int totalPlayers = Mathf.Min(allPlayers.Length, 3);

        // Debug: mostrar jugadores en la zona y total de jugadores
        string playersInZoneNames = string.Join(", ", playersInZone.ConvertAll(p => p.gameObject.name));
        string playersInteractedNames = string.Join(", ", new List<PlayerInput>(playersInteracted).ConvertAll(p => p.gameObject.name));

        // Teletransportar individualmente al asiento cuando pulsan Interact
        if (!bonfireCinematicActive)
        {
            for (int i = 0; i < playersInZone.Count && i < seatPoints.Count; i++)
            {
                var player = playersInZone[i];
                var interactAction = player.actions["Interact"];
                if (interactAction != null && interactAction.WasPressedThisFrame())
                {
                    if (!playersSeated.Contains(player))
                    {
                        // Guardar posición y rotación original
                        originalPositions[player] = player.transform.position;
                        originalRotations[player] = player.transform.rotation;
                        // Guardar y poner Rigidbody en isKinematic
                        Rigidbody rb = player.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            playerWasKinematic[player] = rb.isKinematic;
                            rb.isKinematic = true;
                        }
                        // Teletransportar y "sentar"
                        var seat = seatPoints[i];
                        player.transform.position = seat.position;
                        player.transform.rotation = seat.rotation;
                        playersSeated.Add(player);
                        // Desactivar controles salvo Interact
                        SetPlayerControls(player, false);
                    }
                    else
                    {
                        // Volver a la posición original y devolver controles
                        if (originalPositions.ContainsKey(player) && originalRotations.ContainsKey(player))
                        {
                            player.transform.position = originalPositions[player];
                            player.transform.rotation = originalRotations[player];
                            originalPositions.Remove(player);
                            originalRotations.Remove(player);
                        }
                        // Restaurar Rigidbody isKinematic
                        Rigidbody rb = player.GetComponent<Rigidbody>();
                        if (rb != null && playerWasKinematic.ContainsKey(player))
                        {
                            rb.isKinematic = playerWasKinematic[player];
                            playerWasKinematic.Remove(player);
                        }
                        playersSeated.Remove(player);
                        SetPlayerControls(player, true);
                    }
                }
            }
        }
        else
        {
            // Si la cinemática está activa, bloquear todos los controles de los jugadores sentados
            foreach (var player in playersSeated)
            {
                SetPlayerControls(player, false);
            }
        }

        // Si todos los jugadores de la partida están sentados y la cinemática no está activa, activar modo BonfireCinematic
        if (!bonfireCinematicActive && playersSeated.Count == totalPlayers && totalPlayers > 0)
        {
            bonfireCinematicActive = true;
            {
                GameObject bonfireObj = GameObject.FindWithTag("Bonfire");
                cameraMovement.bonfireTarget = bonfireObj.transform;
                cameraMovement.SetCameraMode(CameraMovement.CameraMode.BonfireCinematic);
                UIManager.Instance.DialogueSetup();
                // Activar diálogo de bonfire igual que en pasillos
                var dialogueSystem = FindFirstObjectByType<DialogueSystem>();
                if (dialogueSystem != null)
                {
                    dialogueSystem.ActivateDialogueForBonfire(bonfireObj != null ? bonfireObj.name : "Bonfire");
                }
            }
            StartCoroutine(BonfireCinematicCoroutine());
        }
            // Coroutine simple para la cinemática de la hoguera
            System.Collections.IEnumerator BonfireCinematicCoroutine()
            {
                yield return new WaitForSeconds(bonfireCinematicDuration);

                // Devolver jugadores a su posición original y reactivar controles
                foreach (var player in new List<PlayerInput>(playersSeated))
                {
                    if (originalPositions.ContainsKey(player) && originalRotations.ContainsKey(player))
                    {
                        player.transform.position = originalPositions[player];
                        player.transform.rotation = originalRotations[player];
                        originalPositions.Remove(player);
                        originalRotations.Remove(player);
                    }

                    Rigidbody rb = player.GetComponent<Rigidbody>();
                    if (rb != null && playerWasKinematic.ContainsKey(player))
                    {
                        rb.isKinematic = playerWasKinematic[player];
                        playerWasKinematic.Remove(player);
                    }

                    SetPlayerControls(player, true);
                    playersSeated.Remove(player);
                }

                cameraMovement.SetCameraMode(CameraMovement.CameraMode.Normal);
                
                // Reactivar movimiento del camión
                GameObject carObj = GameObject.FindGameObjectWithTag("Car");
                MovCar movCar = carObj.GetComponent<MovCar>();
                movCar.enabled = true;
                UIManager.Instance.GameUISetup();

            }
        // Si algún jugador se levanta, desactivar la cinemática
        if (bonfireCinematicActive && playersSeated.Count < totalPlayers)
        {
            bonfireCinematicActive = false;
            if (cameraMovement != null)
            {
                cameraMovement.SetCameraMode(CameraMovement.CameraMode.Normal);
            }
        }

    // Habilita o deshabilita todos los controles del jugador salvo Interact
    void SetPlayerControls(PlayerInput player, bool enable)
    {
        foreach (var action in player.actions)
        {
            if (action.name != "Interact")
            {
                if (enable) action.Enable();
                else action.Disable();
            }
        }
    }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerInput playerInput = other.GetComponent<PlayerInput>();
        if (playerInput != null && !playersInZone.Contains(playerInput))
        {
            playersInZone.Add(playerInput);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerInput playerInput = other.GetComponent<PlayerInput>();
        if (playerInput != null && playersInZone.Contains(playerInput))
        {
            playersInZone.Remove(playerInput);
            playersInteracted.Remove(playerInput);
        }
    }

    // La función StartBonfireCinematic ya no es necesaria para el tp individual
}
