using MagicOnion.Server.Hubs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tanks.Shared;

namespace Tanks.Server.Services
{
    public class TankGameHub : StreamingHubBase<ITankGameHub, ITankGameHubReceiver>, ITankGameHub
    {
        // 接続ID -> プレイヤー情報
        static Dictionary<string, TankGamePlayer> players = new Dictionary<string, TankGamePlayer>();
        static int nextPlayerId = 0;

        // 現在のプレイヤーとルーム
        TankGamePlayer currentPlayer = default!;
        IGroup<ITankGameHubReceiver> room = default!;

        public async Task<bool> JoinAsync(string playerName)
        {
            var connectionId = Context.ContextId.ToString();
            currentPlayer = new TankGamePlayer
            {
                ConnectionId = connectionId,
                PlayerID = Interlocked.Increment(ref nextPlayerId),
                PlayerName = playerName
            };
            players[connectionId] = currentPlayer;

            // ルームに参加（IGroup<ITankGameHubReceiver>を返す）
            room = await Group.AddAsync("GameRoom");
            // 全員に参加通知
            room.All.OnJoin(currentPlayer.PlayerID, currentPlayer.PlayerName);
            if (players.Count == 2)
            {
                // 全員にゲーム開始通知
                room.All.OnGameStart();
            }
            return true;
        }

        public async Task LeaveAsync()
        {
            // ルームから退出
            await room.RemoveAsync(Context);
            // 全員に退出通知
            room.All.OnLeave(currentPlayer.PlayerID);
            players.Remove(currentPlayer.ConnectionId);
        }

        public Task<bool> UpdatePositionAsync(TankPositionData positionData)
        {
            // プレイヤーIDを設定して全員に位置更新通知
            positionData.PlayerID = currentPlayer.PlayerID;
            room.All.OnUpdatePosition(positionData);
            return Task.FromResult(true);
        }

        public Task<bool> FireAsync(int playerID)
        {
            // 全員に発射通知
            room.All.OnFire(playerID);
            return Task.FromResult(true);
        }

        protected override ValueTask OnDisconnected()
        {
            LeaveAsync().Wait();
            return CompletedTask;
        }
    }

    public class TankGamePlayer
    {
        public string ConnectionId { get; set; } = string.Empty;
        public int PlayerID { get; set; }
        public string PlayerName { get; set; } = string.Empty;
    }
} 