/*using UnityEngine;
using System.Collections;

public class FrogCombat : MonoBehaviour
{
    [Header("Boss Data")]
    [SerializeField] private EnemyStatsData bossData;
    [SerializeField] private bool showDebugLogs = true;

    [Header("Sistema de Fases")]
    [SerializeField] private int faseActual = 1;
    [SerializeField] private float umbralFase2 = 0.66f; // Cambia a fase 2 al 66% de vida
    [SerializeField] private float umbralFase3 = 0.33f; // Cambia a fase 3 al 33% de vida

    [Header("Efectos de Transición (Opcional)")]
    [SerializeField] private ParticleSystem efectoCambioFase;
    [SerializeField] private AudioClip sonidoCambioFase;
    [SerializeField] private AudioSource audioSource;

    [Header("Fase 1 - Spawn de Enemigos")]
    [SerializeField] private GameObject enemigoPrefab;
    [SerializeField] private Transform[] puntosSpawn; // Puntos donde aparecerán los enemigos
    [SerializeField] private float intervaloSpawn = 5f; // Cada cuántos segundos spawnea
    [SerializeField] private int maxEnemigosSimultaneos = 5;
    [SerializeField] private int enemigosSpawneadosPorOleada = 2;

    // Stats del boss (cargadas desde ScriptableObject)
    private float vidaMaxima;
    private float vidaActual;

    private bool enTransicionFase = false;
    private float tiempoUltimoSpawn;
    private int enemigosActivosFase1 = 0;

    void Start()
    {
        if (bossData == null)
        {
            Debug.LogError("[FrogCombat] No hay BossData asignado!");
            return;
        }

        // Cargar stats desde el ScriptableObject
        vidaMaxima = bossData.MaxHealth;
        vidaActual = vidaMaxima;
        
        InicializarFase1();
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        Log($"Boss iniciado - Vida: {vidaMaxima} ({bossData.EnemyName})");
    }

    void Update()
    {
        if (enTransicionFase) return;

        // Ejecutar comportamiento de la fase actual
        switch (faseActual)
        {
            case 1:
                ComportamientoFase1();
                break;
            case 2:
                ComportamientoFase2();
                break;
            case 3:
                ComportamientoFase3();
                break;
        }
    }

    #region Sistema de Vida y Daño
    
    public void RecibirDanio(float cantidad)
    {
        if (vidaActual <= 0) return;

        vidaActual -= cantidad;
        vidaActual = Mathf.Max(0, vidaActual);

        Log($"Boss recibió {cantidad} de daño. Vida: {vidaActual}/{vidaMaxima} ({GetPorcentajeVida() * 100:F1}%)");

        VerificarCambioDeFase();

        if (vidaActual <= 0)
        {
            MorirBoss();
        }
    }

    public float GetPorcentajeVida()
    {
        return vidaActual / vidaMaxima;
    }

    #endregion

    #region Sistema de Fases

    private void VerificarCambioDeFase()
    {
        float porcentajeVida = GetPorcentajeVida();

        if (faseActual == 1 && porcentajeVida <= umbralFase2)
        {
            CambiarAFase(2);
        }
        else if (faseActual == 2 && porcentajeVida <= umbralFase3)
        {
            CambiarAFase(3);
        }
    }

    private void CambiarAFase(int nuevaFase)
    {
        if (enTransicionFase || faseActual == nuevaFase) return;

        Log($"========== CAMBIO DE FASE: {faseActual} -> {nuevaFase} ==========");
        StartCoroutine(TransicionDeFase(nuevaFase));
    }

    private IEnumerator TransicionDeFase(int nuevaFase)
    {
        enTransicionFase = true;

        // Efectos de transición
        if (efectoCambioFase != null)
            efectoCambioFase.Play();

        if (audioSource != null && sonidoCambioFase != null)
            audioSource.PlayOneShot(sonidoCambioFase);

        yield return new WaitForSeconds(1f);

        faseActual = nuevaFase;

        // Inicializar la nueva fase
        switch (nuevaFase)
        {
            case 2:
                InicializarFase2();
                break;
            case 3:
                InicializarFase3();
                break;
        }

        enTransicionFase = false;
    }

    #endregion

    #region Inicialización de Fases

    private void InicializarFase1()
    {
        Log("=== FASE 1 INICIADA: Invocación de Enemigos ===");
        tiempoUltimoSpawn = Time.time;
        enemigosActivosFase1 = 0;
    }

    private void InicializarFase2()
    {
        Log("=== FASE 2 INICIADA ===");
        // TODO: Configurar comportamiento de fase 2
    }

    private void InicializarFase3()
    {
        Log("=== FASE 3 INICIADA ===");
        // TODO: Configurar comportamiento de fase 3
    }

    #endregion

    #region Comportamiento de Fases

    private void ComportamientoFase1()
    {
        // Spawnear enemigos periódicamente
        if (Time.time - tiempoUltimoSpawn >= intervaloSpawn)
        {
            if (enemigosActivosFase1 < maxEnemigosSimultaneos)
            {
                SpawnearOleadaEnemigos();
                tiempoUltimoSpawn = Time.time;
            }
        }
    }

    private void SpawnearOleadaEnemigos()
    {
        if (enemigoPrefab == null)
        {
            Log("ERROR: No hay prefab de enemigo asignado");
            return;
        }

        int enemigosASpawnear = Mathf.Min(enemigosSpawneadosPorOleada, maxEnemigosSimultaneos - enemigosActivosFase1);

        for (int i = 0; i < enemigosASpawnear; i++)
        {
            Vector3 posicionSpawn = ObtenerPosicionSpawn();
            GameObject enemigo = Instantiate(enemigoPrefab, posicionSpawn, Quaternion.identity);
            
            // Buscar el componente que gestiona el enemigo (carga el ScriptableObject)
            var enemigoComponent = enemigo.GetComponent<Enemy>();
            if (enemigoComponent != null)
            {
                // Suscribirse al evento de muerte desde el ScriptableObject/Component
                enemigoComponent.OnMuerte += OnEnemigoMuerto;
            }
            else
            {
                Log("ADVERTENCIA: El enemigo no tiene componente Enemy");
            }
            
            enemigosActivosFase1++;
            Log($"Enemigo spawneado en {posicionSpawn}. Enemigos activos: {enemigosActivosFase1}");
        }

        Log($"Oleada spawneada: {enemigosASpawnear} enemigos");
    }

    private void OnEnemigoMuerto()
    {
        enemigosActivosFase1--;
        Log($"Enemigo eliminado. Enemigos activos: {enemigosActivosFase1}");
    }

    private Vector3 ObtenerPosicionSpawn()
    {
        // Si hay puntos de spawn definidos, usar uno aleatorio
        if (puntosSpawn != null && puntosSpawn.Length > 0)
        {
            Transform puntoAleatorio = puntosSpawn[Random.Range(0, puntosSpawn.Length)];
            return puntoAleatorio.position;
        }
        
        // Si no, spawnear en círculo alrededor del boss
        float angulo = Random.Range(0f, 360f);
        float distancia = Random.Range(5f, 10f);
        Vector3 offset = new Vector3(
            Mathf.Cos(angulo * Mathf.Deg2Rad) * distancia,
            0f,
            Mathf.Sin(angulo * Mathf.Deg2Rad) * distancia
        );
        
        return transform.position + offset;
    }

    private void ComportamientoFase2()
    {
        // TODO: Implementar comportamiento de fase 2
    }

    private void ComportamientoFase3()
    {
        // TODO: Implementar comportamiento de fase 3
    }

    #endregion

    #region Muerte

    private void MorirBoss()
    {
        Log("¡BOSS DERROTADO!");
        // TODO: Implementar muerte del boss
        gameObject.SetActive(false);
    }

    #endregion

    #region Debug y Testing

    private void Log(string mensaje)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[FrogCombat] {mensaje}");
        }
    }

    [ContextMenu("Probar Daño (100)")]
    private void ProbarDanio()
    {
        RecibirDanio(100f);
    }

    [ContextMenu("Forzar Fase 2")]
    private void ForzarFase2()
    {
        CambiarAFase(2);
    }

    [ContextMenu("Forzar Fase 3")]
    private void ForzarFase3()
    {
        CambiarAFase(3);
    }

    #endregion
}
*/