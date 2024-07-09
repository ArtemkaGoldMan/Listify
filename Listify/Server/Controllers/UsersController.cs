using System;
using BaseLibrary.Entities;
using Microsoft.AspNetCore.Mvc;
using ServerLibrary.Repositories.Contracts;

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

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            var createdUser = await _userRepository.CreateUserAsync(user);
            return CreatedAtAction(nameof(GetUserById), new { userId = createdUser.UserID }, createdUser);
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserById(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpGet("telegram/{telegramUserId}")]
        public async Task<IActionResult> GetUserByTelegramUserId(string telegramUserId)
        {
            var user = await _userRepository.GetUserByTelegramUserIdAsync(telegramUserId);
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] User user)
        {
            var updatedUser = await _userRepository.UpdateUserAsync(userId, user);
            if (updatedUser == null)
                return NotFound();

            return Ok(updatedUser);
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var result = await _userRepository.DeleteUserAsync(userId);
            if (!result)
                return NotFound();

            return NoContent();
        }
    }
}

