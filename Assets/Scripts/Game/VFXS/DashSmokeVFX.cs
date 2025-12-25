using UnityEngine;

public class DashSmokeVFX : MonoBehaviour
{
    public ParticleSystem dashParticleSystem;

    public void PlayDashSmoke(Vector3 dashDirection)
    {
        float[] angles = { 0, 50, 100, -50, -100,};
        Quaternion dashRotation = Quaternion.LookRotation(dashDirection, Vector3.up);
        for (int i = 0; i < angles.Length; i++)
        {
            var emitParams = new ParticleSystem.EmitParams();
            float angleRad = angles[i] * Mathf.Deg2Rad;
            Vector3 localDirection = new Vector3(-Mathf.Sin(angleRad), 1, -Mathf.Cos(angleRad)).normalized;
            Vector3 finalDirection = dashRotation * localDirection - dashDirection * 0.5f;
            emitParams.velocity = finalDirection * 2.5f;
            dashParticleSystem.Emit(emitParams, 1);
        }
    }

    private void Update()
    {
        // Si el sistema de partículas ya no está reproduciéndose y no quedan partículas vivas, destruye el GameObject
        if (dashParticleSystem != null && !dashParticleSystem.IsAlive())
        {
            Destroy(gameObject);
        }
    }
}
