using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tanks.Shared;

namespace Tanks.Server.Services
{
    public class PlayerManagerService : IPlayerManagerService
    {
        private readonly ConcurrentDictionary<string, TankGamePlayer> _players = new();
        private int _totalConnectionCount = 0;
        private const int MaxPlayers = 2;

        public Task<bool> TryAddPlayerAsync(string connectionId, string playerName)
        {
            _totalConnectionCount++;
            
            if (_players.Count >= MaxPlayers)
            {
                return Task.FromResult(false);
            }

            var assignedPlayerID = _players.Count + 1;
            var player = new TankGamePlayer
            {
                ConnectionId = connectionId,
                PlayerID = assignedPlayerID,
                PlayerName = playerName
            };

            return Task.FromResult(_players.TryAdd(connectionId, player));
        }

        public Task RemovePlayerAsync(string connectionId)
        {
            _players.TryRemove(connectionId, out _);
            return Task.CompletedTask;
        }

        public TankGamePlayer? GetPlayer(string connectionId)
        {
            return _players.TryGetValue(connectionId, out var player) ? player : null;
        }

        public IReadOnlyList<TankGamePlayer> GetAllPlayers()
        {
            return _players.Values.ToList();
        }

        public int GetPlayerCount()
        {
            return _players.Count;
        }

        public bool IsGameReady()
        {
            return _players.Count >= MaxPlayers;
        }

        public int GetActivePlayerCount()
        {
            return _players.Count;
        }

        public int GetTotalConnectionCount()
        {
            return _totalConnectionCount;
        }
    }
}