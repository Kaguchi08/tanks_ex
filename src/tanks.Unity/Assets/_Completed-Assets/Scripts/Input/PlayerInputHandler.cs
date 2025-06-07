using UnityEngine;
using Complete.Interfaces;

namespace Complete.Input
{
    /// <summary>
    /// プレイヤー入力を処理するクラス
    /// 単一責任の原則に従って入力処理のみを担当
    /// </summary>
    public class PlayerInputHandler : IInputHandler
    {
        private readonly int _playerNumber;
        private readonly string _movementAxisName;
        private readonly string _turnAxisName;
        private readonly string _fireButtonName;

        public float MovementInput { get; private set; }
        public float TurnInput { get; private set; }
        public bool FireButtonDown { get; private set; }
        public bool FireButtonHeld { get; private set; }
        public bool FireButtonUp { get; private set; }

        public PlayerInputHandler(int playerNumber)
        {
            _playerNumber = playerNumber;
            _movementAxisName = "Vertical" + _playerNumber;
            _turnAxisName = "Horizontal" + _playerNumber;
            _fireButtonName = "Fire" + _playerNumber;
        }

        public void UpdateInput()   
        {
            MovementInput = UnityEngine.Input.GetAxis(_movementAxisName); // Explicitly reference UnityEngine.Input
            TurnInput = UnityEngine.Input.GetAxis(_turnAxisName); // Explicitly reference UnityEngine.Input
            FireButtonDown = UnityEngine.Input.GetButtonDown(_fireButtonName); // Explicitly reference UnityEngine.Input
            FireButtonHeld = UnityEngine.Input.GetButton(_fireButtonName); // Explicitly reference UnityEngine.Input
            FireButtonUp = UnityEngine.Input.GetButtonUp(_fireButtonName); // Explicitly reference UnityEngine.Input
        }
    }
} 