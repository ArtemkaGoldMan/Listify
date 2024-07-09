using System;
using BaseLibrary.Entities;

namespace ServerLibrary.Repositories.Contracts
{
	public interface IUserRepository
	{
        Task<User> CreateUserAsync(User user);
        Task<User> GetUserByIdAsync(int userId);
        Task<User> GetUserByTelegramUserIdAsync(string telegramUserId);
        Task<User> UpdateUserAsync(int userId, User user);
        Task<bool> DeleteUserAsync(int userId);
    }
}

