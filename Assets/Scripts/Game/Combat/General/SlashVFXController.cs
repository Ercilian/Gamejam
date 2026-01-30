using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Controlador dedicado para los efectos visuales de slash de combate.
    /// Maneja la instanciación, configuración y propiedades de material de los slashes.
    /// </summary>
    public class SlashVFXController : MonoBehaviour
    {
        [Header("Weapon Reference")]
        [Tooltip("Transform del arma donde se instanciarán los slashes. Si no se asigna, se usará el transform del personaje.")]
        public Transform weaponTransform;
        
        [Header("Debug")]
        [Tooltip("Activar logs detallados para debugging")]
        public bool showDebugLogs = false;
        
        /// <summary>
        /// Configuración completa de un slash VFX para un paso del combo
        /// </summary>
        [Serializable]
        public class SlashConfig
        {
            [Header("VFX - Slash Effect")]
            [Tooltip("Prefab del efecto visual de slash para este paso. Se instancia cuando se ejecuta el ataque.")]
            public GameObject slashVFXPrefab;
            [Tooltip("Si está activo, el slash será hijo del arma y la seguirá. Si es false, se instancia en el mundo.")]
            public bool attachToWeapon = true;
            [Tooltip("Offset local donde aparece el slash (relativo al arma o personaje)")]
            public Vector3 slashOffset = new Vector3(0f, 0f, 0.5f);
            [Tooltip("Rotación local del slash (en grados Euler)")]
            public Vector3 slashRotation = Vector3.zero;
            [Tooltip("Escala del slash VFX (X, Y, Z independientes)")]
            public Vector3 slashScale = Vector3.one;
            [Tooltip("Tiempo de vida del slash antes de destruirse (si no tiene ParticleSystem con auto-destroy)")]
            public float slashLifetime = 1f;
            
            [Header("VFX - Material Properties")]
            [Tooltip("Lista de propiedades personalizadas del material a modificar")]
            public List<MaterialProperty> materialProperties = new List<MaterialProperty>();
            
            [Header("VFX - Animated Properties")]
            [Tooltip("Lista de propiedades a animar durante el lifetime del slash")]
            public List<MaterialPropertyAnimation> animatedProperties = new List<MaterialPropertyAnimation>();
        }
        
        /// <summary>
        /// Animación de una propiedad de material a lo largo del tiempo
        /// </summary>
        [Serializable]
        public class MaterialPropertyAnimation
        {
            [Tooltip("Nombre de la propiedad a animar (ej: _Color, _EmissionColor, _Alpha)")]
            public string propertyName = "_Color";
            
            public enum AnimationType { Float, Color }
            [Tooltip("Tipo de animación")]
            public AnimationType animationType = AnimationType.Color;
            
            [Header("Float Animation")]
            [Tooltip("Curva que define cómo cambia el valor float en el tiempo (0-1 = inicio-fin del lifetime)")]
            public AnimationCurve floatCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            [Tooltip("Multiplicador del valor de la curva")]
            public float floatMultiplier = 1f;
            
            [Header("Color Animation")]
            [Tooltip("Gradiente de color a lo largo del lifetime (0-1 = inicio-fin)")]
            public Gradient colorGradient = new Gradient();
            [Tooltip("Si está activo, habilita _EMISSION keyword si la propiedad contiene 'Emission'")]
            public bool enableEmissionKeyword = true;
        }
        
        /// <summary>
        /// Propiedad personalizada de un material/shader
        /// </summary>
        [Serializable]
        public class MaterialProperty
        {
            [Tooltip("Nombre de la propiedad en el shader (ej: _Color, _Metallic, _Smoothness, _EmissionColor)")]
            public string propertyName = "_Color";
            
            public enum PropertyType { Float, Color, Vector, Texture }
            [Tooltip("Tipo de propiedad del shader")]
            public PropertyType type = PropertyType.Color;
            
            [Tooltip("Valor float (si type == Float)")]
            public float floatValue = 1f;
            
            [Tooltip("Valor color (si type == Color). Soporta HDR")]
            [ColorUsage(true, true)]
            public Color colorValue = Color.white;
            
            [Tooltip("Valor vector (si type == Vector)")]
            public Vector4 vectorValue = Vector4.zero;
            
            [Tooltip("Textura (si type == Texture)")]
            public Texture textureValue;
        }
        
        /// <summary>
        /// Instancia un slash VFX según la configuración proporcionada
        /// </summary>
        /// <param name="config">Configuración del slash a instanciar</param>
        /// <param name="parentTransform">Transform del personaje (usado si no hay arma)</param>
        /// <param name="stepIndex">Índice del paso del combo (para debugging)</param>
        /// <returns>GameObject instanciado del slash, o null si no se pudo crear</returns>
        public GameObject SpawnSlash(SlashConfig config, Transform parentTransform, int stepIndex = -1)
        {
            if (showDebugLogs)
                Debug.Log($"[SlashVFX] === INTENTANDO INSTANCIAR SLASH (Paso {stepIndex}) ===");
            
            if (config == null)
            {
                Debug.LogError("[SlashVFX] Config es NULL!");
                return null;
            }
            
            if (config.slashVFXPrefab == null)
            {
                Debug.LogError($"[SlashVFX] slashVFXPrefab es NULL en el paso {stepIndex}!");
                Debug.LogError("[SlashVFX] → Asigna un prefab en el Inspector: Steps → VFX Slash Effect → Slash Config → Slash VFX Prefab");
                return null;
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"[SlashVFX] Prefab válido: {config.slashVFXPrefab.name}");
                Debug.Log($"[SlashVFX] AttachToWeapon: {config.attachToWeapon}");
                Debug.Log($"[SlashVFX] WeaponTransform asignado: {(weaponTransform != null ? weaponTransform.name : "NO (se usará el personaje)")}");
            }
            
            Transform targetParent = config.attachToWeapon && weaponTransform != null ? weaponTransform : parentTransform;
            GameObject slashInstance;
            
            if (config.attachToWeapon && weaponTransform != null)
            {
                // Instanciar como hijo del arma
                slashInstance = Instantiate(config.slashVFXPrefab, weaponTransform);
                slashInstance.transform.localPosition = config.slashOffset;
                slashInstance.transform.localRotation = Quaternion.Euler(config.slashRotation);
                slashInstance.transform.localScale = config.slashScale;
                slashInstance.name = $"Slash_Step{stepIndex}_{config.slashVFXPrefab.name}";
                
                if (showDebugLogs)
                {
                    Debug.Log($"[SlashVFX] ✓ Slash instanciado como hijo del arma: {weaponTransform.name}");
                    Debug.Log($"[SlashVFX]   - Posición local: {slashInstance.transform.localPosition}");
                    Debug.Log($"[SlashVFX]   - Posición mundo: {slashInstance.transform.position}");
                    Debug.Log($"[SlashVFX]   - Escala: {slashInstance.transform.localScale}");
                }
            }
            else
            {
                // Instanciar como hijo del player para que siga su movimiento
                slashInstance = Instantiate(config.slashVFXPrefab, parentTransform);
                slashInstance.transform.localPosition = config.slashOffset;
                slashInstance.transform.localRotation = Quaternion.Euler(config.slashRotation);
                slashInstance.transform.localScale = config.slashScale;
                slashInstance.name = $"Slash_Step{stepIndex}_{config.slashVFXPrefab.name}";
                
                if (showDebugLogs)
                {
                    Debug.Log($"[SlashVFX] ✓ Slash instanciado como hijo del player: {parentTransform.name}");
                    Debug.Log($"[SlashVFX]   - Posición local: {slashInstance.transform.localPosition}");
                    Debug.Log($"[SlashVFX]   - Posición mundo: {slashInstance.transform.position}");
                    Debug.Log($"[SlashVFX]   - Escala: {slashInstance.transform.localScale}");
                }
            }
            
            // Verificar que el slash tiene componentes visibles
            if (showDebugLogs)
            {
                var renderers = slashInstance.GetComponentsInChildren<Renderer>(true);
                var particleSystems = slashInstance.GetComponentsInChildren<ParticleSystem>(true);
                Debug.Log($"[SlashVFX]   - Renderers encontrados: {renderers.Length}");
                Debug.Log($"[SlashVFX]   - ParticleSystems encontrados: {particleSystems.Length}");
                
                if (renderers.Length == 0 && particleSystems.Length == 0)
                {
                    Debug.LogWarning($"[SlashVFX] ⚠ El prefab no tiene Renderers ni ParticleSystems! No será visible.");
                }
            }
            
            // Aplicar propiedades personalizadas del material
            if (config.materialProperties != null && config.materialProperties.Count > 0)
            {
                ApplyMaterialProperties(slashInstance, config);
            }
            
            // Iniciar animación de propiedades si hay configuradas
            if (config.animatedProperties != null && config.animatedProperties.Count > 0)
            {
                float lifetime = GetSlashLifetime(slashInstance, config);
                StartCoroutine(AnimateMaterialProperties(slashInstance, config, lifetime));
            }
            
            // Configurar auto-destrucción
            SetupAutoDestroy(slashInstance, config);
            
            return slashInstance;
        }
        
        /// <summary>
        /// Obtiene el lifetime del slash basándose en ParticleSystem o configuración
        /// </summary>
        float GetSlashLifetime(GameObject slashInstance, SlashConfig config)
        {
            ParticleSystem ps = slashInstance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                if (!main.loop)
                {
                    return main.duration + main.startLifetime.constantMax;
                }
            }
            return config.slashLifetime;
        }
        
        /// <summary>
        /// Configura la destrucción automática del slash basándose en su duración
        /// </summary>
        void SetupAutoDestroy(GameObject slashInstance, SlashConfig config)
        {
            ParticleSystem ps = slashInstance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                // Si tiene ParticleSystem, usar su duración
                var main = ps.main;
                if (!main.loop)
                {
                    Destroy(slashInstance, main.duration + main.startLifetime.constantMax);
                }
            }
            else
            {
                // Si no tiene ParticleSystem, usar el lifetime configurado
                Destroy(slashInstance, config.slashLifetime);
            }
        }
        
        /// <summary>
        /// Aplica todas las propiedades personalizadas del material al slash instanciado
        /// </summary>
        void ApplyMaterialProperties(GameObject slashInstance, SlashConfig config)
        {
            // Buscar todos los Renderers en el slash y sus hijos
            Renderer[] renderers = slashInstance.GetComponentsInChildren<Renderer>(true);
            
            if (renderers.Length == 0)
            {
                if (showDebugLogs)
                    Debug.LogWarning("[SlashVFX] No se encontraron Renderers en el slash VFX");
                return;
            }
            
            foreach (Renderer renderer in renderers)
            {
                // Crear copias de los materiales para no modificar el asset original
                Material[] materials = renderer.materials;
                bool materialsModified = false;
                
                for (int i = 0; i < materials.Length; i++)
                {
                    Material mat = materials[i];
                    
                    // Aplicar cada propiedad personalizada
                    foreach (var prop in config.materialProperties)
                    {
                        if (string.IsNullOrEmpty(prop.propertyName)) continue;
                        
                        if (!mat.HasProperty(prop.propertyName))
                        {
                            if (showDebugLogs)
                                Debug.LogWarning($"[SlashVFX] Material '{mat.name}' no tiene la propiedad '{prop.propertyName}'");
                            continue;
                        }
                        
                        ApplySingleProperty(mat, prop);
                        materialsModified = true;
                    }
                }
                
                if (materialsModified)
                {
                    renderer.materials = materials;
                }
            }
        }
        
        /// <summary>
        /// Aplica una propiedad individual al material
        /// </summary>
        void ApplySingleProperty(Material mat, MaterialProperty prop)
        {
            switch (prop.type)
            {
                case MaterialProperty.PropertyType.Float:
                    mat.SetFloat(prop.propertyName, prop.floatValue);
                    if (showDebugLogs)
                        Debug.Log($"[SlashVFX] Aplicado {prop.propertyName} = {prop.floatValue}");
                    break;
                    
                case MaterialProperty.PropertyType.Color:
                    mat.SetColor(prop.propertyName, prop.colorValue);
                    // Si es una propiedad de emisión, habilitar la keyword
                    if (prop.propertyName.Contains("Emission"))
                    {
                        mat.EnableKeyword("_EMISSION");
                    }
                    if (showDebugLogs)
                        Debug.Log($"[SlashVFX] Aplicado {prop.propertyName} = {prop.colorValue}");
                    break;
                    
                case MaterialProperty.PropertyType.Vector:
                    mat.SetVector(prop.propertyName, prop.vectorValue);
                    if (showDebugLogs)
                        Debug.Log($"[SlashVFX] Aplicado {prop.propertyName} = {prop.vectorValue}");
                    break;
                    
                case MaterialProperty.PropertyType.Texture:
                    if (prop.textureValue != null)
                    {
                        mat.SetTexture(prop.propertyName, prop.textureValue);
                        if (showDebugLogs)
                            Debug.Log($"[SlashVFX] Aplicado {prop.propertyName} = {prop.textureValue.name}");
                    }
                    break;
            }
        }
        
        /// <summary>
        /// Anima las propiedades del material a lo largo del lifetime del slash
        /// </summary>
        System.Collections.IEnumerator AnimateMaterialProperties(GameObject slashInstance, SlashConfig config, float lifetime)
        {
            if (slashInstance == null || config.animatedProperties == null || config.animatedProperties.Count == 0)
                yield break;
            
            // Obtener todos los renderers una sola vez
            Renderer[] renderers = slashInstance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                if (showDebugLogs)
                    Debug.LogWarning("[SlashVFX] No hay renderers para animar");
                yield break;
            }
            
            // Recopilar todos los materiales
            List<Material> allMaterials = new List<Material>();
            foreach (var renderer in renderers)
            {
                allMaterials.AddRange(renderer.materials);
            }
            
            if (showDebugLogs)
                Debug.Log($"[SlashVFX] Iniciando animación de {config.animatedProperties.Count} propiedades durante {lifetime}s");
            
            float elapsed = 0f;
            
            while (elapsed < lifetime && slashInstance != null)
            {
                float t = elapsed / lifetime; // Normalizado 0-1
                
                // Aplicar cada propiedad animada
                foreach (var animProp in config.animatedProperties)
                {
                    if (string.IsNullOrEmpty(animProp.propertyName)) continue;
                    
                    foreach (var mat in allMaterials)
                    {
                        if (mat == null || !mat.HasProperty(animProp.propertyName)) continue;
                        
                        switch (animProp.animationType)
                        {
                            case MaterialPropertyAnimation.AnimationType.Float:
                                float floatValue = animProp.floatCurve.Evaluate(t) * animProp.floatMultiplier;
                                mat.SetFloat(animProp.propertyName, floatValue);
                                break;
                                
                            case MaterialPropertyAnimation.AnimationType.Color:
                                Color colorValue = animProp.colorGradient.Evaluate(t);
                                mat.SetColor(animProp.propertyName, colorValue);
                                
                                // Habilitar keyword de emisión si es necesario
                                if (animProp.enableEmissionKeyword && animProp.propertyName.Contains("Emission"))
                                {
                                    mat.EnableKeyword("_EMISSION");
                                }
                                break;
                        }
                    }
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (showDebugLogs)
                Debug.Log("[SlashVFX] Animación de propiedades completada");
        }
        
        /// <summary>
        /// Destruye inmediatamente un slash VFX si existe
        /// </summary>
        public void DestroySlash(GameObject slash)
        {
            if (slash != null)
            {
                Destroy(slash);
            }
        }
        
        /// <summary>
        /// Cambia dinámicamente una propiedad de material en un slash ya instanciado
        /// </summary>
        public void UpdateSlashMaterialProperty(GameObject slash, string propertyName, Color colorValue)
        {
            if (slash == null) return;
            
            Renderer[] renderers = slash.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                foreach (Material mat in renderer.materials)
                {
                    if (mat.HasProperty(propertyName))
                    {
                        mat.SetColor(propertyName, colorValue);
                    }
                }
            }
        }
        
        /// <summary>
        /// Cambia dinámicamente una propiedad float de material en un slash ya instanciado
        /// </summary>
        public void UpdateSlashMaterialProperty(GameObject slash, string propertyName, float floatValue)
        {
            if (slash == null) return;
            
            Renderer[] renderers = slash.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                foreach (Material mat in renderer.materials)
                {
                    if (mat.HasProperty(propertyName))
                    {
                        mat.SetFloat(propertyName, floatValue);
                    }
                }
            }
        }
    }
}
