namespace Complete.Interfaces
{
    /// <summary>
    /// タンクの制御機能を抽象化するインターフェース
    /// 依存関係逆転の原則に従って具体的な実装から分離
    /// </summary>
    public interface ITankController
    {
        /// <summary>
        /// タンクの制御を有効にする
        /// </summary>
        void Enable();
        
        /// <summary>
        /// タンクの制御を無効にする
        /// </summary>
        void Disable();
        
        /// <summary>
        /// タンクをリセットする
        /// </summary>
        void Reset();
    }
} 