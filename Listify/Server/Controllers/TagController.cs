using BaseLibrary.DTOs;
using BaseLibrary.Entities;
using Microsoft.AspNetCore.Mvc;
using ServerLibrary.Repositories.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TagController : ControllerBase
    {
        private readonly ITagRepository _tagRepository;

        public TagController(ITagRepository tagRepository)
        {
            _tagRepository = tagRepository;
        }

        [HttpPost("createTag/{userId}")]
        public async Task<IActionResult> CreateTag(int userId, TagDTO tagDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if the user can add more tags
                if (!await _tagRepository.CanAddTagAsync(userId))
                {
                    return BadRequest("Tag limit reached for this user.");
                }

                var createdTag = await _tagRepository.CreateTagAsync(userId, tagDto);
                if (createdTag == null)
                {
                    return NotFound($"User with ID {userId} not found.");
                }

                return CreatedAtAction(nameof(GetTagById), new { userId, tagId = createdTag.TagID }, createdTag);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("getTagsByUserId/{userId}")]
        public async Task<ActionResult<IEnumerable<TagDTO>>> GetTagsByUserId(int userId)
        {
            try
            {
                var tags = await _tagRepository.GetTagsByUserIdAsync(userId);
                if (tags == null)
                {
                    return NotFound($"User with ID {userId} not found.");
                }

                return Ok(tags);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("getTagInfoById/{userId}/{tagId}")]
        public async Task<ActionResult<TagDTO>> GetTagById(int userId, int tagId)
        {
            try
            {
                var tag = await _tagRepository.GetTagByIdAsync(userId, tagId);
                if (tag == null)
                {
                    return NotFound($"Tag with ID {tagId} for user with ID {userId} not found.");
                }
                return Ok(tag);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("updateTagInfoById/{userId}/{tagId}")]
        public async Task<IActionResult> UpdateTag(int userId, int tagId, TagDTO tagDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var updatedTag = await _tagRepository.UpdateTagAsync(userId, tagId, tagDto);
                if (updatedTag == null)
                {
                    return NotFound($"Tag with ID {tagId} for user with ID {userId} not found.");
                }

                return Ok(updatedTag);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("deleteTag/{userId}/{tagId}")]
        public async Task<IActionResult> DeleteTag(int userId, int tagId)
        {
            try
            {
                var deleted = await _tagRepository.DeleteTagAsync(userId, tagId);
                if (!deleted)
                {
                    return NotFound($"Tag with ID {tagId} for user with ID {userId} not found.");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("{userId}/addTagToContent")]
        public async Task<IActionResult> AddTagToContent(int userId, [FromBody] ContentTagDTO contentTagDto)
        {
            try
            {
                var success = await _tagRepository.AddTagToContentAsync(userId, contentTagDto.ContentID, contentTagDto.TagID);
                if (!success)
                {
                    return NotFound($"Content with ID {contentTagDto.ContentID} or Tag with ID {contentTagDto.TagID} not found.");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("{userId}/removeTagFromContent")]
        public async Task<IActionResult> RemoveTagFromContent(int userId, [FromBody] ContentTagDTO contentTagDto)
        {
            try
            {
                var success = await _tagRepository.RemoveTagFromContentAsync(userId, contentTagDto.ContentID, contentTagDto.TagID);
                if (!success)
                {
                    return NotFound($"Content with ID {contentTagDto.ContentID} or Tag with ID {contentTagDto.TagID} not found.");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
