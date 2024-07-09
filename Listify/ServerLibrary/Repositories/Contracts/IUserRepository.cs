using BaseLibrary.DTOs;
using System.Threading.Tasks;

namespace ServerLibrary.Repositories.Contracts
{
    public interface IUserRepository
    {
        Task<UserDTO> CreateUserAsync(UserDTO user);
        Task<UserDTO> GetUserByIdAsync(int userId);
        Task<UserDTO> GetUserByTelegramUserIdAsync(string telegramUserId);
        Task<bool> DeleteUserAsync(int userId);
    }
}
