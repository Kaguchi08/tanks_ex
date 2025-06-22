using System;
using MessagePack;

namespace Tanks.Shared.Models
{
    public enum UserStatus
    {
        Active = 0,     // 通常
        Suspended = 1,  // 停止
        Banned = 2      // 永久停止
    }

    [MessagePackObject]
    public class User
    {
        [Key(0)]
        public int UserId { get; set; }

        [Key(1)]
        public string HandleName { get; set; } = string.Empty;

        [Key(2)]
        public int WinCount { get; set; } = 0;

        [Key(3)]
        public int LoseCount { get; set; } = 0;

        [Key(4)]
        public UserStatus Status { get; set; } = UserStatus.Active;

        [Key(5)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Key(6)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [IgnoreMember]
        public int TotalGames => WinCount + LoseCount;

        [IgnoreMember]
        public double WinRate => TotalGames > 0 ? (double)WinCount / TotalGames : 0.0;

        [IgnoreMember]
        public bool IsActive => Status == UserStatus.Active;

        public bool IsValidHandleName()
        {
            return !string.IsNullOrWhiteSpace(HandleName) && HandleName.Length <= 100;
        }
    }
}