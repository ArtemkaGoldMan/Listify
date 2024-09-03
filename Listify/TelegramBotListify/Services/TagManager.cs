using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using System.Net.Http.Json;
using System.Collections.Concurrent;
using BaseLibrary.DTOs;
using System.Net.Http;
using TelegramBotListify.Services;

public class TagManager
{
    private readonly TelegramBotClient _bot;
    private readonly HttpClient _httpClient;
    private readonly Helper _helper;
    private readonly UserManager _userManager;
    private readonly ConcurrentDictionary<long, string> _userStates;
    private readonly ConcurrentDictionary<long, int> _promptMessageIds = new ConcurrentDictionary<long, int>();

    public TagManager(TelegramBotClient bot, HttpClient httpClient, UserManager userManager, ConcurrentDictionary<long, string> userStates)
    {
        _bot = bot;
        _httpClient = httpClient;
        _userStates = userStates;
        _userManager = userManager;
        _helper = new Helper(_bot);
    }

    /// <summary>
    /// Displays a menu of tags for a user. If the user has tags, it shows a list of their tags with options to delete each tag. 
    /// If the user does not have any tags, it informs them and provides options to create a new tag or return to the main menu.
    /// </summary>
    /// <param name="chatId">The unique identifier for the chat where the message should be sent.</param>
    /// <param name="telegramUserId">The unique identifier of the user whose tags are being retrieved and displayed.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains no value.</returns>
    public async Task ShowTagMenu(long chatId)
    {
        var userResponse = await _httpClient.GetAsync($"api/Users/getUserByTelegramUserId/{chatId}");
        if (!userResponse.IsSuccessStatusCode)
        {
            await _helper.SendAndDeleteMessageAsync(chatId, "Failed to retrieve user information.");
            await _userManager.ShowMainMenu(chatId);
        }

        var user = await userResponse.Content.ReadFromJsonAsync<UserDTO>();
        if (user == null)
        {
            await _helper.SendAndDeleteMessageAsync(chatId, "User not found.");
            await _userManager.ShowMainMenu(chatId);
        }

        var tagsResponse = await _httpClient.GetAsync($"api/Tag/getTagsByUserId/{user!.UserID}");
        if (!tagsResponse.IsSuccessStatusCode)
        {
            await _helper.SendAndDeleteMessageAsync(chatId, "Failed to retrieve tags.");
            await _userManager.ShowMainMenu(chatId);
        }

        var tags = await tagsResponse.Content.ReadFromJsonAsync<IEnumerable<TagDTO>>();
        var inlineButtons = new List<IEnumerable<InlineKeyboardButton>>();

        if (tags != null && tags.Any())
        {
            inlineButtons.AddRange(tags.Select(tag => new[]
            {
            InlineKeyboardButton.WithCallbackData(tag.Name!, $"Delete Tag:{tag.TagID}")
            }));
        }

        inlineButtons.Add(new[]
        {
        InlineKeyboardButton.WithCallbackData("Create Tag"),
        InlineKeyboardButton.WithCallbackData("Main Menu")
        });

        var submenuKeyboard = new InlineKeyboardMarkup(inlineButtons);

        await _bot.SendTextMessageAsync(chatId, $"<b>Your tags:</b>", parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, replyMarkup: submenuKeyboard);
    }


    /// <summary>
    /// Initiates the process for creating a new tag by prompting the user to enter the name of the tag.
    /// The user state is updated to "awaitingTagName" to indicate that the bot is waiting for the tag name input.
    /// </summary>
    /// <param name="chatId">The unique identifier for the chat where the prompt should be sent.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains no value.</returns>
    public async Task HandleCreateTag(long chatId)
    {
        // Send the message and store the sent message's ID
        var message = await _bot.SendTextMessageAsync(chatId, "Please enter the name of the tag:");

        // Store the state and the prompt message ID
        _userStates[chatId] = "awaitingTagName";
        _promptMessageIds[chatId] = message.MessageId;
    }

