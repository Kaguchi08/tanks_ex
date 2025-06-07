namespace Complete.Interfaces
{
    /// <summary>
    /// 入力処理を抽象化するインターフェース
    /// 単一責任の原則に従って入力処理を分離
    /// </summary>
    public interface IInputHandler
    {
        /// <summary>
        /// 移動入力値を取得
        /// </summary>
        float MovementInput { get; }
        
        /// <summary>
        /// 回転入力値を取得
        /// </summary>
        float TurnInput { get; }
        
        /// <summary>
        /// 射撃ボタンが押された瞬間
        /// </summary>
        bool FireButtonDown { get; }
        
        /// <summary>
        /// 射撃ボタンが押されている間
        /// </summary>
        bool FireButtonHeld { get; }
        
        /// <summary>
        /// 射撃ボタンが離された瞬間
        /// </summary>
        bool FireButtonUp { get; }
        
        /// <summary>
        /// 入力を更新
        /// </summary>
        void UpdateInput();
    }
} 