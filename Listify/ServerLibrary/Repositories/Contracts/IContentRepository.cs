using BaseLibrary.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServerLibrary.Repositories.Contracts
{
    public interface IContentRepository
    {
        Task<ContentDTO> CreateContentAsync(int userId, ContentDTO content);
        Task<IEnumerable<ContentDTO>> GetContentsByUserIdAsync(int userId);
        Task<ContentDTO> GetContentByIdAsync(int userId, int contentId);
        Task<ContentDTO> UpdateContentAsync(int userId, int contentId, ContentDTO content);
        Task<bool> DeleteContentAsync(int userId, int contentId);
    }
}
