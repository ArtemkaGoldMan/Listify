using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using System.Net.Http.Json;
using System.Collections.Concurrent;
using BaseLibrary.DTOs;
using System.Net.Http;

public class TagManager
{
    private readonly TelegramBotClient _bot;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<long, string> _userStates;

    public TagManager(TelegramBotClient bot, HttpClient httpClient, ConcurrentDictionary<long, string> userStates)
    {
        _bot = bot;
        _httpClient = httpClient;
        _userStates = userStates;
    }

    public async Task ShowTagMenu(long chatId, string telegramUserId)
    {
        var userResponse = await _httpClient.GetAsync($"api/Users/telegram/{telegramUserId}");
        if (!userResponse.IsSuccessStatusCode)
        {
            await _bot.SendTextMessageAsync(chatId, "Failed to retrieve user information.");
            return;
        }

        var user = await userResponse.Content.ReadFromJsonAsync<UserDTO>();
        if (user == null)
        {
            await _bot.SendTextMessageAsync(chatId, "User not found.");
            return;
        }


        var tagsResponse = await _httpClient.GetAsync($"api/Tag/{user.UserID}");
        if (!tagsResponse.IsSuccessStatusCode)
        {
            await _bot.SendTextMessageAsync(chatId, "Failed to retrieve tags.");
            return;
        }

        var tags = await tagsResponse.Content.ReadFromJsonAsync<IEnumerable<TagDTO>>();
        var inlineButtons = new List<IEnumerable<InlineKeyboardButton>>();

        if (tags != null && tags.Any())
        {
            inlineButtons.AddRange(tags.Select(tag => new[]
            {
            InlineKeyboardButton.WithCallbackData(tag.Name, $"deleteTag:{tag.TagID}")
        }));
        }

        inlineButtons.Add(new[]
        {
        InlineKeyboardButton.WithCallbackData("Create Tag"),
        InlineKeyboardButton.WithCallbackData("Back to Main Menu")
        });

        var submenuKeyboard = new InlineKeyboardMarkup(inlineButtons);

        await _bot.SendTextMessageAsync(chatId, "Your tags:", replyMarkup: submenuKeyboard);
    }

    public async Task HandleCreateTag(long chatId)
    {
        await _bot.SendTextMessageAsync(chatId, "Please enter the name of the tag:");
        _userStates[chatId] = "awaitingTagName";
    }

    public async Task HandleTagNameInput(Message msg)
    {
        var tagName = msg.Text;
        var telegramUserId = msg.From.Id.ToString();

        var userResponse = await _httpClient.GetAsync($"api/Users/telegram/{telegramUserId}");
        if (userResponse.IsSuccessStatusCode)
        {
            var user = await userResponse.Content.ReadFromJsonAsync<UserDTO>();
            var tagDto = new TagDTO { Name = tagName };
            var createResponse = await _httpClient.PostAsJsonAsync($"api/Tag/{user.UserID}", tagDto);
            if (createResponse.IsSuccessStatusCode)
            {
                await _bot.SendTextMessageAsync(msg.Chat, $"Tag '{tagName}' has been added successfully.");
            }
            else
            {
                await _bot.SendTextMessageAsync(msg.Chat, "Failed to add tag.");
            }
        }
        else
        {
            await _bot.SendTextMessageAsync(msg.Chat, "Failed to retrieve user information.");
        }

        _userStates.TryRemove(msg.Chat.Id, out _);
        await _bot.DeleteMessageAsync(msg.Chat.Id, msg.MessageId);

        await ShowTagMenu(msg.Chat.Id, telegramUserId);
    }

    public async Task HandleDeleteTag(long chatId, string telegramUserId, int tagId)
    {
        var userResponse = await _httpClient.GetAsync($"api/Users/telegram/{telegramUserId}");
        if (userResponse.IsSuccessStatusCode)
        {
            var user = await userResponse.Content.ReadFromJsonAsync<UserDTO>();
            var deleteResponse = await _httpClient.DeleteAsync($"api/Tag/{user.UserID}/{tagId}");
            if (deleteResponse.IsSuccessStatusCode)
            {
                await _bot.SendTextMessageAsync(chatId, $"Tag with ID {tagId} has been deleted.");
            }
            else
            {
                await _bot.SendTextMessageAsync(chatId, "Failed to delete tag.");
            }
        }
        else
        {
            await _bot.SendTextMessageAsync(chatId, "User not found.");
        }

        // Show the Tag Menu again for further actions
        await ShowTagMenu(chatId, telegramUserId);
    }
}
