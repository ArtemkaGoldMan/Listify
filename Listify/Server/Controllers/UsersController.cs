using BaseLibrary.DTOs;
using Microsoft.AspNetCore.Mvc;
using ServerLibrary.Repositories.Contracts;
using System.Threading.Tasks;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public UsersController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpPost("CreateUser")]
        public async Task<IActionResult> CreateUser([FromBody] UserDTO userDto)
        {
            try
            {
                // Ensure the DTO does not contain UserID
                if (userDto.UserID != 0)
                {
                    ModelState.AddModelError("UserID", "UserID should not be specified.");
                    return BadRequest(ModelState);
                }

                var createdUser = await _userRepository.CreateUserAsync(userDto);
                if (createdUser == null)
                {
                    return StatusCode(500, "An error occurred while creating the user.");
                }

                return CreatedAtAction(nameof(GetUserById), new { userId = createdUser.UserID }, createdUser);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("getUserByID/{userId}")]
        public async Task<IActionResult> GetUserById(int userId)
        {
            try
            {
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound($"User with ID {userId} not found.");
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("getUserByTelegramUserId/{telegramUserId}")]
        public async Task<IActionResult> GetUserByTelegramUserId(string telegramUserId)
        {
            try
            {
                var user = await _userRepository.GetUserByTelegramUserIdAsync(telegramUserId);
                if (user == null)
                {
                    return NotFound($"User with Telegram ID {telegramUserId} not found.");
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("deleteUser/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                var result = await _userRepository.DeleteUserAsync(userId);
                if (!result)
                {
                    return NotFound($"User with ID {userId} not found.");
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
