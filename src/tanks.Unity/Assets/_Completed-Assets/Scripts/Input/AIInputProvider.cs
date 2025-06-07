using UnityEngine;
using Complete.Interfaces;
using UniRx;
using System;

namespace Complete.Input
{
    /// <summary>
    /// AIの入力を提供するクラス
    /// </summary>
    public class AIInputProvider : IInputProvider, IDisposable
    {
        public IReadOnlyReactiveProperty<float> MovementInput => _movementInput;
        public IReadOnlyReactiveProperty<float> TurnInput => _turnInput;
        public IReadOnlyReactiveProperty<bool> FireButton => _fireButton;

        private readonly ReactiveProperty<float> _movementInput = new ReactiveProperty<float>();
        private readonly ReactiveProperty<float> _turnInput = new ReactiveProperty<float>();
        private readonly ReactiveProperty<bool> _fireButton = new ReactiveProperty<bool>();

        public AIInputProvider(/* AIのターゲットや設定などを引数で受け取る */)
        {
            // TODO: AIのロジックに基づいて各ReactivePropertyを更新する
            // 例: ターゲットに向かって移動、定期的に射撃など
        }

        public void Dispose()
        {
            _movementInput?.Dispose();
            _turnInput?.Dispose();
            _fireButton?.Dispose();
        }
    }
} 