    /// <summary>
    /// Handles the user's input for the tag name. Creates a new tag with the specified name and notifies the user of the result.
    /// If the tag is successfully created, the user is informed and the tag menu is displayed again.
    /// If an error occurs while creating the tag or retrieving user information, an appropriate error message is sent.
    /// </summary>
    /// <param name="msg">The message containing the tag name provided by the user.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains no value.</returns>
    public async Task HandleTagNameInput(Message msg)
    {
        var tagName = msg.Text;

        // Validation: Check if contentName is null or too long
        if (string.IsNullOrWhiteSpace(tagName))
        {
            await _bot.DeleteMessageAsync(msg.Chat.Id, msg.MessageId);
            await _helper.SendAndDeleteMessageAsync(msg.Chat, "!!!Warning!!!\nThe TAG name cannot be empty, GIFs, Stickers or Media( Can be Emoji 🙃 )." +
                                                              " Please enter a valid TAG name:", 3000);
            return;
        }
        else if (tagName.Length > 20)
        {
            await _bot.DeleteMessageAsync(msg.Chat.Id, msg.MessageId);
            await _helper.SendAndDeleteMessageAsync(msg.Chat, "!!!Warning!!!\nThe TAG name is too long. Please enter a name with 20 characters or less:", 3000);
            return; 
        }

        var telegramUserId = msg.From!.Id.ToString();

        var userResponse = await _httpClient.GetAsync($"api/Users/getUserByTelegramUserId/{telegramUserId}");
        if (userResponse.IsSuccessStatusCode)
        {
            var user = await userResponse.Content.ReadFromJsonAsync<UserDTO>();
            var tagDto = new TagDTO { Name = tagName };
            var createResponse = await _httpClient.PostAsJsonAsync($"api/Tag/createTag/{user!.UserID}", tagDto);
            if (!createResponse.IsSuccessStatusCode)
            {
                var errorMessage = await createResponse.Content.ReadAsStringAsync();
                if (errorMessage.Contains("Tag limit reached"))
                {
                    await _helper.SendAndDeleteMessageAsync(msg.Chat, "You have reached your limit for adding tags. Please remove some existing tags.", 5000);
                }
                else
                {
                    await _helper.SendAndDeleteMessageAsync(msg.Chat, "Failed to add tag. Please try again later.", 5000);
                }
            }
        }
        else
        {
            await _helper.SendAndDeleteMessageAsync(msg.Chat, "Failed to retrieve user information.");
        }

        _userStates.TryRemove(msg.Chat.Id, out _);
        if (_promptMessageIds.TryRemove(msg.Chat.Id, out var promptMessageId))
        {
            await _bot.DeleteMessageAsync(msg.Chat.Id, promptMessageId);
        }

        // Remove the user's input message
        await _bot.DeleteMessageAsync(msg.Chat.Id, msg.MessageId);

        await ShowTagMenu(msg.Chat.Id);
    }

    /// <summary>
    /// Handles the deletion of a tag identified by its ID. Notifies the user of the result of the deletion operation.
    /// If the tag is successfully deleted, the user is informed and the tag menu is displayed again.
    /// If there is an error retrieving user information or deleting the tag, an appropriate error message is sent.
    /// </summary>
    /// <param name="chatId">The unique identifier for the chat where the notification about the tag deletion should be sent.</param>
    /// <param name="tagId">The unique identifier of the tag to be deleted.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains no value.</returns>
    public async Task HandleDeleteTag(long chatId, int tagId)
    {
        var userResponse = await _httpClient.GetAsync($"api/Users/getUserByTelegramUserId/{chatId}");
        if (userResponse.IsSuccessStatusCode)
        {
            var user = await userResponse.Content.ReadFromJsonAsync<UserDTO>();
            var deleteResponse = await _httpClient.DeleteAsync($"api/Tag/deleteTag/{user!.UserID}/{tagId}");
            if (!deleteResponse.IsSuccessStatusCode)
            {
                await _helper.SendAndDeleteMessageAsync(chatId, "Failed to delete tag.");
            }
        }
        else
        {
            await _helper.SendAndDeleteMessageAsync(chatId, "User not found.");
        }

        // Show the Tag Menu again for further actions
        await ShowTagMenu(chatId);
    }

}