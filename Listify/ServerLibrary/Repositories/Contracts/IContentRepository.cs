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

        //method to get all tags for a specific content
        Task<IEnumerable<TagDTO>> GetTagsByContentIdAsync(int userId, int contentId);

        // Method to get all content for a user that is connected to a specific list of tags
        Task<IEnumerable<ContentDTO>> GetContentsByUserIdAndTagListAsync(int userId, IEnumerable<int> tagIds);
    }
}

