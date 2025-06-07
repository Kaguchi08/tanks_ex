using System.Collections;

namespace Complete.Interfaces
{
    /// <summary>
    /// ゲーム状態を管理するためのインターフェース
    /// 開放閉鎖の原則に従って新しい状態を簡単に追加できるようにする
    /// </summary>
    public interface IGameState
    {
        /// <summary>
        /// 状態に入る時の処理
        /// </summary>
        /// <returns>状態の実行コルーチン</returns>
        IEnumerator Enter();
        
        /// <summary>
        /// 状態から出る時の処理
        /// </summary>
        void Exit();
        
        /// <summary>
        /// 状態の名前
        /// </summary>
        string StateName { get; }
    }
} 