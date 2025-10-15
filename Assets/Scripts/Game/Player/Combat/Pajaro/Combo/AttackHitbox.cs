using UnityEngine;

// Hitbox de ataque basada en GameObject con Collider (isTrigger)
// Detecta enemigos al entrar y aplica daño/efecto. Evita dobles golpes en un mismo enemigo durante su vida.
public class AttackHitbox : MonoBehaviour
{
    private int damage;
    private LayerMask enemyLayer;
    private Transform owner;

    // Para evitar aplicar daño varias veces al mismo objetivo en una ventana
    private System.Collections.Generic.HashSet<Collider> hitSet = new System.Collections.Generic.HashSet<Collider>();

    // Inicialización desde el spawner (MeleeBase)
    public void Init(int damage, LayerMask enemyLayer, Transform owner)
    {
        this.damage = damage;
        this.enemyLayer = enemyLayer;
        this.owner = owner;

        // Hacer invisible por si el prefab tiene MeshRenderer
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) == 0) return; // filtrar por capa
        if (other.transform == owner) return; // no golpear al dueño
        if (hitSet.Contains(other)) return; // ya golpeado en esta ventana

        hitSet.Add(other);

        // Aquí aplicas daño real si tienes un componente de vida en el enemigo
        // var health = other.GetComponent<EnemyHealth>();
        // if (health) health.TakeDamage(damage);

        // Por ahora, placeholder: destruir enemigo
        Destroy(other.gameObject);
    }
}
