using Microsoft.AspNetCore.Mvc;
using ServerLibrary.Repositories.Contracts;
using BaseLibrary.DTOs;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class ContentController : ControllerBase
{
    private readonly IContentRepository _contentRepository;

    public ContentController(IContentRepository contentRepository)
    {
        _contentRepository = contentRepository;
    }

    [HttpPost("{userId}")]
    public async Task<IActionResult> CreateContent(int userId, [FromBody] ContentDTO contentDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var createdContent = await _contentRepository.CreateContentAsync(userId, contentDto);
        if (createdContent == null)
        {
            return NotFound($"User with ID {userId} not found.");
        }

        return CreatedAtAction(nameof(GetContentById), new { userId, contentId = createdContent.ContentID }, createdContent);
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetContentsByUserId(int userId)
    {
        var contents = await _contentRepository.GetContentsByUserIdAsync(userId);
        if (contents == null)
        {
            return NotFound($"User with ID {userId} not found.");
        }

        return Ok(contents);
    }

    [HttpGet("{userId}/{contentId}")]
    public async Task<IActionResult> GetContentById(int userId, int contentId)
    {
        var content = await _contentRepository.GetContentByIdAsync(userId, contentId);
        if (content == null)
        {
            return NotFound($"Content with ID {contentId} for user {userId} not found.");
        }

        return Ok(content);
    }

    [HttpPut("{userId}/{contentId}")]
    public async Task<IActionResult> UpdateContent(int userId, int contentId, [FromBody] ContentDTO contentDto)
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

    [HttpDelete("{userId}/{contentId}")]
    public async Task<IActionResult> DeleteContent(int userId, int contentId)
    {
        var success = await _contentRepository.DeleteContentAsync(userId, contentId);
        if (!success)
        {
            return NotFound($"Content with ID {contentId} for user {userId} not found.");
        }

        return NoContent();
    }

    // to get all tags for a specific content
    [HttpGet("{userId}/{contentId}/tags")]
    public async Task<ActionResult<IEnumerable<TagDTO>>> GetTagsByContentId(int userId, int contentId)
    {
        var tags = await _contentRepository.GetTagsByContentIdAsync(userId, contentId);
        return Ok(tags);
    }

   

    [HttpGet("{userId}/contentsByTags")]
    public async Task<IActionResult> GetContentsByUserIdAndTagList(int userId, [FromQuery] IEnumerable<int> tagIds)
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
            return Ok($"No contents found for user {userId} with the specified tags.");
        }

        return Ok(filteredContents);
    }

}
