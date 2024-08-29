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
            // Ensure the DTO does not contain UserID
            if (userDto.UserID != 0)
            {
                ModelState.AddModelError("UserID", "UserID should not be specified.");
                return BadRequest(ModelState);
            }

            var createdUser = await _userRepository.CreateUserAsync(userDto);

            return CreatedAtAction(nameof(GetUserById), new { userId = createdUser.UserID }, createdUser);
        }


        [HttpGet("getUserByID/{userId}")]
        public async Task<IActionResult> GetUserById(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpGet("getUserByTelegramUserId/{telegramUserId}")]
        public async Task<IActionResult> GetUserByTelegramUserId(string telegramUserId)
        {
            var user = await _userRepository.GetUserByTelegramUserIdAsync(telegramUserId);
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpDelete("deleteUser/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var result = await _userRepository.DeleteUserAsync(userId);
            if (!result)
                return NotFound();

            return NoContent();
        }
    }
}
