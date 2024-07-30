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
            new BotCommand { Command = "start", Description = "Start the bot" },
            new BotCommand { Command = "delete", Description = "Delete your user" }
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
            await bot.AnswerCallbackQueryAsync(query.Id, $"You picked {query.Data}");
            await bot.SendTextMessageAsync(query.Message!.Chat, $"User {query.From} clicked on {query.Data}");
        }
    }

    //private static async Task ShowMainMenuOLD(TelegramBotClient bot, long chatId)
    //{
    //    var mainMenuKeyboard = new ReplyKeyboardMarkup(new[]
    //    {
    //    new KeyboardButton[] { "Content", "Tags" },
    //    new KeyboardButton[] { "List" }
    //    })
    //    {
    //        ResizeKeyboard = true
    //    };

    //    await bot.SendTextMessageAsync(chatId, "Choose an option:", replyMarkup: mainMenuKeyboard);
    //}

    private static async Task ShowMainMenu(TelegramBotClient bot, long chatId)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("Content Menu"), InlineKeyboardButton.WithCallbackData("Tag Menu") },
                    new[] { InlineKeyboardButton.WithCallbackData("List Menu")}
                });

        await bot.SendTextMessageAsync(chatId, "Main Menu", replyMarkup: inlineKeyboard);
    }

    private static async Task ShowSubMenu(TelegramBotClient bot, long chatId, string menuName)
    {
        var submenuKeyboard = new ReplyKeyboardMarkup(new[]
        {
        new KeyboardButton[] { "Back" }
        })
        {
            ResizeKeyboard = true
        };

        await bot.SendTextMessageAsync(chatId, $"You are in the {menuName} menu. Press 'Back' to return to the main menu.", replyMarkup: submenuKeyboard);
    }
}
