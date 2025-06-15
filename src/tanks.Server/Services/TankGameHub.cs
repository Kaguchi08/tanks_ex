using MagicOnion.Server.Hubs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tanks.Shared;
using MagicOnion;

namespace Tanks.Server.Services
{
    public class TankGameHub : StreamingHubBase<ITankGameHub, ITankGameHubReceiver>, ITankGameHub
    {
        // 接続ID -> プレイヤー情報
        static Dictionary<string, TankGamePlayer> players = new Dictionary<string, TankGamePlayer>();
        static int nextPlayerId = 0;
        static bool gameStarted = false; // ゲーム開始状態を追跡

        // 現在のプレイヤーとルーム
        TankGamePlayer currentPlayer = default!;
        IGroup<ITankGameHubReceiver> room = default!;

        public async Task<bool> JoinAsync(string playerName)
        {
            var connectionId = Context.ContextId.ToString();
            
            // 2人対戦用に PlayerID を 1-2 の範囲で割り当て
            int assignedPlayerID = players.Count + 1;
            if (assignedPlayerID > 2)
            {
                // 2人を超える場合は観戦者として扱う（現在は拒否）
                Console.WriteLine($"満員のため参加を拒否: {playerName}");
                return false;
            }
            
            currentPlayer = new TankGamePlayer
            {
                ConnectionId = connectionId,
                PlayerID = assignedPlayerID,
                PlayerName = playerName
            };
            players[connectionId] = currentPlayer;
            
            Console.WriteLine($"プレイヤー参加: PlayerID={assignedPlayerID}, Name={playerName}, 現在の参加者数={players.Count}");

            // ルームに参加（IGroup<ITankGameHubReceiver>を返す）
            room = await Group.AddAsync("GameRoom");
            
            // 既存プレイヤーの情報を新規参加者に送信
            foreach (var existingPlayer in players.Values)
            {
                if (existingPlayer.PlayerID != currentPlayer.PlayerID)
                {
                    // NOTE: Context.Receiver は MagicOnion v7 では存在しないため一旦送信をスキップ
                }
            }
            
            // 全員に新規参加者の通知
            room.All.OnJoin(currentPlayer.PlayerID, currentPlayer.PlayerName);
            
            if (players.Count >= 2 && !gameStarted)
            {
                // プレイヤーが2人以上集まったら初回のみゲーム開始通知
                gameStarted = true;
                Console.WriteLine($"ゲーム開始: {players.Count}人のプレイヤーが参加");
                room.All.OnGameStart();
            }
            else if (gameStarted)
            {
                // 既にゲームが開始されている場合、新規参加者にのみゲーム開始を通知
                Console.WriteLine($"遅れて参加: PlayerId={currentPlayer.PlayerID}にゲーム開始を通知");
                // NOTE: Context.Receiver は MagicOnion v7 では存在しないため一旦送信をスキップ
            }
            else
            {
                Console.WriteLine($"プレイヤー待機中: {players.Count}/2人");
            }
            return true;
        }

        public async Task LeaveAsync()
        {
            // ルームから退出
            await room.RemoveAsync(Context);
            // 全員に退出通知
            room.All.OnLeave(currentPlayer.PlayerID);
            Console.WriteLine($"プレイヤー退出: PlayerID={currentPlayer.PlayerID}, Name={currentPlayer.PlayerName}");
            players.Remove(currentPlayer.ConnectionId);
            
            // プレイヤーが2人未満になったらゲーム状態をリセット
            if (players.Count < 2)
            {
                gameStarted = false;
                Console.WriteLine($"プレイヤーが不足、ゲーム状態をリセット: {players.Count}/2人");
            }
            
            // PlayerIDが再利用できるよう、nextPlayerIdをリセット（必要に応じて）
            if (players.Count == 0)
            {
                nextPlayerId = 0;
                Console.WriteLine("全プレイヤーが退出、PlayerIDカウンターをリセット");
            }
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