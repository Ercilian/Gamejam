using UnityEngine;

public class MeleeBase : MonoBehaviour
{
    [Header("Ataque")]
    public float attackRange = 2f;
    public float attackCooldown = 0.3f;
    public int comboCount = 3;
    public float comboResetTime = 1f;
    public int attackDamage = 10;
    public LayerMask enemyLayer;

    private int currentCombo = 0;
    private float lastAttackTime = 0f;
    private float comboTimer = 0f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Botón izquierdo del mouse
        {
            TryAttack();
        }

        // Reset combo si pasa mucho tiempo sin atacar
        if (currentCombo > 0 && Time.time - comboTimer > comboResetTime)
        {
            currentCombo = 0;
        }
    }

    void TryAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;

        lastAttackTime = Time.time;
        comboTimer = Time.time;
        currentCombo = (currentCombo + 1) % comboCount;

        // Aquí puedes llamar a la animación correspondiente al combo
        // animator.SetTrigger("Attack" + currentCombo);

        // Detección de enemigos en un área frente al jugador
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position + transform.forward * attackRange, 1f, enemyLayer);
        foreach (var enemy in hitEnemies)
        {
            // Destruye al enemigo si lo toca
           Destroy(enemy.gameObject);
        }

        // Aquí puedes agregar efectos visuales/sonoros
    }

    // Visualiza el área de ataque en el editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * attackRange, 1f);
    }
}
