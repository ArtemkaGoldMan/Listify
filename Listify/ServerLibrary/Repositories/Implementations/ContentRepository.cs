using BaseLibrary.DTOs;
using BaseLibrary.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServerLibrary.Data;
using ServerLibrary.Repositories.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServerLibrary.Repositories.Implementations
{
    public class ContentRepository : IContentRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ContentRepository> _logger;

        public ContentRepository(AppDbContext context, ILogger<ContentRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ContentDTO> CreateContentAsync(int userId, ContentDTO contentDto)
        {
            var user = await _context.Users.Include(u => u.ListOfContent).FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null)
            {
                _logger.LogWarning($"User with ID {userId} not found.");
                return null;
            }

            if (user.ListOfContent == null)
            {
                _logger.LogWarning($"User with ID {userId} does not have a list of content.");
                return null;
            }

            var content = new Content
            {
                Name = contentDto.Name,
                Description = contentDto.Description,
                ImageUrl = contentDto.ImageUrl,
                ListOfContentID = user.ListOfContent.ListOfContentID
            };

            _context.Contents.Add(content);
            await _context.SaveChangesAsync();

            contentDto.ContentID = content.ContentID;
            return contentDto;
        }

        public async Task<IEnumerable<ContentDTO>> GetContentsByUserIdAsync(int userId)
        {
            var user = await _context.Users.Include(u => u.ListOfContent).ThenInclude(lc => lc!.Contents).FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null)
            {
                _logger.LogWarning($"User with ID {userId} not found.");
                return null;
            }

            if (user.ListOfContent == null)
            {
                _logger.LogWarning($"User with ID {userId} does not have a list of content.");
                return null;
            }

            var contents = user.ListOfContent.Contents!.Select(c => new ContentDTO
            {
                ContentID = c.ContentID,
                Name = c.Name,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                ListOfContentID = c.ListOfContentID
            }).ToList();

            return contents;
        }

        public async Task<ContentDTO> GetContentByIdAsync(int userId, int contentId)
        {
            var content = await _context.Contents.Include(c => c.ListOfContent).FirstOrDefaultAsync(c => c.ContentID == contentId && c.ListOfContent!.UserID == userId);
            if (content == null)
            {
                _logger.LogWarning($"Content with ID {contentId} for user {userId} not found.");
                return null;
            }

            return new ContentDTO
            {
                ContentID = content.ContentID,
                Name = content.Name,
                Description = content.Description,
                ImageUrl = content.ImageUrl,
                ListOfContentID = content.ListOfContentID
            };
        }

        public async Task<ContentDTO> UpdateContentAsync(int userId, int contentId, ContentDTO contentDto)
        {
            var content = await _context.Contents.Include(c => c.ListOfContent).FirstOrDefaultAsync(c => c.ContentID == contentId && c.ListOfContent!.UserID == userId);
            if (content == null)
            {
                _logger.LogWarning($"Content with ID {contentId} for user {userId} not found.");
                return null;
            }

            content.Name = contentDto.Name;
            content.Description = contentDto.Description;
            content.ImageUrl = contentDto.ImageUrl;

            await _context.SaveChangesAsync();

            return new ContentDTO
            {
                ContentID = content.ContentID,
                Name = content.Name,
                Description = content.Description,
                ImageUrl = content.ImageUrl,
                ListOfContentID = content.ListOfContentID
            };
        }

        public async Task<bool> DeleteContentAsync(int userId, int contentId)
        {
            var content = await _context.Contents.Include(c => c.ListOfContent).FirstOrDefaultAsync(c => c.ContentID == contentId && c.ListOfContent!.UserID == userId);
            if (content == null)
            {
                _logger.LogWarning($"Content with ID {contentId} for user {userId} not found.");
                return false;
            }

            _context.Contents.Remove(content);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<TagDTO>> GetTagsByContentIdAsync(int userId, int contentId)
        {
            var tags = await _context.ContentTags
               .Where(ct => ct.Content!.ListOfContent!.UserID == userId && ct.ContentID == contentId)
               .Select(ct => new TagDTO
               {
                   TagID = ct.Tag!.TagID,
                   Name = ct.Tag.Name,
                   Description = ct.Tag.Description,
                   ListOfTagsID = ct.Tag.ListOfTagsID
               })
               .ToListAsync();

            return tags;
        }
    }
}
