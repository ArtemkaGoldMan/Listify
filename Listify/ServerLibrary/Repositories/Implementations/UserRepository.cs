using System;
using BaseLibrary.Entities;
using Microsoft.EntityFrameworkCore;
using ServerLibrary.Data;
using ServerLibrary.Repositories.Contracts;

namespace ServerLibrary.Repositories.Implementations
{
	public class UserRepository : IUserRepository
	{
        private readonly AppDbContext _context;

		public UserRepository(AppDbContext context)
		{
            _context = context;
		}

        public async Task<User> CreateUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public Task<bool> DeleteUserAsync(int userId)
        {
            throw new NotImplementedException();
        }

        public async Task<User> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.ListOfContent)
                .Include(u => u.ListOfTags)
                .FirstOrDefaultAsync(u => u.UserID == userId);
        }

        public async Task<User> GetUserByTelegramUserIdAsync(string telegramUserId)
        {
            return await _context.Users
                .Include(u => u.ListOfContent)
                .Include(u => u.ListOfTags)
                .FirstOrDefaultAsync(u => u.TelegramUserID == telegramUserId);
        }

        public async Task<User> UpdateUserAsync(int userId, User user)
        {
            var existingUser = await GetUserByIdAsync(userId);
            if (existingUser == null) return null;

            existingUser.TelegramUserID = user.TelegramUserID; // there can be added other changes if i wi

            _context.Users.Update(existingUser);
            await _context.SaveChangesAsync();
            return existingUser;
        }

    }
}

