using Tanks.Shared.Models;

namespace Tanks.Server.Services
{
    public interface IUserService
    {
        Task<List<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int userId);
        Task<User> CreateUserAsync(User user);
        Task<User?> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int userId);
        Task<List<User>> GetUsersByStatusAsync(UserStatus status);
        Task<User?> GetUserByHandleNameAsync(string handleName);
    }
}