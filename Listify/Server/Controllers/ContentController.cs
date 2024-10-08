﻿using Microsoft.AspNetCore.Mvc;
using ServerLibrary.Repositories.Contracts;
using BaseLibrary.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

[Route("api/[controller]")]
[ApiController]
public class ContentController : ControllerBase
{
    private readonly IContentRepository _contentRepository;

    public ContentController(IContentRepository contentRepository)
    {
        _contentRepository = contentRepository;
    }

    [HttpPost("createContent/{userId}")]
    public async Task<IActionResult> CreateContent(int userId, [FromBody] ContentDTO contentDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if the user can add more content
            if (!await _contentRepository.CanAddContentAsync(userId))
            {
                return BadRequest("Content limit reached for this user.");
            }

            var createdContent = await _contentRepository.CreateContentAsync(userId, contentDto);
            if (createdContent == null)
            {
                return NotFound($"User with ID {userId} not found.");
            }

            return CreatedAtAction(nameof(GetContentById), new { userId, contentId = createdContent.ContentID }, createdContent);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("getContentsByUserId/{userId}")]
    public async Task<IActionResult> GetContentsByUserId(int userId)
    {
        try
        {
            var contents = await _contentRepository.GetContentsByUserIdAsync(userId);
            if (contents == null)
            {
                return NotFound($"User with ID {userId} not found.");
            }

            return Ok(contents);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("getContentById/{userId}/{contentId}")]
    public async Task<IActionResult> GetContentById(int userId, int contentId)
    {
        try
        {
            var content = await _contentRepository.GetContentByIdAsync(userId, contentId);
            if (content == null)
            {
                return NotFound($"Content with ID {contentId} for user {userId} not found.");
            }

            return Ok(content);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPut("{userId}/{contentId}")]
    public async Task<IActionResult> UpdateContent(int userId, int contentId, [FromBody] ContentDTO contentDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedContent = await _contentRepository.UpdateContentAsync(userId, contentId, contentDto);
            if (updatedContent == null)
            {
                return NotFound($"Content with ID {contentId} for user {userId} not found.");
            }

            return Ok(updatedContent);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpDelete("deleteContent/{userId}/{contentId}")]
    public async Task<IActionResult> DeleteContent(int userId, int contentId)
    {
        try
        {
            var success = await _contentRepository.DeleteContentAsync(userId, contentId);
            if (!success)
            {
                return NotFound($"Content with ID {contentId} for user {userId} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // to get all tags for a specific content
    [HttpGet("getTagsByContentId/{userId}/{contentId}/tags")]
    public async Task<ActionResult<IEnumerable<TagDTO>>> GetTagsByContentId(int userId, int contentId)
    {
        try
        {
            var tags = await _contentRepository.GetTagsByContentIdAsync(userId, contentId);
            return Ok(tags);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("{userId}/contentsByTags")]
    public async Task<IActionResult> GetContentsByUserIdAndTagList(int userId, [FromQuery] IEnumerable<int> tagIds)
    {
        try
        {
            if (tagIds == null || !tagIds.Any())
            {
                var userContents = await _contentRepository.GetContentsByUserIdAsync(userId);
                if (userContents == null)
                {
                    return NotFound($"User with ID {userId} not found.");
                }

                return Ok(userContents);
            }

            var filteredContents = await _contentRepository.GetContentsByUserIdAndTagListAsync(userId, tagIds);
            if (filteredContents == null || !filteredContents.Any())
            {
                return NotFound($"No contents found for user {userId} with the specified tags.");
            }

            return Ok(filteredContents);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
