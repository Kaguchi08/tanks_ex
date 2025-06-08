using MagicOnion;
using MessagePack;
using System.Threading.Tasks;

namespace Tanks.Shared
{
    // StreamingHubのインターフェース
    public interface ITankGameHub : IStreamingHub<ITankGameHub, ITankGameHubReceiver>
    {
        // クライアントから呼び出されるサーバーメソッド
        Task<bool> JoinAsync(string playerName);
        Task LeaveAsync();
        Task<bool> UpdatePositionAsync(TankPositionData positionData);
        Task<bool> FireAsync(int playerID);
    }

    // クライアントが実装するインターフェース
    public interface ITankGameHubReceiver
    {
        // サーバーから呼び出されるクライアントメソッド
        void OnJoin(int playerID, string playerName);
        void OnLeave(int playerID);
        void OnGameStart();
        void OnUpdatePosition(TankPositionData positionData);
        void OnFire(int playerID);
    }

    // タンクの位置情報データ
    [MessagePackObject]
    public class TankPositionData
    {
        [Key(0)]
        public int PlayerID { get; set; }

        [Key(1)]
        public float PositionX { get; set; }

        [Key(2)]
        public float PositionY { get; set; }

        [Key(3)]
        public float PositionZ { get; set; }

        [Key(4)]
        public float RotationY { get; set; }
    }
} 