using UnityEngine;
using UnityEngine.InputSystem;

namespace VrAction.Game.Character
{
    /// <summary>
    /// Controller input (FR-018): left stick = move, A = jump, trigger = attack, B = dodge.
    /// Keyboard fallbacks (WASD, Space, J, K) allow testing without a headset.
    /// </summary>
    public sealed class CharacterInput : MonoBehaviour
    {
        public SmallHero Hero;
        public System.Func<CombatController> Combat;

        InputAction _move, _jump, _attack, _dodge;

        void OnEnable()
        {
            _move = new InputAction("move", InputActionType.Value);
            _move.AddBinding("<XRController>{LeftHand}/thumbstick");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            _jump = new InputAction("jump", InputActionType.Button);
            _jump.AddBinding("<XRController>{RightHand}/primaryButton");
            _jump.AddBinding("<Keyboard>/space");
            _attack = new InputAction("attack", InputActionType.Button);
            _attack.AddBinding("<XRController>{RightHand}/triggerPressed");
            _attack.AddBinding("<Keyboard>/j");
            _dodge = new InputAction("dodge", InputActionType.Button);
            _dodge.AddBinding("<XRController>{RightHand}/secondaryButton");
            _dodge.AddBinding("<Keyboard>/k");
            _move.Enable(); _jump.Enable(); _attack.Enable(); _dodge.Enable();
        }

        void OnDisable()
        {
            _move?.Disable(); _jump?.Disable(); _attack?.Disable(); _dodge?.Disable();
        }

        void Update()
        {
            if (Hero == null) return;
            Hero.MoveInput = _move.ReadValue<Vector2>();
            Hero.JumpInput = _jump.IsPressed();
            var combat = Combat?.Invoke();
            if (combat == null) return;
            if (_attack.WasPressedThisFrame()) combat.Attack();
            if (_dodge.WasPressedThisFrame()) combat.Dodge();
        }
    }
}
