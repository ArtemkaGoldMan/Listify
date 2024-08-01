using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Configuration;
using BaseLibrary.DTOs;
using System.Net.Http.Json;
using System.Collections.Concurrent;

public class Program
{
    private static readonly HttpClient httpClient = new HttpClient();
    private static readonly ConcurrentDictionary<long, string> userStates = new ConcurrentDictionary<long, string>();
    private static InlineKeyboardMarkup contentSubmenuKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Show Contents"), InlineKeyboardButton.WithCallbackData("Show with filter") },
            new[] { InlineKeyboardButton.WithCallbackData("Add Content"), InlineKeyboardButton.WithCallbackData("Delete Content") },
            new[] { InlineKeyboardButton.WithCallbackData("Update Content") },
            new[] { InlineKeyboardButton.WithCallbackData("Connect Tag with Content"), InlineKeyboardButton.WithCallbackData("Remove Tag from Content") },
            new[] { InlineKeyboardButton.WithCallbackData("Back to Main Menu") }
        });

    public static async Task Main(string[] args)
    {
        var builder = new ConfigurationBuilder().AddUserSecrets<Program>();
        var configuration = builder.Build();

        string botToken = configuration["TelegramBotToken"];
        string apiBaseUrl = "http://localhost:5038";

        if (string.IsNullOrEmpty(botToken))
        {
            throw new Exception("Telegram bot token is null.");
        }

        httpClient.BaseAddress = new Uri(apiBaseUrl);

        using var cts = new CancellationTokenSource();
        var bot = new TelegramBotClient(botToken, cancellationToken: cts.Token);
        var me = await bot.GetMeAsync();

        await SetBotCommands(bot); // Set the commands for the bot

        bot.OnError += OnError;
        bot.OnMessage += async (msg, type) => await OnMessage(bot, msg);
        bot.OnUpdate += async (update) => await OnUpdate(bot, update);

        Console.WriteLine($"@{me.Username} is running... Press Enter to terminate");
        Console.ReadLine();
        cts.Cancel();
    }

    private static async Task SetBotCommands(TelegramBotClient bot)
    {
        var commands = new List<BotCommand>
        {
            new BotCommand { Command = "start", Description = "Start the bot and register it at the same time" },
            new BotCommand { Command = "delete", Description = "Delete your user from db" }
        };

        await bot.SetMyCommandsAsync(commands);
    }

    private static async Task OnError(Exception exception, HandleErrorSource source)
    {
        Console.WriteLine(exception);
    }

    private static async Task OnMessage(TelegramBotClient bot, Message msg)
    {
        if (userStates.TryGetValue(msg.Chat.Id, out var state) && state == "awaitingContentName")
        {
            await HandleContentNameInput(bot, msg);
        }
        else
        {
            switch (msg.Text)
            {
                case "/start":
                    await HandleStartCommand(bot, msg);
                    break;

                case "/delete":
                    await HandleDeleteCommand(bot, msg);
                    break;

                default:
                    await bot.SendTextMessageAsync(msg.Chat, "Do not do that");
                    break;
            }
        }
    }

    private static async Task HandleContentNameInput(TelegramBotClient bot, Message msg)
    {
        var contentName = msg.Text;
        var telegramUserId = msg.From.Id.ToString();

        // Fetch user by telegram ID to get the user ID
        var userResponse = await httpClient.GetAsync($"api/Users/telegram/{telegramUserId}");
        if (userResponse.IsSuccessStatusCode)
        {
            var user = await userResponse.Content.ReadFromJsonAsync<UserDTO>();
            if (user != null)
            {
                // Create content for the user
                var contentDto = new ContentDTO { Name = contentName };
                var createResponse = await httpClient.PostAsJsonAsync($"api/Content/{user.UserID}", contentDto);
                if (createResponse.IsSuccessStatusCode)
                {
                    await bot.SendTextMessageAsync(msg.Chat, $"Content '{contentName}' has been added successfully.");
                }
                else
                {
                    await bot.SendTextMessageAsync(msg.Chat, "Failed to add content.");
                }
            }
        }
        else
        {
            await bot.SendTextMessageAsync(msg.Chat, "Failed to retrieve user information.");
        }

        // Remove the state and delete the user's message
        userStates.TryRemove(msg.Chat.Id, out _);
        await bot.DeleteMessageAsync(msg.Chat.Id, msg.MessageId);

        // Show the Content Menu again
        await ShowContentMenu(bot, msg.Chat.Id);
    }

    private static async Task OnUpdate(TelegramBotClient bot, Update update)
    {
        if (update is { CallbackQuery: { } query })
        {
            // Delete the previous message
            await bot.DeleteMessageAsync(query.Message.Chat.Id, query.Message.MessageId);

            switch (query.Data)
            {
                case "Content Menu":
                    await ShowContentMenu(bot, query.Message.Chat.Id);
                    break;

                case "Tag Menu":
                    await ShowTagMenu(bot, query.Message.Chat.Id);
                    break;

                case "Show Contents":
                    await ShowContents(bot, query.Message.Chat.Id, query.From.Id.ToString());
                    break;

                case "Add Content":
                    await bot.SendTextMessageAsync(query.Message.Chat.Id, "Please enter the name of the content.");
                    userStates[query.Message.Chat.Id] = "awaitingContentName";
                    break;

                case "Back to Main Menu":
                    await ShowMainMenu(bot, query.Message.Chat.Id);
                    break;

                default:
                    await bot.AnswerCallbackQueryAsync(query.Id, $"Unknown command: {query.Data}");
                    break;
            }
        }
    }

    private static async Task ShowMainMenu(TelegramBotClient bot, long chatId)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Content Menu"), InlineKeyboardButton.WithCallbackData("Tag Menu") },
        });

        await bot.SendTextMessageAsync(chatId, "Main Menu", replyMarkup: inlineKeyboard);
    }

    private static async Task ShowContentMenu(TelegramBotClient bot, long chatId)
    {
        await bot.SendTextMessageAsync(chatId, "You are in the Content Menu. Choose an option or go back to the main menu.", replyMarkup: contentSubmenuKeyboard);
    }

    private static async Task ShowTagMenu(TelegramBotClient bot, long chatId)
    {
        var submenuKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Show tags") },
            new[] { InlineKeyboardButton.WithCallbackData("Add Tag"), InlineKeyboardButton.WithCallbackData("Delete Tag") },
            new[] { InlineKeyboardButton.WithCallbackData("Update tag") },
            new[] { InlineKeyboardButton.WithCallbackData("Back to Main Menu") }
        });

        await bot.SendTextMessageAsync(chatId, "You are in the Tag Menu. Choose an option or go back to the main menu.", replyMarkup: submenuKeyboard);
    }

    private static async Task HandleStartCommand(TelegramBotClient bot, Message msg)
    {
        var telegramUserId = msg.From.Id.ToString();

        var userResponse = await httpClient.GetAsync($"api/Users/telegram/{telegramUserId}");
        if (!userResponse.IsSuccessStatusCode)
        {
            var userDto = new UserDTO { TelegramUserID = telegramUserId };
            var response = await httpClient.PostAsJsonAsync("api/Users/CreateUser", userDto);
            if (response.IsSuccessStatusCode)
            {
                var createdUser = await response.Content.ReadFromJsonAsync<UserDTO>();
                await bot.SendTextMessageAsync(msg.Chat, $"User created with ID {createdUser.UserID}");

                await ShowMainMenu(bot, msg.Chat.Id);
            }
            else
            {
                await bot.SendTextMessageAsync(msg.Chat, "Failed to create user.");
            }
        }
        else
        {
            await bot.SendTextMessageAsync(msg.Chat, "User has already been created.");

            await ShowMainMenu(bot, msg.Chat.Id);
        }
    }

    private static async Task HandleDeleteCommand(TelegramBotClient bot, Message msg)
    {
        var telegramUserId = msg.From.Id.ToString();

        var userResponse = await httpClient.GetAsync($"api/Users/telegram/{telegramUserId}");
        if (userResponse.IsSuccessStatusCode)
        {
            var user = await userResponse.Content.ReadFromJsonAsync<UserDTO>();
            var deleteResponse = await httpClient.DeleteAsync($"api/Users/{user.UserID}");
            if (deleteResponse.IsSuccessStatusCode)
            {
                await bot.SendTextMessageAsync(msg.Chat, $"User with ID {user.UserID} has been deleted.");
            }
            else
            {
                await bot.SendTextMessageAsync(msg.Chat, "Failed to delete user.");
            }
        }
        else
        {
            await bot.SendTextMessageAsync(msg.Chat, "User not found.");
        }
    }

    private static async Task ShowContents(TelegramBotClient bot, long chatId, string telegramUserId)
    {
        // Fetch user by telegram ID to get the user ID
        var userResponse = await httpClient.GetAsync($"api/Users/telegram/{telegramUserId}");
        if (!userResponse.IsSuccessStatusCode)
        {
            await bot.SendTextMessageAsync(chatId, "Failed to retrieve user information.");
            return;
        }

        var user = await userResponse.Content.ReadFromJsonAsync<UserDTO>();
        if (user == null)
        {
            await bot.SendTextMessageAsync(chatId, "User not found.");
            return;
        }

        // Fetch contents for the user
        var contentsResponse = await httpClient.GetAsync($"api/Content/{user.UserID}");
        if (!contentsResponse.IsSuccessStatusCode)
        {
            await bot.SendTextMessageAsync(chatId, "Failed to retrieve contents.");
            return;
        }

        var contents = await contentsResponse.Content.ReadFromJsonAsync<IEnumerable<ContentDTO>>();
        if (contents == null || !contents.Any())
        {
            await bot.SendTextMessageAsync(chatId, "No contents found.");
            return;
        }

        // Build the message with content names and their tags
        var messageBuilder = new System.Text.StringBuilder();
        int index = 1;
        foreach (var content in contents)
        {
            // Fetch tags for each content
            var tagsResponse = await httpClient.GetAsync($"api/Content/{user.UserID}/{content.ContentID}/tags");
            var tags = tagsResponse.IsSuccessStatusCode
                ? await tagsResponse.Content.ReadFromJsonAsync<IEnumerable<TagDTO>>()
                : Enumerable.Empty<TagDTO>();

            var tagsString = string.Join(", ", tags.Select(t => t.Name));
            messageBuilder.AppendLine($"{index}. {content.Name} ({tagsString})");
            index++;
        }

        await bot.SendTextMessageAsync(chatId, $"{messageBuilder}\n\nYou are in the Content Menu. Choose an option or go back to the main menu.", replyMarkup: contentSubmenuKeyboard);
    }
}

