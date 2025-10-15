using UnityEngine;
using Game.Combat;

/// <summary>
/// Preset de configuración para combos de 3 golpes con escalamiento de daño
/// Úsalo como referencia para configurar tu MeleeBase
/// </summary>
[CreateAssetMenu(fileName = "ComboPreset", menuName = "Game/Combat/Combo Preset")]
public class ComboPreset : ScriptableObject
{
    [Header("Configuración del Combo")]
    [Tooltip("Daño base para todos los ataques (se puede sobrescribir por golpe)")]
    public int baseDamage = 10;
    
    [Header("Golpe 1: Ataque Rápido")]
    public HitboxConfig primerGolpe = new HitboxConfig
    {
        // Configuración de hitbox
        offset = new Vector3(0, 0, 1.5f),
        shape = HitboxShape.Box,
        boxSize = new Vector3(1.2f, 1f, 0.8f),
        
        // Configuración de daño
        damageOverride = -1,        // Usa baseDamage
        damageMultiplier = 0.8f,    // 80% del daño base
        damageType = DamageType.Normal,
        effects = DamageEffects.None,
        knockbackForce = 2f
    };
    
    [Header("Golpe 2: Ataque Medio")]
    public HitboxConfig segundoGolpe = new HitboxConfig
    {
        // Configuración de hitbox
        offset = new Vector3(0, 0, 1.8f),
        shape = HitboxShape.Box,
        boxSize = new Vector3(1.5f, 1.2f, 1f),
        
        // Configuración de daño
        damageOverride = -1,        // Usa baseDamage
        damageMultiplier = 1.2f,    // 120% del daño base
        damageType = DamageType.Normal,
        effects = DamageEffects.Knockback,
        knockbackForce = 5f
    };
    
    [Header("Golpe 3: Ataque Finalizador (Boomerang)")]
    public HitboxConfig tercerGolpe = new HitboxConfig
    {
        // Configuración de hitbox
        offset = new Vector3(0, 0, 2f),
        shape = HitboxShape.Sphere,
        sphereRadius = 1.5f,
        
        // Configuración de daño
        damageOverride = -1,        // Usa baseDamage
        damageMultiplier = 2.0f,    // 200% del daño base
        damageType = DamageType.Crítico,
        effects = DamageEffects.Knockback | DamageEffects.Stun,
        knockbackForce = 10f
    };
    
    [Header("Configuración Ejemplo Fuego")]
    public HitboxConfig golpeFuego = new HitboxConfig
    {
        offset = new Vector3(0, 0, 2f),
        shape = HitboxShape.Sector,
        sectorRadius = 3f,
        sectorAngleDeg = 120f,
        sectorHeight = 2f,
        
        damageOverride = 15,        // Daño fijo de fuego
        damageMultiplier = 1.5f,    
        damageType = DamageType.Fuego,
        effects = DamageEffects.Burn | DamageEffects.Knockback,
        knockbackForce = 8f
    };
    
    /// <summary>
    /// Aplica esta configuración a un MeleeBase
    /// </summary>
    public void ApplyToMeleeBase(MeleeBase meleeBase)
    {
        if (meleeBase == null) return;
        
        meleeBase.attackDamage = baseDamage;
        meleeBase.comboHitboxes.Clear();
        meleeBase.comboHitboxes.Add(primerGolpe);
        meleeBase.comboHitboxes.Add(segundoGolpe);
        meleeBase.comboHitboxes.Add(tercerGolpe);
        
        Debug.Log($"Configuración de combo aplicada a {meleeBase.name}:");
        Debug.Log($"- Golpe 1: {primerGolpe.damageMultiplier * baseDamage} daño ({primerGolpe.damageType})");
        Debug.Log($"- Golpe 2: {segundoGolpe.damageMultiplier * baseDamage} daño ({segundoGolpe.damageType})");
        Debug.Log($"- Golpe 3: {tercerGolpe.damageMultiplier * baseDamage} daño ({tercerGolpe.damageType})");
    }
}