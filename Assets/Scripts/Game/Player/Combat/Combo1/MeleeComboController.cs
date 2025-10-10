using UnityEngine;

namespace Game.Combat
{
    public class MeleeComboController : MonoBehaviour
    {
        private float attackCooldown;
        private int comboCount;
        private float comboResetTime;

        private int currentStep = 0;
        private float lastAttackTime = -999f;
        private float lastComboTime = -999f;

        public void Configure(float cooldown, int count, float resetTime)
        {
            attackCooldown = Mathf.Max(0f, cooldown);
            comboCount = Mathf.Max(1, count);
            comboResetTime = Mathf.Max(0f, resetTime);
        }

        public void Tick()
        {
            if (currentStep > 0 && Time.time - lastComboTime > comboResetTime)
            {
                currentStep = 0;
            }
        }

        public bool CanAttack()
        {
            return Time.time - lastAttackTime >= attackCooldown;
        }

        // Returns step index (0..comboCount-1) or -1 if still on cooldown
        public int StartAttackAndGetStep()
        {
            if (!CanAttack()) return -1;

            lastAttackTime = Time.time;
            lastComboTime = Time.time;
            int step = currentStep;
            currentStep = (currentStep + 1) % comboCount;
            return step;
        }

        public void ResetCombo()
        {
            currentStep = 0;
        }

        public int PeekStep() => currentStep;
    }
}
