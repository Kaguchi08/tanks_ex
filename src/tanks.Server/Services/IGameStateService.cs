using System.Threading.Tasks;

namespace Tanks.Server.Services
{
    public interface IGameStateService
    {
        bool IsGameStarted { get; }
        Task StartGameAsync();
        Task ResetGameAsync();
    }
}