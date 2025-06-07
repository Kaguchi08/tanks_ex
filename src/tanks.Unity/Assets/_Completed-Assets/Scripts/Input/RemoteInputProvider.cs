using Complete.Interfaces;
using UniRx;
using System;

namespace Complete.Input
{
    /// <summary>
    /// ネットワーク越しのリモートプレイヤーの入力を提供するクラス
    /// </summary>
    public class RemoteInputProvider : IInputProvider, IDisposable
    {
        public IReadOnlyReactiveProperty<float> MovementInput => _movementInput;
        public IReadOnlyReactiveProperty<float> TurnInput => _turnInput;
        public IReadOnlyReactiveProperty<bool> FireButton => _fireButton;

        // NOTE: ReactivePropertyはスレッドセーフではないため、
        // MagicOnionのバックグラウンドスレッドから直接値をセットしないこと。
        // MainThreadDispatcherなどを介してメインスレッドで値を更新する必要がある。
        private readonly ReactiveProperty<float> _movementInput = new ReactiveProperty<float>();
        private readonly ReactiveProperty<float> _turnInput = new ReactiveProperty<float>();
        private readonly ReactiveProperty<bool> _fireButton = new ReactiveProperty<bool>();

        public RemoteInputProvider()
        {
            // TODO: MagicOnionのHubからストリームを受信し、各ReactivePropertyを更新する
        }
        
        /// <summary>
        /// ネットワーク経由で受信した入力値を設定する
        /// </summary>
        public void SetInput(float movement, float turn, bool fire)
        {
            _movementInput.Value = movement;
            _turnInput.Value = turn;
            _fireButton.Value = fire;
        }

        public void Dispose()
        {
            _movementInput?.Dispose();
            _turnInput?.Dispose();
            _fireButton?.Dispose();
        }
    }
} 