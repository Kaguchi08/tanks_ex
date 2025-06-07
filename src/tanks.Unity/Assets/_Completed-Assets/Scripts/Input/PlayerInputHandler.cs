using UnityEngine;
using Complete.Interfaces;
using UniRx;

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

        // 既存の値取得プロパティ
        public float MovementInput { get; private set; }
        public float TurnInput { get; private set; }
        public bool FireButtonDown { get; private set; }
        public bool FireButtonHeld { get; private set; }
        public bool FireButtonUp { get; private set; }

        // UniRx で利用できるリアクティブプロパティ
        public IReadOnlyReactiveProperty<float> MovementStream => _movementReactive;
        public IReadOnlyReactiveProperty<float> TurnStream => _turnReactive;
        public IReadOnlyReactiveProperty<bool> FireDownStream => _fireDownReactive;
        public IReadOnlyReactiveProperty<bool> FireHeldStream => _fireHeldReactive;
        public IReadOnlyReactiveProperty<bool> FireUpStream => _fireUpReactive;

        private readonly ReactiveProperty<float> _movementReactive = new ReactiveProperty<float>();
        private readonly ReactiveProperty<float> _turnReactive = new ReactiveProperty<float>();
        private readonly ReactiveProperty<bool> _fireDownReactive = new ReactiveProperty<bool>();
        private readonly ReactiveProperty<bool> _fireHeldReactive = new ReactiveProperty<bool>();
        private readonly ReactiveProperty<bool> _fireUpReactive = new ReactiveProperty<bool>();

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

            // Reactiveプロパティへ値を流す
            _movementReactive.Value = MovementInput;
            _turnReactive.Value = TurnInput;
            _fireDownReactive.Value = FireButtonDown;
            _fireHeldReactive.Value = FireButtonHeld;
            _fireUpReactive.Value = FireButtonUp;
        }
    }
} 