using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace Game.Combat
{
    [DefaultExecutionOrder(-100)]
    public class AttackInputBridge : MonoBehaviour
    {
        public event Action OnAttack;

        [Tooltip("Si se asigna, se usará esta referencia en lugar del PlayerInput.actions['Attack']")]
        public InputActionReference attackActionRef;

        private PlayerInput playerInput;
        private InputAction attackAction;

        void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            if (attackActionRef != null)
            {
                attackAction = attackActionRef.action;
            }
            else if (playerInput != null && playerInput.actions != null)
            {
                attackAction = playerInput.actions["Attack"];
            }
        }

        void OnEnable()
        {
            if (attackAction != null)
            {
                attackAction.performed += OnPerformed;
                if (!attackAction.enabled) attackAction.Enable();
            }
        }

        void OnDisable()
        {
            if (attackAction != null)
                attackAction.performed -= OnPerformed;
        }

        private void OnPerformed(InputAction.CallbackContext ctx)
        {
            OnAttack?.Invoke();
        }
    }
}
