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
        Task<bool> FireAsync(ShellFireData fireData);
        Task<bool> UpdateHealthAsync(int playerID, float currentHealth);
        Task<bool> ApplyExplosionForceAsync(ExplosionForceData explosionData);
        Task<bool> NotifyGameResultAsync(GameResultData gameResult);
    }

    // クライアントが実装するインターフェース
    public interface ITankGameHubReceiver
    {
        // サーバーから呼び出されるクライアントメソッド
        void OnJoin(int playerID, string playerName);
        void OnLeave(int playerID);
        void OnGameStart();
        void OnUpdatePosition(TankPositionData positionData);
        void OnFire(ShellFireData fireData);
        void OnHealthUpdate(int playerID, float currentHealth);
        void OnExplosionForce(ExplosionForceData explosionData);
        void OnGameResult(GameResultData gameResult);
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

    [MessagePackObject]
    public class ShellFireData
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
        public float DirectionX { get; set; }

        [Key(5)]
        public float DirectionY { get; set; }

        [Key(6)]
        public float DirectionZ { get; set; }

        [Key(7)]
        public float Force { get; set; }
    }

    [MessagePackObject]
    public class ExplosionForceData
    {
        [Key(0)]
        public int TargetPlayerID { get; set; }

        [Key(1)]
        public float ExplosionX { get; set; }

        [Key(2)]
        public float ExplosionY { get; set; }

        [Key(3)]
        public float ExplosionZ { get; set; }

        [Key(4)]
        public float Force { get; set; }

        [Key(5)]
        public float Radius { get; set; }

        [Key(6)]
        public float Damage { get; set; }
    }

    [MessagePackObject]
    public class GameResultData
    {
        [Key(0)]
        public int WinnerPlayerID { get; set; }

        [Key(1)]
        public int RoundNumber { get; set; }

        [Key(2)]
        public bool IsGameEnd { get; set; }

        [Key(3)]
        public string WinnerName { get; set; } = string.Empty;
    }

    [MessagePackObject]
    public class TankGamePlayer
    {
        [Key(0)]
        public string ConnectionId { get; set; } = string.Empty;
        
        [Key(1)]
        public int PlayerID { get; set; }
        
        [Key(2)]
        public string PlayerName { get; set; } = string.Empty;
    }
} 