using UnityEngine;

namespace Game.Player.Combat.Pajaro.Habilidad
{
    [System.Serializable]
    public class HabilidadDamageConfig
    {
        public int dañoPorTick = 10;
        public float knockbackForce = 5f;
        public HabilidadDamageType damageType = HabilidadDamageType.Normal;
        public LayerMask layerEnemigos;
        public KeyCode teclaHabilidad = KeyCode.Q;
    }
}
