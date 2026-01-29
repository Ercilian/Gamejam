using UnityEngine;

public class DeadManager : MonoBehaviour
{
    private bool gameOverTriggered = false;

    void Update()
    {
        if (gameOverTriggered) return;

        // Buscar todos los jugadores en la escena
        Player[] players = FindObjectsOfType<Player>();
        if (players.Length == 0) return;

        // Si todos están muertos (isDead == true)
        bool allDead = true;
        foreach (var player in players)
        {
            if (!player.isDead)
            {
                allDead = false;
                break;
            }
        }

        if (allDead)
        {
            // Buscar Options en la escena y llamar a GameOver
            Options options = FindObjectOfType<Options>();
            if (options != null)
            {
                options.GameOver();
                gameOverTriggered = true;
            }
        }
    }
}
