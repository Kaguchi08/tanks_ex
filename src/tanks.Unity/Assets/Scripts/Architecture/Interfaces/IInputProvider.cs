using UniRx;

namespace Complete.Interfaces
{
    /// <summary>
    /// 入力ソースを抽象化するインターフェース
    /// ローカル、AI、ネットワークからの入力を同じように扱う
    /// </summary>
    public interface IInputProvider
    {
        IReadOnlyReactiveProperty<float> MovementInput { get; }
        IReadOnlyReactiveProperty<float> TurnInput { get; }
        IReadOnlyReactiveProperty<bool> FireButton { get; }
    }
} 