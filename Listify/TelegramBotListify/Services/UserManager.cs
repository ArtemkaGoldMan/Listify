using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using System.Net.Http.Json;
using System.Collections.Concurrent;
using BaseLibrary.DTOs;
using System.Net.Http;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using TelegramBotListify.Services;

public class UserManager
{
    private readonly TelegramBotClient _bot;
    private readonly HttpClient _httpClient;
    private readonly Helper _helper;
    private readonly ConcurrentDictionary<long, string> _userStates;

    public UserManager(TelegramBotClient bot, HttpClient httpClient, ConcurrentDictionary<long, string> userStates)
    {
        _bot = bot;
        _httpClient = httpClient;
        _userStates = userStates;
        _helper = new Helper(_bot);
    }

    /// <summary>
    /// Handles the "/start" command from the user. 
    /// Checks if the user exists in the system using their Telegram ID.
    /// If the user doesn't exist, it creates a new user and shows the main menu.
    /// If the user already exists, it directly shows the main menu.
    /// </summary>
    /// <param name="msg">The message received from the user containing the "/start" command.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task HandleStartCommand(Message msg)
    {
        var telegramUserId = msg.From!.Id.ToString();

        var userResponse = await _httpClient.GetAsync($"api/Users/getUserByTelegramUserId/{telegramUserId}");
        if (!userResponse.IsSuccessStatusCode)
        {
            var userDto = new UserDTO { TelegramUserID = telegramUserId };
            var response = await _httpClient.PostAsJsonAsync("api/Users/CreateUser", userDto);
            if (response.IsSuccessStatusCode)
            {
                //var createdUser = await response.Content.ReadFromJsonAsync<UserDTO>();
                await _helper.SendAndDeleteMessageAsync(msg.Chat.Id, $"User has been created.", 500);

                await ShowMainMenu(msg.Chat.Id);
            }
            else
            {
                await _helper.SendAndDeleteMessageAsync(msg.Chat.Id, "Failed to create user.");
            }
        }
        else
        {
            await _helper.SendAndDeleteMessageAsync(msg.Chat.Id, "User has already been created.", 500);
            await ShowMainMenu(msg.Chat.Id);
        }
    }

    /// <summary>
    /// Handles the "/delete" command from the user. 
    /// Checks if the user exists in the system using their Telegram ID.
    /// If the user exists, it deletes the user from the system.
    /// If the user doesn't exist, it informs the user that they were not found.
    /// </summary>
    /// <param name="msg">The message received from the user containing the "/delete" command.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task HandleDeleteCommand(Message msg)
    {
        var telegramUserId = msg.From!.Id.ToString();

        var userResponse = await _httpClient.GetAsync($"api/Users/getUserByTelegramUserId/{telegramUserId}");
        if (userResponse.IsSuccessStatusCode)
        {
            var user = await userResponse.Content.ReadFromJsonAsync<UserDTO>();
            var deleteResponse = await _httpClient.DeleteAsync($"api/Users/deleteUser/{user!.UserID}");
            if (deleteResponse.IsSuccessStatusCode)
            {
                await _helper.SendAndDeleteMessageAsync(msg.Chat.Id, $"User has been deleted.");
            }
            else
            {
                await _helper.SendAndDeleteMessageAsync(msg.Chat.Id, "Failed to delete user.");
            }
        }
        else
        {
            await _helper.SendAndDeleteMessageAsync(msg.Chat.Id, "User not found.");
        }
    }

    /// <summary>
    /// Displays the main menu to the user in the chat.
    /// </summary>
    /// <param name="chatId">The chat ID where the main menu should be displayed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ShowMainMenu(long chatId)
    {
        await _bot.SendTextMessageAsync(chatId, "You are in the Main Menu. Choose an option:", replyMarkup: Helper.mainMenuKeyboard);
    }
}