using System;
using BaseLibrary.DTOs;
using BaseLibrary.Entities;

namespace ServerLibrary.Repositories.Contracts
{
	public interface ITagRepository
	{
        Task<TagDTO> CreateTagAsync(int userId, TagDTO tagDto);
        Task<IEnumerable<TagDTO>> GetTagsByUserIdAsync(int userId);
        Task<TagDTO> GetTagByIdAsync(int userId, int tagId);
        Task<TagDTO> UpdateTagAsync(int userId, int tagId, TagDTO tagDto);
        Task<bool> DeleteTagAsync(int userId, int tagId);

        //methods for associating and dissociating tags with/from content
        Task<bool> AddTagToContentAsync(int userId, int contentId, int tagId);
        Task<bool> RemoveTagFromContentAsync(int userId, int contentId, int tagId);

        Task<bool> CanAddTagAsync(int userId);
    }
}

