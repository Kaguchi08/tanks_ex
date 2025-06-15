using MagicOnion.Server.Hubs;
using System.Threading.Tasks;
using Tanks.Shared;
using MagicOnion;
using Microsoft.Extensions.Logging;

namespace Tanks.Server.Services
{
    public class TankGameHub : StreamingHubBase<ITankGameHub, ITankGameHubReceiver>, ITankGameHub
    {
        private readonly IPlayerManagerService _playerManager;
        private readonly IGameStateService _gameState;
        private readonly ILogger<TankGameHub> _logger;

        private TankGamePlayer? _currentPlayer;
        private IGroup<ITankGameHubReceiver>? _room;

        public TankGameHub(
            IPlayerManagerService playerManager,
            IGameStateService gameState,
            ILogger<TankGameHub> logger)
        {
            _playerManager = playerManager;
            _gameState = gameState;
            _logger = logger;
        }

        public async Task<bool> JoinAsync(string playerName)
        {
            var connectionId = Context.ContextId.ToString();
            
            if (!await _playerManager.TryAddPlayerAsync(connectionId, playerName))
            {
                _logger.LogWarning("満員のため参加を拒否: {PlayerName}", playerName);
                return false;
            }

            _currentPlayer = _playerManager.GetPlayer(connectionId);
            if (_currentPlayer == null)
            {
                _logger.LogError("プレイヤー追加後に取得できませんでした: {ConnectionId}", connectionId);
                return false;
            }

            _logger.LogInformation("プレイヤー参加: PlayerID={PlayerId}, Name={PlayerName}, 現在の参加者数={PlayerCount}",
                _currentPlayer.PlayerID, _currentPlayer.PlayerName, _playerManager.GetPlayerCount());

            _room = await Group.AddAsync("GameRoom");
            
            // 全員に新規参加者の通知
            _room.All.OnJoin(_currentPlayer.PlayerID, _currentPlayer.PlayerName);
            
            if (_playerManager.IsGameReady() && !_gameState.IsGameStarted)
            {
                await _gameState.StartGameAsync();
                _logger.LogInformation("ゲーム開始: {PlayerCount}人のプレイヤーが参加", _playerManager.GetPlayerCount());
                _room.All.OnGameStart();
            }
            else if (_gameState.IsGameStarted)
            {
                _logger.LogInformation("遅れて参加: PlayerId={PlayerId}にゲーム開始を通知", _currentPlayer.PlayerID);
            }
            else
            {
                _logger.LogInformation("プレイヤー待機中: {PlayerCount}/2人", _playerManager.GetPlayerCount());
            }
            
            return true;
        }

        public async Task LeaveAsync()
        {
            if (_currentPlayer == null || _room == null)
            {
                return;
            }

            await _room.RemoveAsync(Context);
            _room.All.OnLeave(_currentPlayer.PlayerID);
            
            _logger.LogInformation("プレイヤー退出: PlayerID={PlayerId}, Name={PlayerName}",
                _currentPlayer.PlayerID, _currentPlayer.PlayerName);
            
            await _playerManager.RemovePlayerAsync(_currentPlayer.ConnectionId);
            
            if (!_playerManager.IsGameReady() && _gameState.IsGameStarted)
            {
                await _gameState.ResetGameAsync();
                _logger.LogInformation("プレイヤーが不足、ゲーム状態をリセット: {PlayerCount}/2人", _playerManager.GetPlayerCount());
            }
        }

        public Task<bool> UpdatePositionAsync(TankPositionData positionData)
        {
            if (_currentPlayer == null || _room == null)
            {
                return Task.FromResult(false);
            }

            positionData.PlayerID = _currentPlayer.PlayerID;
            _room.All.OnUpdatePosition(positionData);
            return Task.FromResult(true);
        }

        public Task<bool> FireAsync(ShellFireData fireData)
        {
            if (_currentPlayer == null || _room == null)
            {
                return Task.FromResult(false);
            }

            fireData.PlayerID = _currentPlayer.PlayerID;
            _room.All.OnFire(fireData);
            return Task.FromResult(true);
        }

        public Task<bool> UpdateHealthAsync(int playerID, float currentHealth)
        {
            if (_currentPlayer == null || _room == null)
            {
                return Task.FromResult(false);
            }

            _room.All.OnHealthUpdate(playerID, currentHealth);
            return Task.FromResult(true);
        }

        public Task<bool> ApplyExplosionForceAsync(ExplosionForceData explosionData)
        {
            if (_currentPlayer == null || _room == null)
            {
                return Task.FromResult(false);
            }

            _room.All.OnExplosionForce(explosionData);
            return Task.FromResult(true);
        }

        public Task<bool> NotifyGameResultAsync(GameResultData gameResult)
        {
            if (_currentPlayer == null || _room == null)
            {
                return Task.FromResult(false);
            }

            _room.All.OnGameResult(gameResult);
            return Task.FromResult(true);
        }

        protected override async ValueTask OnDisconnected()
        {
            try
            {
                await LeaveAsync();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "OnDisconnected中にエラーが発生しました");
            }
        }
    }

    public class TankGamePlayer
    {
        public string ConnectionId { get; set; } = string.Empty;
        public int PlayerID { get; set; }
        public string PlayerName { get; set; } = string.Empty;
    }
} 