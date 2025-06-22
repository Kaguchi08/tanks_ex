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

        public string GetCurrentState()
        {
            return _gameStarted ? "実行中" : "停止中";
        }

        public void StartGame()
        {
            lock (_lock)
            {
                _gameStarted = true;
            }
        }

        public void StopGame()
        {
            lock (_lock)
            {
                _gameStarted = false;
            }
        }

        public void ResetGame()
        {
            lock (_lock)
            {
                _gameStarted = false;
            }
        }
    }
}