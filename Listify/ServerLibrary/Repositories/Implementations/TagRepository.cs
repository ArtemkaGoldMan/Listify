using System;
using System.Reflection.Metadata;
using BaseLibrary.DTOs;
using BaseLibrary.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServerLibrary.Data;
using ServerLibrary.Repositories.Contracts;

namespace ServerLibrary.Repositories.Implementations
{
	public class TagRepository : ITagRepository
	{
        private readonly AppDbContext _context;
        private readonly ILogger<ContentRepository> _logger;

        public TagRepository(AppDbContext context, ILogger<ContentRepository> logger)
		{
            _context = context;
            _logger = logger;
        }

        public async Task<TagDTO> CreateTagAsync(int userId, TagDTO tagDto)
        {
            var user = await _context.Users.Include(u => u.ListOfTags).FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null)
            {
                _logger.LogWarning($"User with ID {userId} not found.");
                return null!;
            }

            if (user.ListOfTags == null)
            {
                _logger.LogWarning($"User with ID {userId} does not have a list of content.");
                return null!;
            }

            var tag = new Tag
            {
                Name = tagDto.Name,
                ListOfTagsID = user.ListOfTags.ListOfTagsID
            };

            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();

            tagDto.TagID = tag.TagID;
            return tagDto;
        }

        public async Task<TagDTO> GetTagByIdAsync(int userId, int tagId)
        {
            var tag = await _context.Tags.Include(t => t.ListOfTags).FirstOrDefaultAsync(t => t.TagID == tagId && t.ListOfTags!.UserID == userId);
            if (tag == null)
            {
                _logger.LogWarning($"Content with ID {tagId} for user {userId} not found.");
                return null!;
            }

            return new TagDTO
            {
                TagID = tag.TagID,
                Name = tag.Name,
                ListOfTagsID = tag.ListOfTagsID
            };
        }

        public async Task<IEnumerable<TagDTO>> GetTagsByUserIdAsync(int userId)
        {
            var user = await _context.Users.Include(u => u.ListOfTags).ThenInclude(lt => lt!.Tags).FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null)
            {
                _logger.LogWarning($"User with ID {userId} not found.");
                return null!;
            }

            if (user.ListOfTags == null)
            {
                _logger.LogWarning($"User with ID {userId} does not have a list of content.");
                return null!;
            }

            var tags = user.ListOfTags.Tags!.Select(t => new TagDTO
            {
                TagID = t.TagID,
                Name = t.Name,
                ListOfTagsID = t.ListOfTagsID
            }).ToList();

            return tags;
        }

        public async Task<TagDTO> UpdateTagAsync(int userId, int tagId, TagDTO tagDto)
        {
            var tag = await _context.Tags.Include(t => t.ListOfTags).FirstOrDefaultAsync(t => t.TagID == tagId && t.ListOfTags!.UserID == userId);
            if (tag == null)
            {
                _logger.LogWarning($"Content with ID {tagId} for user {userId} not found.");
                return null!;
            }

            tag.Name = tagDto.Name;

            await _context.SaveChangesAsync();

            return new TagDTO
            {
                TagID = tag.TagID,
                Name = tag.Name,
                ListOfTagsID = tag.ListOfTagsID
            };
        }

        public async Task<bool> DeleteTagAsync(int userId, int tagId)
        {
            var tag = await _context.Tags.Include(t => t.ListOfTags).FirstOrDefaultAsync(t => t.TagID == tagId && t.ListOfTags!.UserID == userId);
            if (tag == null)
            {
                _logger.LogWarning($"Content with ID {tagId} for user {userId} not found.");
                return false;
            }

            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddTagToContentAsync(int userId, int contentId, int tagId)
        {
            var user = await _context.Users.Include(u => u.ListOfContent)
                                           .ThenInclude(lc => lc!.Contents)
                                           .Include(u => u.ListOfTags)
                                           .ThenInclude(lt => lt!.Tags)
                                           .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
                return false;

            var content = user.ListOfContent?.Contents?.FirstOrDefault(c => c.ContentID == contentId);
            var tag = user.ListOfTags?.Tags?.FirstOrDefault(t => t.TagID == tagId);

            if (content == null || tag == null)
                return false;

            var contentTag = new ContentTag
            {
                ContentID = contentId,
                TagID = tagId
            };

            _context.ContentTags.Add(contentTag);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveTagFromContentAsync(int userId, int contentId, int tagId)
        {
            var contentTag = await _context.ContentTags
                .Include(ct => ct.Content)
                .Include(ct => ct.Tag)
                .Where(ct => ct.Content!.ListOfContent!.UserID == userId && ct.ContentID == contentId && ct.TagID == tagId)
                .FirstOrDefaultAsync();

            if (contentTag == null)
                return false;

            _context.ContentTags.Remove(contentTag);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CanAddTagAsync(int userId)
        {
            var user = await _context.Users.Include(u => u.ListOfTags)
                                           .ThenInclude(lt => lt!.Tags)
                                           .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null || user.IsUnlimited) return true;

            var tagCount = user.ListOfTags?.Tags?.Count() ?? 0;
            return tagCount < user.MaxTags;
        }
    }
}

