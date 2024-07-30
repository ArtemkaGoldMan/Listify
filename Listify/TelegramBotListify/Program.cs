using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Configuration;
using BaseLibrary.DTOs;
using System.Net.Http.Json;

public class Program
{
    private static readonly HttpClient httpClient = new HttpClient();

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
        switch (msg.Text)
        {
            case "/start":
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
                break;

            case "/delete":
                telegramUserId = msg.From.Id.ToString();

                userResponse = await httpClient.GetAsync($"api/Users/telegram/{telegramUserId}");
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
                break;

            default:
                await bot.SendTextMessageAsync(msg.Chat, "Do not do that");
                break;
        }
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

                case "List Menu":
                    await ShowListMenu(bot, query.Message.Chat.Id);
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

    //Main menu from bot start
    private static async Task ShowMainMenu(TelegramBotClient bot, long chatId)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Content Menu"), InlineKeyboardButton.WithCallbackData("Tag Menu") },
            new[] { InlineKeyboardButton.WithCallbackData("List Menu") }
        });

        await bot.SendTextMessageAsync(chatId, "Main Menu", replyMarkup: inlineKeyboard);
    }

    //Menu for contents with content managing
    private static async Task ShowContentMenu(TelegramBotClient bot, long chatId)
    {
        var submenuKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Show Contents"), InlineKeyboardButton.WithCallbackData("Add Content") },
            new[] { InlineKeyboardButton.WithCallbackData("Update Content"), InlineKeyboardButton.WithCallbackData("Delete Content") },
            new[] { InlineKeyboardButton.WithCallbackData("Connect Tag with Content"), InlineKeyboardButton.WithCallbackData("Remove Tag from Content") },
            new[] { InlineKeyboardButton.WithCallbackData("Back to Main Menu") }
        });

        await bot.SendTextMessageAsync(chatId, "You are in the Content Menu. Choose an option or go back to the main menu.", replyMarkup: submenuKeyboard);
    }

    //Menu for tags with tag managing
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

    //Menu for Showing and filtering List
    private static async Task ShowListMenu(TelegramBotClient bot, long chatId)
    {
        var submenuKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Show all Contents") },
            new[] { InlineKeyboardButton.WithCallbackData("Show with filter") },
            new[] { InlineKeyboardButton.WithCallbackData("Back to Main Menu") }
        });

        await bot.SendTextMessageAsync(chatId, "You are in the List Menu. Choose an option or go back to the main menu.", replyMarkup: submenuKeyboard);
    }
}
