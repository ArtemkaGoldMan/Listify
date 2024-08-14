using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using System.Net.Http.Json;
using System.Collections.Concurrent;
using BaseLibrary.DTOs;
using System.Net.Http;

public class ContentManager
{
    private readonly TelegramBotClient _bot;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<long, string> _userStates;


    private static InlineKeyboardMarkup contentSubmenuKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Show Contents"), InlineKeyboardButton.WithCallbackData("Show with filter") },
            new[] { InlineKeyboardButton.WithCallbackData("Add Content"), InlineKeyboardButton.WithCallbackData("Delete Content") },
            new[] { InlineKeyboardButton.WithCallbackData("Back to Main Menu") }
        });

    public ContentManager(TelegramBotClient bot, HttpClient httpClient, ConcurrentDictionary<long, string> userStates)
    {
        _bot = bot;
        _httpClient = httpClient;
        _userStates = userStates;
    }

    public async Task ShowContentMenu(long chatId)
    {
        await _bot.SendTextMessageAsync(chatId, "You are in the Content Menu. Choose an option or go back to the main menu.", replyMarkup: contentSubmenuKeyboard);
    }

    public async Task ShowContents(long chatId, string telegramUserId)
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

        var contentsResponse = await _httpClient.GetAsync($"api/Content/{user.UserID}");
        if (!contentsResponse.IsSuccessStatusCode)
        {
            await _bot.SendTextMessageAsync(chatId, "Failed to retrieve contents.");
            return;
        }

        var contents = await contentsResponse.Content.ReadFromJsonAsync<IEnumerable<ContentDTO>>();
        if (contents == null || !contents.Any())
        {
            await _bot.SendTextMessageAsync(chatId, "No contents found.");
            return;
        }

        var inlineButtons = contents.Select(content => new[]
        {
        InlineKeyboardButton.WithCallbackData(content.Name, $"content:{content.ContentID}")
        });

        var submenuKeyboard = new InlineKeyboardMarkup(inlineButtons.Append(new[]
        {
        InlineKeyboardButton.WithCallbackData("Back to Content Menu", "Content Menu")
        }));

        await _bot.SendTextMessageAsync(chatId, "Select content to manage or go back to content menu:", replyMarkup: submenuKeyboard);
    }

    public async Task ShowContentsForDeletion(long chatId, string telegramUserId)
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

        var contentsResponse = await _httpClient.GetAsync($"api/Content/{user.UserID}");
        if (!contentsResponse.IsSuccessStatusCode)
        {
            await _bot.SendTextMessageAsync(chatId, "Failed to retrieve contents.");
            return;
        }

        var contents = await contentsResponse.Content.ReadFromJsonAsync<IEnumerable<ContentDTO>>();
        if (contents == null || !contents.Any())
        {
            await _bot.SendTextMessageAsync(chatId, "No contents found.");
            return;
        }

        var inlineButtons = contents.Select(content => new[]
        {
            InlineKeyboardButton.WithCallbackData($"Delete {content.Name}", $"delete:{content.ContentID}")
        });

        var submenuKeyboard = new InlineKeyboardMarkup(inlineButtons.Append(new[]
        {
            InlineKeyboardButton.WithCallbackData("Back to Content Menu", "Content Menu")
        }));

        await _bot.SendTextMessageAsync(chatId, "Select content to delete or go back to content menu:", replyMarkup: submenuKeyboard);
    }

    public async Task HandleContentOptions(long chatId, string telegramUserId, int contentId)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Add Tag", $"addTag:{contentId}"),
                InlineKeyboardButton.WithCallbackData("Remove Tag", $"showRemoveTag:{contentId}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Delete Content", $"delete:{contentId}"),
                InlineKeyboardButton.WithCallbackData("Back to Contents", "Show Contents")
            }
        });

        await _bot.SendTextMessageAsync(chatId, "Choose an option:", replyMarkup: inlineKeyboard);
    }

    public async Task HandleDeleteContent(long chatId, string telegramUserId, int contentId)
    {
        var userResponse = await _httpClient.GetAsync($"api/Users/telegram/{telegramUserId}");
        if (userResponse.IsSuccessStatusCode)
        {
            var user = await userResponse.Content.ReadFromJsonAsync<UserDTO>();
            var deleteResponse = await _httpClient.DeleteAsync($"api/Content/{user.UserID}/{contentId}");
            if (deleteResponse.IsSuccessStatusCode)
            {
                await _bot.SendTextMessageAsync(chatId, $"Content with ID {contentId} has been deleted.");
            }
            else
            {
                await _bot.SendTextMessageAsync(chatId, "Failed to delete content.");
            }
        }
        else
        {
            await _bot.SendTextMessageAsync(chatId, "User not found.");
        }

        // Show the Content Menu again for further actions
        await ShowContentsForDeletion(chatId, telegramUserId);
    }

    public async Task AddContent(long chatId)
    {
        await _bot.SendTextMessageAsync(chatId, "Please enter the name of the content:");
        _userStates[chatId] = "awaitingContentName";
    }

    public async Task HandleContentNameInput(Message msg)
    {
        var contentName = msg.Text;
        var telegramUserId = msg.From.Id.ToString();

        // Fetch user by telegram ID to get the user ID
        var userResponse = await _httpClient.GetAsync($"api/Users/telegram/{telegramUserId}");
        if (userResponse.IsSuccessStatusCode)
        {
            var user = await userResponse.Content.ReadFromJsonAsync<UserDTO>();
            if (user != null)
            {
                // Create content for the user
                var contentDto = new ContentDTO { Name = contentName };
                var createResponse = await _httpClient.PostAsJsonAsync($"api/Content/{user.UserID}", contentDto);
                if (createResponse.IsSuccessStatusCode)
                {
                    await _bot.SendTextMessageAsync(msg.Chat, $"Content '{contentName}' has been added successfully.");
                }
                else
                {
                    await _bot.SendTextMessageAsync(msg.Chat, "Failed to add content.");
                }
            }
        }
        else
        {
            await _bot.SendTextMessageAsync(msg.Chat, "Failed to retrieve user information.");
        }

        // Remove the state and delete the user's message
        _userStates.TryRemove(msg.Chat.Id, out _);
        await _bot.DeleteMessageAsync(msg.Chat.Id, msg.MessageId);

        // Show the Content Menu again
        await ShowContentMenu(msg.Chat.Id);
    }

    public async Task ShowTagsForAdding(long chatId, string telegramUserId, int contentId)
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

        // Get tags that are already associated with the content
        var contentTagsResponse = await _httpClient.GetAsync($"api/Content/{user.UserID}/{contentId}/tags");
        var contentTags = await contentTagsResponse.Content.ReadFromJsonAsync<IEnumerable<TagDTO>>();

        var inlineButtons = new List<IEnumerable<InlineKeyboardButton>>();

        if (tags != null && tags.Any())
        {
            inlineButtons.AddRange(tags.Select(tag =>
            {
                bool isConnected = contentTags != null && contentTags.Any(ct => ct.TagID == tag.TagID);
                string buttonText = isConnected ? $"{tag.Name} ✅" : tag.Name;
                string callbackData = isConnected ? $"removeTagFromContent:{contentId}:{tag.TagID}" : $"addTagToContent:{contentId}:{tag.TagID}";

                return new[] { InlineKeyboardButton.WithCallbackData(buttonText, callbackData) };
            }));
        }

        inlineButtons.Add(new[]
        {
        InlineKeyboardButton.WithCallbackData("Back to Content Options", $"content:{contentId}")
    });

        var submenuKeyboard = new InlineKeyboardMarkup(inlineButtons);

        await _bot.SendTextMessageAsync(chatId, "Select a tag to add or remove:", replyMarkup: submenuKeyboard);
    }

    public async Task AddTagToContent(long chatId, string telegramUserId, int contentId, int tagId)
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

        var contentTag = new ContentTagDTO { ContentID = contentId, TagID = tagId };
        var addTagResponse = await _httpClient.PostAsJsonAsync($"api/Tag/{user.UserID}/addTagToContent", contentTag);
        if (addTagResponse.IsSuccessStatusCode)
        {
            await _bot.SendTextMessageAsync(chatId, "Tag added to content.");
        }
        else
        {
            await _bot.SendTextMessageAsync(chatId, "Failed to add tag to content.");
        }

        // Refresh the tags menu
        await ShowTagsForAdding(chatId, telegramUserId, contentId);
    }

    // Directly shows the tags to be removed without going through additional menus
    public async Task ShowRemoveTagMenu(long chatId, string telegramUserId, int contentId)
    {
        // Fetch the tags connected to this content
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

        var tagsResponse = await _httpClient.GetAsync($"api/Content/{user.UserID}/{contentId}/tags");
        if (!tagsResponse.IsSuccessStatusCode)
        {
            await _bot.SendTextMessageAsync(chatId, "Failed to retrieve tags.");
            return;
        }

        var tags = await tagsResponse.Content.ReadFromJsonAsync<IEnumerable<TagDTO>>();
        if (tags == null || !tags.Any())
        {
            await _bot.SendTextMessageAsync(chatId, "No tags are currently associated with this content.");
            return;
        }

        var inlineButtons = tags.Select(tag => new[]
        {
            InlineKeyboardButton.WithCallbackData(tag.Name, $"removeTagFromContent:{contentId}:{tag.TagID}")
        });

        var submenuKeyboard = new InlineKeyboardMarkup(inlineButtons.Append(new[]
        {
            InlineKeyboardButton.WithCallbackData("Back to Content Options", $"content:{contentId}")
        }));

        await _bot.SendTextMessageAsync(chatId, "Select a tag to remove or go back:", replyMarkup: submenuKeyboard);
    }

    // Method to handle the actual removal of the tag from the content
    public async Task RemoveTagFromContent(long chatId, string telegramUserId, int contentId, int tagId)
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

        var removeTagResponse = await _httpClient.PostAsJsonAsync($"api/Tag/{user.UserID}/removeTagFromContent", new ContentTagDTO
        {
            ContentID = contentId,
            TagID = tagId
        });

        if (!removeTagResponse.IsSuccessStatusCode)
        {
            await _bot.SendTextMessageAsync(chatId, "Failed to remove tag from content.");
        }
        else
        {
            await _bot.SendTextMessageAsync(chatId, $"Tag has been removed from content.");
        }

        // Refresh the tag removal menu
        await ShowRemoveTagMenu(chatId, telegramUserId, contentId);
    }

    
}
