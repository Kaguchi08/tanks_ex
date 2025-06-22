using System.Collections.Generic;
using System.Threading.Tasks;
using Tanks.Shared;

namespace Tanks.Server.Services
{
    public interface IPlayerManagerService
    {
        Task<bool> TryAddPlayerAsync(string connectionId, string playerName);
        Task RemovePlayerAsync(string connectionId);
        TankGamePlayer? GetPlayer(string connectionId);
        IReadOnlyList<TankGamePlayer> GetAllPlayers();
        int GetPlayerCount();
        bool IsGameReady();
        int GetActivePlayerCount();
        int GetTotalConnectionCount();
    }
}