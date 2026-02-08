
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

    [Header("Player Selection Data (Assign in Inspector)")]
    public PlayerSelectionDataSO playerSelectionDataSO;
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
            for (int i = 0; i < playersInZone.Count; i++)
            {
                var playerInput = playersInZone[i];
                var interactAction = playerInput.actions["Interact"];
                if (interactAction != null && interactAction.WasPressedThisFrame())
                {
                    if (!playersSeated.Contains(playerInput))
                    {
                        // Buscar el characterIndex en el componente Player
                        var playerComponent = playerInput.GetComponent<Player>();
                        int characterIndex = (playerComponent != null) ? playerComponent.characterIndex : -1;
                        if (characterIndex < 0 || characterIndex >= seatPoints.Count)
                        {
                            Debug.LogWarning($"[ShopBonfire] No se encontró characterIndex válido para {playerInput.gameObject.name}");
                            continue;
                        }
                        // Guardar posición y rotación original
                        originalPositions[playerInput] = playerInput.transform.position;
                        originalRotations[playerInput] = playerInput.transform.rotation;
                        // Guardar y poner Rigidbody en isKinematic
                        Rigidbody rb = playerInput.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            playerWasKinematic[playerInput] = rb.isKinematic;
                            rb.isKinematic = true;
                        }
                        // Asignar asiento por characterIndex
                        var seat = seatPoints[characterIndex];
                        playerInput.transform.position = seat.position;
                        playerInput.transform.rotation = seat.rotation;
                        playersSeated.Add(playerInput);
                        // Activar trigger Bonfire en el Animator del jugador
                        Animator animator = playerInput.GetComponentInChildren<Animator>();
                        if (animator != null)
                        {
                            animator.SetTrigger("Bonfire");
                        }
                        // Desactivar controles salvo Interact
                        SetPlayerControls(playerInput, false);
                    }
                    else
                    {
                        // Volver a la posición original y devolver controles
                        if (originalPositions.ContainsKey(playerInput) && originalRotations.ContainsKey(playerInput))
                        {
                            playerInput.transform.position = originalPositions[playerInput];
                            playerInput.transform.rotation = originalRotations[playerInput];
                            originalPositions.Remove(playerInput);
                            originalRotations.Remove(playerInput);
                        }
                        // Restaurar Rigidbody isKinematic
                        Rigidbody rb = playerInput.GetComponent<Rigidbody>();
                        if (rb != null && playerWasKinematic.ContainsKey(playerInput))
                        {
                            rb.isKinematic = playerWasKinematic[playerInput];
                            playerWasKinematic.Remove(playerInput);
                        }
                        // Activar trigger BonfireExit en el Animator del jugador
                        Animator animator = playerInput.GetComponentInChildren<Animator>();
                        if (animator != null)
                        {
                            animator.SetTrigger("BonfireExit");
                        }
                        playersSeated.Remove(playerInput);
                        SetPlayerControls(playerInput, true);
                        // Busca el characterIndex correspondiente a un PlayerInput usando PlayerSelectionDataSO
                        int GetCharacterIndexForPlayerInput(PlayerInput playerInput)
                        {
                            if (playerSelectionDataSO == null) return -1;
                            // Buscar por deviceId (puedes ajustar esto según tu lógica de emparejamiento)
                            string inputDeviceId = playerInput.devices.Count > 0 ? playerInput.devices[0].deviceId.ToString() : "";
                            foreach (var info in playerSelectionDataSO.selectedPlayers)
                            {
                                if (info.inputDeviceId == inputDeviceId)
                                {
                                    return info.characterIndex;
                                }
                            }
                            return -1;
                        }
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
    // Busca el characterIndex correspondiente a un PlayerInput usando PlayerSelectionDataSO
    private int GetCharacterIndexForPlayerInput(PlayerInput playerInput)
    {
        if (playerSelectionDataSO == null) return -1;
        // Buscar por deviceId (puedes ajustar esto según tu lógica de emparejamiento)
        string inputDeviceId = playerInput.devices.Count > 0 ? playerInput.devices[0].deviceId.ToString() : "";
        foreach (var info in playerSelectionDataSO.selectedPlayers)
        {
            if (info.inputDeviceId == inputDeviceId)
            {
                return info.characterIndex;
            }
        }
        return -1;
    }
}
