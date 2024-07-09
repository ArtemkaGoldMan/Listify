using System;
using BaseLibrary.Entities;

namespace ServerLibrary.Repositories.Contracts
{
	public interface ITagRepository
	{
        Task<Tag> CreateTagAsync(int userId, Tag tag);
        Task<IEnumerable<Tag>> GetTagsByUserIdAsync(int userId);
        Task<Tag> GetTagByIdAsync(int userId, int tagId);
        Task<Tag> UpdateTagAsync(int userId, int tagId, Tag tag);
        Task<bool> DeleteTagAsync(int userId, int tagId);
    }
}

