using UnityEngine;
using Complete.Interfaces;
using UniRx;
using System;

namespace Complete.Input
{
    /// <summary>
    /// ローカルプレイヤーの入力を提供するクラス
    /// </summary>
    public class LocalInputProvider : IInputProvider, IDisposable
    {
        public IReadOnlyReactiveProperty<float> MovementInput => _movementInput;
        public IReadOnlyReactiveProperty<float> TurnInput => _turnInput;
        public IReadOnlyReactiveProperty<bool> FireButton => _fireButton;

        private readonly ReactiveProperty<float> _movementInput = new ReactiveProperty<float>();
        private readonly ReactiveProperty<float> _turnInput = new ReactiveProperty<float>();
        private readonly ReactiveProperty<bool> _fireButton = new ReactiveProperty<bool>();

        private readonly string _movementAxisName;
        private readonly string _turnAxisName;
        private readonly string _fireButtonName;

        private readonly IDisposable _updateSubscription;

        public LocalInputProvider(int playerNumber)
        {
            // PlayerIDを適切な入力番号にマッピング（1-4の範囲）
            int inputNumber = Mathf.Clamp(playerNumber, 1, 4);
            
            _movementAxisName = "Vertical" + inputNumber;
            _turnAxisName = "Horizontal" + inputNumber;
            _fireButtonName = "Fire" + inputNumber;

            // MonoBehaviourに依存しないUpdateループ
            _updateSubscription = Observable.EveryUpdate()
                .Subscribe(_ =>
                {
                    _movementInput.Value = UnityEngine.Input.GetAxis(_movementAxisName);
                    _turnInput.Value = UnityEngine.Input.GetAxis(_turnAxisName);
                    _fireButton.Value = UnityEngine.Input.GetButton(_fireButtonName);
                });
        }

        public void Dispose()
        {
            _updateSubscription?.Dispose();
            _movementInput?.Dispose();
            _turnInput?.Dispose();
            _fireButton?.Dispose();
        }
    }
} 