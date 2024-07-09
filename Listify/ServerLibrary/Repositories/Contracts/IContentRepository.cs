using System;
using BaseLibrary.Entities;

namespace ServerLibrary.Repositories.Contracts
{
	public interface IContentRepository
	{
        Task<Content> CreateContentAsync(int userId, Content content);
        Task<IEnumerable<Content>> GetContentsByUserIdAsync(int userId);
        Task<Content> GetContentByIdAsync(int userId, int contentId);
        Task<Content> UpdateContentAsync(int userId, int contentId, Content content);
        Task<bool> DeleteContentAsync(int userId, int contentId);
    }
}

