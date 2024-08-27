using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using System.Net.Http.Json;
using System.Collections.Concurrent;
using BaseLibrary.DTOs;
using System.Net.Http;

public class UserManager
{
    private readonly TelegramBotClient _bot;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<long, string> _userStates;

    private static InlineKeyboardMarkup mainMenuKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Content Menu"), InlineKeyboardButton.WithCallbackData("Tag Menu") }
        });

    public UserManager(TelegramBotClient bot, HttpClient httpClient, ConcurrentDictionary<long, string> userStates)
    {
        _bot = bot;
        _httpClient = httpClient;
        _userStates = userStates;
    }

    public async Task HandleStartCommand(Message msg)
    {
        var telegramUserId = msg.From.Id.ToString();

        var userResponse = await _httpClient.GetAsync($"api/Users/telegram/{telegramUserId}");
        if (!userResponse.IsSuccessStatusCode)
        {
            var userDto = new UserDTO { TelegramUserID = telegramUserId };
            var response = await _httpClient.PostAsJsonAsync("api/Users/CreateUser", userDto);
            if (response.IsSuccessStatusCode)
            {
                var createdUser = await response.Content.ReadFromJsonAsync<UserDTO>();
                await _bot.SendTextMessageAsync(msg.Chat, $"User created with ID {createdUser.UserID}");

                await ShowMainMenu(msg.Chat.Id);
            }
            else
            {
                await _bot.SendTextMessageAsync(msg.Chat, "Failed to create user.");
            }
        }
        else
        {
            await _bot.SendTextMessageAsync(msg.Chat, "User has already been created.");

            await ShowMainMenu(msg.Chat.Id);
        }
    }

    public async Task HandleDeleteCommand(Message msg)
    {
        var telegramUserId = msg.From.Id.ToString();

        var userResponse = await _httpClient.GetAsync($"api/Users/telegram/{telegramUserId}");
        if (userResponse.IsSuccessStatusCode)
        {
            var user = await userResponse.Content.ReadFromJsonAsync<UserDTO>();
            var deleteResponse = await _httpClient.DeleteAsync($"api/Users/{user.UserID}");
            if (deleteResponse.IsSuccessStatusCode)
            {
                await _bot.SendTextMessageAsync(msg.Chat, $"User with ID {user.UserID} has been deleted.");
            }
            else
            {
                await _bot.SendTextMessageAsync(msg.Chat, "Failed to delete user.");
            }
        }
        else
        {
            await _bot.SendTextMessageAsync(msg.Chat, "User not found.");
        }
    }

    public async Task ShowMainMenu(long chatId)
    {
        await _bot.SendTextMessageAsync(chatId, "You are in the Main Menu. Choose an option:", replyMarkup: mainMenuKeyboard);
    }
}