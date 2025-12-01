using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PlayerSelectionDataSO", menuName = "Game/Player Selection Data")]
public class PlayerSelectionDataSO : ScriptableObject
{
    [System.Serializable]
    public class PlayerInfo
    {
        public int slotIndex;
        public int characterIndex;
        public string inputDeviceId; // Guarda el ID del dispositivo
    }

    public List<PlayerInfo> selectedPlayers = new List<PlayerInfo>();

    public void Clear()
    {
        selectedPlayers.Clear();
    }
}
