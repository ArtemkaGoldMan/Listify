using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using System.Net.Http.Json;
using System.Collections.Concurrent;
using BaseLibrary.DTOs;
using System.Net.Http;
using System.Text;
using TelegramBotListify.Services;
using Telegram.Bot.Types.Enums;

public class ContentManager
{
    private readonly TelegramBotClient _bot;
    private readonly HttpClient _httpClient;
    private readonly Helper _helper;
    private readonly ConcurrentDictionary<long, string> _userStates;
    private readonly ConcurrentDictionary<long, HashSet<int>> _userTagSelections; // Track selected tags for each user

    public ContentManager(TelegramBotClient bot, HttpClient httpClient, ConcurrentDictionary<long, string> userStates, ConcurrentDictionary<long, HashSet<int>> userTagSelections)
    {
        _bot = bot;
        _httpClient = httpClient;
        _userStates = userStates;
        _userTagSelections = userTagSelections;
        _helper = new Helper(_bot);
    }

    /// <summary>
    /// Displays the content menu to the user, providing options related to content management and an option to go back to the main menu.
    /// </summary>
    /// <param name="chatId">The unique identifier for the chat where the content menu should be displayed.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains no value.</returns>
    public async Task ShowContentMenu(long chatId)
    {
        await _bot.SendTextMessageAsync(chatId, "You are in the Content Menu. Choose an option or go back to the main menu.", replyMarkup: Helper.contentMenuKeyboard);
    }

