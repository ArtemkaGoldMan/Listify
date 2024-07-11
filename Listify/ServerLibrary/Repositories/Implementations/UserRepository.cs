using BaseLibrary.DTOs;
using BaseLibrary.Entities;
using Microsoft.EntityFrameworkCore;
using ServerLibrary.Data;
using ServerLibrary.Repositories.Contracts;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ServerLibrary.Repositories.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UserDTO> CreateUserAsync(UserDTO user)
        {
            var newUser = new User
            {
                TelegramUserID = user.TelegramUserID,
                ListOfContent = new ListOfContent(),
                ListOfTags = new ListOfTags()
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            user.UserID = newUser.UserID; 

            return user;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<UserDTO> GetUserByIdAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.ListOfContent)
                .Include(u => u.ListOfTags)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
                return null;

            return new UserDTO
            {
                UserID = user.UserID,
                TelegramUserID = user.TelegramUserID
                //include ListOfContentDTO and ListOfTagsDTO here if needed
            };
        }

        public async Task<UserDTO> GetUserByTelegramUserIdAsync(string telegramUserId)
        {
            var user = await _context.Users
                .Include(u => u.ListOfContent)
                .Include(u => u.ListOfTags)
                .FirstOrDefaultAsync(u => u.TelegramUserID == telegramUserId);

            if (user == null)
                return null;

            return new UserDTO
            {
                UserID = user.UserID,
                TelegramUserID = user.TelegramUserID
                //include ListOfContentDTO and ListOfTagsDTO here if needed
            };
        }
    }
}
