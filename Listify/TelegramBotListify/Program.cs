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
            new BotCommand { Command = "create", Description = "Register a new user" },
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
        if (msg.Text == "/start")
        {
            await bot.SendTextMessageAsync(msg.Chat, "Welcome! Pick one direction",
                replyMarkup: new InlineKeyboardMarkup().AddButtons("Left", "Right"));
        }
        else if (msg.Text.StartsWith("/create"))
        {
            
            var telegramUserId = msg.From.Id.ToString();

            var userResponse = await httpClient.GetAsync($"api/Users/telegram/{telegramUserId}");
            if (!userResponse.IsSuccessStatusCode)
            {
                var userDto = new UserDTO
                {
                    TelegramUserID = telegramUserId
                };

                var response = await httpClient.PostAsJsonAsync("api/Users/CreateUser", userDto);
                if (response.IsSuccessStatusCode)
                {
                    var createdUser = await response.Content.ReadFromJsonAsync<UserDTO>();
                    await bot.SendTextMessageAsync(msg.Chat, $"User created with ID {createdUser.UserID}");
                }
                else
                {
                    await bot.SendTextMessageAsync(msg.Chat, "Failed to create user.");
                }
            }
            else
            {
                await bot.SendTextMessageAsync(msg.Chat, "User has already been created.");
            }
        }
        else if (msg.Text.StartsWith("/delete"))
        {

            var telegramUserId = msg.From.Id.ToString();
            //find user by telegram id
            var userResponse = await httpClient.GetAsync($"api/Users/telegram/{telegramUserId}");
            if (userResponse.IsSuccessStatusCode)
            {
                var user = await userResponse.Content.ReadFromJsonAsync<UserDTO>();

                // If user is found, delete the user
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
    }

    private static async Task OnUpdate(TelegramBotClient bot, Update update)
    {
        if (update is { CallbackQuery: { } query })
        {
            await bot.AnswerCallbackQueryAsync(query.Id, $"You picked {query.Data}");
            await bot.SendTextMessageAsync(query.Message!.Chat, $"User {query.From} clicked on {query.Data}");
        }
    }
}
