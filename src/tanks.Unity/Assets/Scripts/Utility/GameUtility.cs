using Complete;

namespace Complete.Utility
{
    /// <summary>
    /// ゲーム全体で使用される共通ユーティリティ関数
    /// </summary>
    public static class GameUtility
    {
        /// <summary>
        /// PlayerIDを1ベースから0ベースのインデックスに正規化
        /// </summary>
        /// <param name="playerId">1-2のPlayerID</param>
        /// <returns>0-1のインデックス、無効な場合は-1</returns>
        public static int NormalizePlayerIdToIndex(int playerId)
        {
            return (playerId >= 1 && playerId <= 2) ? playerId - 1 : -1;
        }

        /// <summary>
        /// インデックスを1ベースのPlayerIDに変換
        /// </summary>
        /// <param name="index">0-1のインデックス</param>
        /// <returns>1-2のPlayerID、無効な場合は-1</returns>
        public static int IndexToPlayerId(int index)
        {
            return (index >= 0 && index <= 1) ? index + 1 : -1;
        }

        /// <summary>
        /// 指定されたPlayerIDのタンクマネージャーを取得
        /// </summary>
        /// <param name="tanks">タンクマネージャー配列</param>
        /// <param name="playerId">PlayerID (1-2)</param>
        /// <returns>対応するTankManager、見つからない場合はnull</returns>
        public static TankManager GetTankManagerByPlayerId(TankManager[] tanks, int playerId)
        {
            if (tanks == null) return null;

            foreach (var tank in tanks)
            {
                if (tank != null && tank.m_PlayerID == playerId)
                {
                    return tank;
                }
            }
            return null;
        }

        /// <summary>
        /// 指定されたPlayerIDのタンクインスタンスを取得
        /// </summary>
        /// <param name="tanks">タンクマネージャー配列</param>
        /// <param name="playerId">PlayerID (1-2)</param>
        /// <returns>対応するGameObject、見つからない場合はnull</returns>
        public static UnityEngine.GameObject GetTankInstanceByPlayerId(TankManager[] tanks, int playerId)
        {
            var tankManager = GetTankManagerByPlayerId(tanks, playerId);
            return tankManager?.m_Instance;
        }
    }
}