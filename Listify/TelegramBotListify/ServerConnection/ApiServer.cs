using BaseLibrary.DTOs;
using System.Net.Http.Json;

public class ApiService
{
    private readonly HttpClient _httpClient;

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UserDTO?> GetUserByTelegramUserIdAsync(string telegramUserId)
    {
        var response = await _httpClient.GetAsync($"api/users/telegram/{telegramUserId}");
        if (response.IsSuccessStatusCode)
        {
            var user = await response.Content.ReadAsAsync<UserDTO>();
            return user;
        }
        return null;
    }

    public async Task<UserDTO?> CreateUserAsync(UserDTO userDto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/users/CreateUser", userDto);
        if (response.IsSuccessStatusCode)
        {
            var user = await response.Content.ReadAsAsync<UserDTO>();
            return user;
        }
        return null;
    }
}
