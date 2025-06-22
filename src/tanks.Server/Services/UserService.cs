using Microsoft.EntityFrameworkCore;
using Tanks.Server.Data;
using Tanks.Shared.Models;

namespace Tanks.Server.Services
{
    public class UserService : IUserService
    {
        private readonly TankGameDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(TankGameDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                // データベース接続確認
                if (!await _context.Database.CanConnectAsync())
                {
                    _logger.LogWarning("Database connection is not available");
                    return new List<User>();
                }

                return await _context.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all users - Database may not be available");
                return new List<User>();
            }
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by ID: {UserId}", userId);
                return null;
            }
        }

        public async Task<User> CreateUserAsync(User user)
        {
            try
            {
                user.CreatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("User created: {HandleName} (ID: {UserId})", user.HandleName, user.UserId);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {HandleName}", user.HandleName);
                throw;
            }
        }

        public async Task<User?> UpdateUserAsync(User user)
        {
            try
            {
                var existingUser = await _context.Users.FindAsync(user.UserId);
                if (existingUser == null)
                {
                    return null;
                }

                existingUser.HandleName = user.HandleName;
                existingUser.WinCount = user.WinCount;
                existingUser.LoseCount = user.LoseCount;
                existingUser.Status = user.Status;
                existingUser.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("User updated: {HandleName} (ID: {UserId})", user.HandleName, user.UserId);
                return existingUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", user.UserId);
                return null;
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return false;
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("User deleted: {HandleName} (ID: {UserId})", user.HandleName, user.UserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", userId);
                return false;
            }
        }

        public async Task<List<User>> GetUsersByStatusAsync(UserStatus status)
        {
            try
            {
                return await _context.Users
                    .Where(u => u.Status == status)
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users by status: {Status}", status);
                return new List<User>();
            }
        }

        public async Task<User?> GetUserByHandleNameAsync(string handleName)
        {
            try
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.HandleName == handleName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by handle name: {HandleName}", handleName);
                return null;
            }
        }
    }
}