using System.Threading;
using System.Threading.Tasks;

namespace Tanks.Server.Services
{
    public class GameStateService : IGameStateService
    {
        private volatile bool _gameStarted = false;
        private readonly object _lock = new();

        public bool IsGameStarted => _gameStarted;

        public Task StartGameAsync()
        {
            lock (_lock)
            {
                _gameStarted = true;
            }
            return Task.CompletedTask;
        }

        public Task ResetGameAsync()
        {
            lock (_lock)
            {
                _gameStarted = false;
            }
            return Task.CompletedTask;
        }
    }
}