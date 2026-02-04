using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    /// <summary>
    /// Reproduce un video de introducción al iniciar el juego y luego carga el menú principal
    /// </summary>
    public class IntroVideoPlayer : MonoBehaviour
    {
        [Header("Configuración del Video")]
        [Tooltip("Video a reproducir (arrastra tu video aquí)")]
        public VideoClip videoClip;
        
        [Tooltip("Nombre de la escena del menú principal a cargar después del video")]
        public string nombreEscenaMenu = "MainMenuScene";
        
        [Tooltip("Tiempo que hay que mantener ESC pulsado para saltar (en segundos)")]
        public float tiempoParaSaltar = 2f;
        
        [Header("UI")]
        [Tooltip("Texto que muestra el aviso para saltar (ej: 'Mantén ESC para saltar')")]
        public TMPro.TextMeshProUGUI textoSaltar;
        
        [Header("Opcional")]
        [Tooltip("RawImage donde mostrar el video (opcional, si no se asigna se reproduce a pantalla completa)")]
        public UnityEngine.UI.RawImage rawImageDestino;

        public UnityEngine.UI.Image blackPanel;
        
        private VideoPlayer videoPlayer;
        private bool videoTerminado = false;
        private float tiempoPresionandoESC = 0f;
        
        void Start()
        {
            ConfigurarVideoPlayer();

            // Mostrar panel negro al inicio
            if (blackPanel != null)
            {
                blackPanel.gameObject.SetActive(true);
            }

            // Ocultar texto al inicio
            if (textoSaltar != null)
            {
                textoSaltar.gameObject.SetActive(false);
            }

            // Preparar el video antes de reproducir
            videoPlayer.Prepare();
            videoPlayer.prepareCompleted += OnVideoPreparado;
        }
        
        void Update()
        {
            if (videoTerminado) return;
            
            // Detectar si se mantiene ESC pulsado
            if (Input.GetKey(KeyCode.Escape))
            {
                tiempoPresionandoESC += Time.deltaTime;
                
                // Mostrar texto
                if (textoSaltar != null && !textoSaltar.gameObject.activeSelf)
                {
                    textoSaltar.gameObject.SetActive(true);
                }
                
                // Actualizar texto con progreso
                if (textoSaltar != null)
                {
                    float progreso = (tiempoPresionandoESC / tiempoParaSaltar) * 100f;
                    textoSaltar.text = $"Mantén ESC para saltar... {Mathf.Min(progreso, 100f):F0}%";
                }
                
                // Saltar si se ha mantenido el tiempo suficiente
                if (tiempoPresionandoESC >= tiempoParaSaltar)
                {
                    SaltarVideo();
                }
            }
            else
            {
                // Si se suelta ESC, resetear
                if (tiempoPresionandoESC > 0f)
                {
                    tiempoPresionandoESC = 0f;
                    
                    if (textoSaltar != null)
                    {
                        textoSaltar.gameObject.SetActive(false);
                    }
                }
            }
        }
        
        private void ConfigurarVideoPlayer()
        {
            // Obtener o crear VideoPlayer
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
            }
            
            // Configurar el VideoPlayer
            videoPlayer.playOnAwake = false;
            videoPlayer.clip = videoClip;
            videoPlayer.isLooping = false;
            
            // Configurar modo de renderizado
            if (rawImageDestino != null)
            {
                // Mostrar en RawImage (UI)
                videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                RenderTexture rt = new RenderTexture(1920, 1080, 0);
                videoPlayer.targetTexture = rt;
                rawImageDestino.texture = rt;
            }
            else
            {
                // Mostrar a pantalla completa usando la cámara
                videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
                videoPlayer.targetCamera = Camera.main;
            }
            
            // Audio
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            
            // Evento cuando termina el video
            videoPlayer.loopPointReached += OnVideoTerminado;
        }
        
        private void ReproducirVideo()
        {
            if (videoClip == null)
            {
                Debug.LogWarning("[IntroVideoPlayer] No hay video asignado, cargando menú directamente");
                CargarMenuPrincipal();
                return;
            }
            videoPlayer.Play();
        }

        private void OnVideoPreparado(VideoPlayer vp)
        {
            // Ocultar panel negro justo cuando el video está listo
            if (blackPanel != null)
            {
                blackPanel.gameObject.SetActive(false);
            }
            ReproducirVideo();
            // Ya no necesitamos el evento
            videoPlayer.prepareCompleted -= OnVideoPreparado;
        }
        
        private void OnVideoTerminado(VideoPlayer vp)
        {
            videoTerminado = true;
            CargarMenuPrincipal();
        }
        
        private void SaltarVideo()
        {
            videoTerminado = true;
            videoPlayer.Stop();
            CargarMenuPrincipal();
        }
        
        private void CargarMenuPrincipal()
        {
            SceneManager.LoadScene(nombreEscenaMenu);
        }
        
        private void OnDestroy()
        {
            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached -= OnVideoTerminado;
                videoPlayer.prepareCompleted -= OnVideoPreparado;
            }
        }
    }
}