    /// <summary>
    /// Displays a menu of content-related options to the user. Options include adding a tag, removing a tag, deleting content, and returning to the contents list.
    /// </summary>
    /// <param name="chatId">The unique identifier for the chat where the content options menu should be displayed.</param>
    /// <param name="contentId">The unique identifier of the content item for which the options are being displayed.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains no value.</returns>
    public async Task HandleContentOptions(long chatId, int contentId, string contentName)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
        new[]{ InlineKeyboardButton.WithCallbackData("Tags", $"addTag:{contentId}:{contentName}") },
        new[]{ InlineKeyboardButton.WithCallbackData("Delete Content", $"deleteContent:{contentId}") },
        new[]{ InlineKeyboardButton.WithCallbackData("Back to Contents", "Show Contents") }
        });

        await _bot.SendTextMessageAsync(
            chatId,
            $"<b>{contentName}</b>\nChoose an option:",
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard
            );

    }


    /// <summary>
    /// Displays a menu of content items available for management. 
    /// Retrieves the list of content items associated with the user and presents them as inline buttons. 
    /// Each button allows the user to select a content item for further management. The menu also includes 
    /// an option to return to the main content menu if the user wishes to do so.
    /// </summary>
    /// <param name="chatId">The unique identifier of the chat where the menu should be displayed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ShowContents(long chatId)
    {
        // Retrieve user information
        var userResponse = await _httpClient.GetAsync($"api/Users/getUserByTelegramUserId/{chatId}");
        if (!userResponse.IsSuccessStatusCode)
        {
            await _helper.SendAndDeleteMessageAsync(chatId, "Failed to retrieve user information.");
            return;
        }

        var user = await userResponse.Content.ReadFromJsonAsync<UserDTO>();
        if (user == null)
        {
            await _helper.SendAndDeleteMessageAsync(chatId, "User not found.");
            return;
        }

        // Retrieve contents
        var contentsResponse = await _httpClient.GetAsync($"api/Content/getContentsByUserId/{user.UserID}");
        if (!contentsResponse.IsSuccessStatusCode)
        {
            await _helper.SendAndDeleteMessageAsync(chatId, "Failed to retrieve contents.");
            return;
        }

        var contents = await contentsResponse.Content.ReadFromJsonAsync<IEnumerable<ContentDTO>>();
        if (contents == null || !contents.Any())
        {
            // If no contents found, show message with "Content Menu" button
            var inlineButtons = new InlineKeyboardMarkup(new[]
            {
            InlineKeyboardButton.WithCallbackData("Content Menu")
            });

            await _bot.SendTextMessageAsync(chatId, "No contents found.", replyMarkup: inlineButtons);
            return;
        }

        // Create buttons for each content
        var inlineButtonsForContents = contents.Select(content => new[]
        {
        InlineKeyboardButton.WithCallbackData(content.Name!, $"Specific Content Managing:{content.ContentID}:{content.Name!}")
        }).ToList();

        // Add "Content Menu" button
        inlineButtonsForContents.Add(new[]
        {
        InlineKeyboardButton.WithCallbackData("Content Menu")
    });

        var submenuKeyboard = new InlineKeyboardMarkup(inlineButtonsForContents);

        await _bot.SendTextMessageAsync(chatId, "Select content to manage or go back to content menu:", replyMarkup: submenuKeyboard);
    }



    public async Task ShowContentsToList(long chatId)
    {
        // Retrieve user information
        var userResponse = await _httpClient.GetAsync($"api/Users/getUserByTelegramUserId/{chatId}");
        if (!userResponse.IsSuccessStatusCode)
        {
            await _helper.SendAndDeleteMessageAsync(chatId, "Failed to retrieve user information.");
            return;
        }

        var user = await userResponse.Content.ReadFromJsonAsync<UserDTO>();
        if (user == null)
        {
            await _helper.SendAndDeleteMessageAsync(chatId, "User not found.");
            return;
        }

        // Retrieve tags
        var tagsResponse = await _httpClient.GetAsync($"api/Tag/getTagsByUserId/{user.UserID}");
        if (!tagsResponse.IsSuccessStatusCode)
        {
            await _helper.SendAndDeleteMessageAsync(chatId, "Failed to retrieve tags.");
            return;
        }

        var tags = await tagsResponse.Content.ReadFromJsonAsync<IEnumerable<TagDTO>>();

        // Initialize or get user's selected tags
        var selectedTags = _userTagSelections.GetOrAdd(chatId, _ => new HashSet<int>());

        // Retrieve contents based on selected tags
        var contentsResponse = await _httpClient.GetAsync($"api/Content/{user.UserID}/contentsByTags?{string.Join("&", selectedTags.Select(t => $"tagIds={t}"))}");

        IEnumerable<ContentDTO>? contents = null;
        if (contentsResponse.IsSuccessStatusCode)
        {
            contents = await contentsResponse.Content.ReadFromJsonAsync<IEnumerable<ContentDTO>>();
        }

        // Create message with contents
        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine("CONTENTS:");

        if (contents != null && contents.Any())
        {
            foreach (var content in contents)
            {
                // Retrieve tags for each content
                var contentTagsResponse = await _httpClient.GetAsync($"api/Content/getTagsByContentId/{user.UserID}/{content.ContentID}/tags");
                IEnumerable<TagDTO>? contentTags = null;
                if (contentTagsResponse.IsSuccessStatusCode)
                {
                    contentTags = await contentTagsResponse.Content.ReadFromJsonAsync<IEnumerable<TagDTO>>();
                }

                // Build content message with tags
                var contentTagNames = contentTags != null ? string.Join(", ", contentTags.Select(t => t.Name)) : "No tags";
                messageBuilder.AppendLine($"- {content.Name} [Tags: {contentTagNames}]");
            }
        }
        else
        {
            messageBuilder.AppendLine("\n*No contents found for the selected tags.");
        }

        // Create tag buttons
        var inlineButtons = tags!.Select(tag =>
        {
            bool isSelected = selectedTags.Contains(tag.TagID);
            string? buttonText = isSelected ? $"✅ {tag.Name}" : tag.Name;
            string callbackData = isSelected ? $"uncountTag:{tag.TagID}" : $"countTag:{tag.TagID}";

            return new[] { InlineKeyboardButton.WithCallbackData(buttonText!, callbackData) };
        }).ToList();

        // Add "Content Menu" button
        inlineButtons.Add(new[]
        {
        InlineKeyboardButton.WithCallbackData("Content Menu")
        });

        var tagKeyboard = new InlineKeyboardMarkup(inlineButtons);

        // Send the message with the "Content Menu" button and tag keyboard
        await _bot.SendTextMessageAsync(chatId, messageBuilder.ToString(), replyMarkup: tagKeyboard);
    }


    public async Task HandleTagSelection(long chatId, int tagId, bool isAdding)
    {
        var selectedTags = _userTagSelections.GetOrAdd(chatId, _ => new HashSet<int>());

        if (isAdding)
        {
            selectedTags.Add(tagId);
        }
        else
        {
            selectedTags.Remove(tagId);
        }

        // Fetch and display contents based on updated tag selection
        await ShowContentsToList(chatId);
    }


    /// <summary>
    /// Displays a menu to the user for selecting content to delete. 
    /// Retrieves the list of content items associated with the user and presents them as inline buttons 
    /// for deletion. The user can select a content item to delete or choose to go back to the content menu.
    /// </summary>
    /// <param name="chatId">The unique identifier of the chat where the menu should be displayed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ShowContentsForDeletion(long chatId)
    {
        // Retrieve user information
        var userResponse = await _httpClient.GetAsync($"api/Users/getUserByTelegramUserId/{chatId}");
        if (!userResponse.IsSuccessStatusCode)
        {
            await _helper.SendAndDeleteMessageAsync(chatId, "Failed to retrieve user information.");
            return;
        }

        var user = await userResponse.Content.ReadFromJsonAsync<UserDTO>();
        if (user == null)
        {
            await _helper.SendAndDeleteMessageAsync(chatId, "User not found.");
            return;
        }

        // Retrieve contents
        var contentsResponse = await _httpClient.GetAsync($"api/Content/getContentsByUserId/{user.UserID}");
        if (!contentsResponse.IsSuccessStatusCode)
        {
            await _helper.SendAndDeleteMessageAsync(chatId, "Failed to retrieve contents.");
            return;
        }

        var contents = await contentsResponse.Content.ReadFromJsonAsync<IEnumerable<ContentDTO>>();
        if (contents == null || !contents.Any())
        {
            // If no contents found, show message with "Content Menu" button
            var inlineButtons = new InlineKeyboardMarkup(new[]
            {
            InlineKeyboardButton.WithCallbackData("Content Menu")
        });

            await _bot.SendTextMessageAsync(chatId, "No contents found.", replyMarkup: inlineButtons);
            return;
        }

        //deleting state
        _userStates[chatId] = "DeletingMenu";

        // Create buttons for each content
        var inlineButtonsForContents = contents.Select(content => new[]
        {
        InlineKeyboardButton.WithCallbackData($"Delete <{content.Name}>", $"deleteContent:{content.ContentID}")
    }).ToList();

        // Add "Content Menu" button
        inlineButtonsForContents.Add(new[]
        {
        InlineKeyboardButton.WithCallbackData("Content Menu")
    });

        var submenuKeyboard = new InlineKeyboardMarkup(inlineButtonsForContents);

        await _bot.SendTextMessageAsync(chatId, "Select content to delete or go back to Content Menu:", replyMarkup: submenuKeyboard);
    }

    /// <summary>
    /// Handles the deletion of a specific content item. 
    /// Retrieves user information to ensure the request is authorized, 
    /// then sends a request to delete the specified content item. 
    /// After processing the deletion request, the method sends a confirmation or error message 
    /// and refreshes the list of contents available for deletion.
    /// </summary>
    /// <param name="chatId">The unique identifier of the chat where the deletion confirmation or error message should be sent.</param>
    /// <param name="contentId">The ID of the content item to be deleted.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task HandleDeleteContent(long chatId, int contentId)
    {
        var userResponse = await _httpClient.GetAsync($"api/Users/getUserByTelegramUserId/{chatId}");
        if (userResponse.IsSuccessStatusCode)
        {
            var user = await userResponse.Content.ReadFromJsonAsync<UserDTO>();
            var deleteResponse = await _httpClient.DeleteAsync($"api/Content/deleteContent/{user!.UserID}/{contentId}");
            if (deleteResponse.IsSuccessStatusCode)
            {
                await _helper.SendAndDeleteMessageAsync(chatId, $"Content has been deleted.");
            }
            else
            {
                await _helper.SendAndDeleteMessageAsync(chatId, "Failed to delete content.");
            }
        }
        else
        {
            await _helper.SendAndDeleteMessageAsync(chatId, "User not found.");
        }

        _userStates.TryGetValue(chatId, out var state);
        
        if (state == "DeletingMenu")
        {
            // Show the Content Menu again for further actions
            _userStates.TryRemove(chatId, out _);
            await ShowContentsForDeletion(chatId);
        }
        else
        {
            await ShowContents(chatId);
        }
        
    }

    /// <summary>
    /// Initiates the process for adding new content by prompting the user to enter the name of the content. 
    /// Sends a message to the specified chat asking the user to provide the content name, 
    /// and updates the user's state to indicate that the system is waiting for the content name input.
    /// </summary>
    /// <param name="chatId">The unique identifier of the chat where the message should be sent and the user state should be updated.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AddContent(long chatId)
    {
        await _helper.SendAndDeleteMessageAsync(chatId, "Please enter the name of the content:", 1500);
        _userStates[chatId] = "awaitingContentName";
    }

    /// <summary>
    /// Handles the user's input for creating new content. 
    /// Retrieves the user's information based on their Telegram user ID to get the user ID. 
    /// Then, it creates a new content entry with the provided content name for the user. 
    /// The method sends a confirmation message if the content creation is successful, 
    /// or an error message if it fails. After processing the request, it removes the user's state, 
    /// deletes the original message, and refreshes the content menu for the user.
    /// </summary>
    /// <param name="msg">The message object containing the user's input for the content name, and additional details like chat ID and message ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task HandleContentNameInput(Message msg)
    {
        var contentName = msg.Text;
        var telegramUserId = msg.From!.Id.ToString();

        // Fetch user by telegram ID to get the user ID
        var userResponse = await _httpClient.GetAsync($"api/Users/getUserByTelegramUserId/{telegramUserId}");
        if (userResponse.IsSuccessStatusCode)
        {
            var user = await userResponse.Content.ReadFromJsonAsync<UserDTO>();
            if (user != null)
            {
                // Create content for the user
                var contentDto = new ContentDTO { Name = contentName };
                var createResponse = await _httpClient.PostAsJsonAsync($"api/Content/createContent/{user.UserID}", contentDto);
                if (createResponse.IsSuccessStatusCode)
                {
                    await _helper.SendAndDeleteMessageAsync(msg.Chat, $"Content '{contentName}' has been added successfully.", 500);
                }
                else
                {
                    await _helper.SendAndDeleteMessageAsync(msg.Chat, "Failed to add content.");
                }
            }
        }
        else
        {
            await _helper.SendAndDeleteMessageAsync(msg.Chat, "Failed to retrieve user information.");
        }

        // Remove the state and delete the user's message
        _userStates.TryRemove(msg.Chat.Id, out _);
        await _bot.DeleteMessageAsync(msg.Chat.Id, msg.MessageId);

        // Show the Content Menu again
        await ShowContentMenu(msg.Chat.Id);
    }

    /// <summary>
    /// Displays a menu to the user for adding or removing tags from a specified content item. 
    /// Retrieves the list of available tags and the tags currently associated with the content. 
    /// The user is presented with inline buttons to either add or remove tags from the content 
    /// based on their current association status. The menu also includes an option to return to 
    /// the content options menu.
    /// </summary>
    /// <param name="chatId">The unique identifier of the chat where the menu should be displayed.</param>
    /// <param name="contentId">The ID of the content for which tags are being managed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ShowTagsForAdding(long chatId, int contentId, string contentName)
    {
        var userResponse = await _httpClient.GetAsync($"api/Users/getUserByTelegramUserId/{chatId}");
        if (!userResponse.IsSuccessStatusCode)
        {
            await _helper.SendAndDeleteMessageAsync(chatId, "Failed to retrieve user information.");
            return;
        }

        var user = await userResponse.Content.ReadFromJsonAsync<UserDTO>();
        if (user == null)
        {
            await _helper.SendAndDeleteMessageAsync(chatId, "User not found.");
            return;
        }

        var tagsResponse = await _httpClient.GetAsync($"api/Tag/getTagsByUserId/{user.UserID}");


        if (!tagsResponse.IsSuccessStatusCode)
        {
            await _helper.SendAndDeleteMessageAsync(chatId, "Failed to retrieve tags.");
            return;
        }

        var tags = await tagsResponse.Content.ReadFromJsonAsync<IEnumerable<TagDTO>>();

        // Get tags that are already associated with the content
        var contentTagsResponse = await _httpClient.GetAsync($"api/Content/getTagsByContentId/{user.UserID}/{contentId}/tags");
        var contentTags = await contentTagsResponse.Content.ReadFromJsonAsync<IEnumerable<TagDTO>>();

        var inlineButtons = new List<IEnumerable<InlineKeyboardButton>>();

        if (tags != null && tags.Any())
        {
            inlineButtons.AddRange(tags.Select(tag =>
            {
                bool isConnected = contentTags != null && contentTags.Any(ct => ct.TagID == tag.TagID);
                string? buttonText = isConnected ? $"✅ {tag.Name}" : $"❌{tag.Name}";
                string callbackData = isConnected ? $"removeTagFromContent:{contentId}:{tag.TagID}:{contentName}" : $"addTagToContent:{contentId}:{tag.TagID}:{contentName}";

                return new[] { InlineKeyboardButton.WithCallbackData(buttonText!, callbackData) };
            }));
        }

        inlineButtons.Add(new[]
        {
        InlineKeyboardButton.WithCallbackData("Content Options", $"Specific Content Managing:{contentId}:{contentName}")
        });

        var submenuKeyboard = new InlineKeyboardMarkup(inlineButtons);

        await _bot.SendTextMessageAsync(chatId, $"<b>{contentName}</b>\nSelect a tag to add or remove:",parseMode: ParseMode.Html, replyMarkup: submenuKeyboard);
    }

    /// <summary>
    /// Adds a specific tag to a content item. 
    /// Retrieves user information to ensure the request is authorized, 
    /// then sends a request to add the specified tag to the content. 
    /// After the tag addition operation, the method refreshes the menu 
    /// that allows the user to add or remove tags, displaying the updated 
    /// state of the tags associated with the content.
    /// </summary>
    /// <param name="chatId">The unique identifier of the chat where the addition confirmation should be sent.</param>
    /// <param name="contentId">The ID of the content to which the tag is being added.</param>
    /// <param name="tagId">The ID of the tag to be added to the content.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AddTagToContent(long chatId, int contentId, int tagId, string contentName)

    {
        var userResponse = await _httpClient.GetAsync($"api/Users/getUserByTelegramUserId/{chatId}");
        if (!userResponse.IsSuccessStatusCode)
        {
            await _helper.SendAndDeleteMessageAsync(chatId, "Failed to retrieve user information.");
            return;
        }

        var user = await userResponse.Content.ReadFromJsonAsync<UserDTO>();
        if (user == null)
        {
            await _helper.SendAndDeleteMessageAsync(chatId, "User not found.");
            return;
        }

        var contentTag = new ContentTagDTO { ContentID = contentId, TagID = tagId };
        var addTagResponse = await _httpClient.PostAsJsonAsync($"api/Tag/{user.UserID}/addTagToContent", contentTag);
        if (!addTagResponse.IsSuccessStatusCode)
        {
            await _helper.SendAndDeleteMessageAsync(chatId, "Failed to add tag to content.");
        }

        // Refresh the tags menu
        await ShowTagsForAdding(chatId, contentId, contentName);
    }

    /// <summary>
    /// Handles the removal of a specific tag from a content item. 
    /// Retrieves the user information to ensure the request is authorized, 
    /// then sends a request to remove the specified tag from the content. 
    /// </summary>
    /// <param name="chatId">The unique identifier of the chat where the removal confirmation should be sent.</param>
    /// <param name="contentId">The ID of the content from which the tag is to be removed.</param>
    /// <param name="tagId">The ID of the tag to be removed from the content.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RemoveTagFromContent(long chatId, int contentId, int tagId, string contentName)
    {
        var userResponse = await _httpClient.GetAsync($"api/Users/getUserByTelegramUserId/{chatId}");
        if (!userResponse.IsSuccessStatusCode)
        {
            await _helper.SendAndDeleteMessageAsync(chatId, "Failed to retrieve user information.");
            return;
        }

        var user = await userResponse.Content.ReadFromJsonAsync<UserDTO>();
        if (user == null)
        {
            await _helper.SendAndDeleteMessageAsync(chatId, "User not found.");
            return;
        }

        var removeTagResponse = await _httpClient.PostAsJsonAsync($"api/Tag/{user.UserID}/removeTagFromContent", new ContentTagDTO
        {
            ContentID = contentId,
            TagID = tagId
        });

        if (!removeTagResponse.IsSuccessStatusCode)
        {
            await _helper.SendAndDeleteMessageAsync(chatId, "Failed to remove tag from content.");
        }

        // Refresh the tag removal menu
        await ShowTagsForAdding(chatId, contentId, contentName);
    }
}