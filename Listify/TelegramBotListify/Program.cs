using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Net.Http.Json;

public class Program
{
    private static readonly HttpClient httpClient = new HttpClient();
    private static readonly ConcurrentDictionary<long, string> userStates = new ConcurrentDictionary<long, string>();
    private static readonly ConcurrentDictionary<long, HashSet<int>> userTagSelections = new ConcurrentDictionary<long, HashSet<int>>();

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

        var botHandler = new BotHandler(bot, httpClient, userStates, userTagSelections);

        await botHandler.SetBotCommands(); // Set the commands for the bot

        bot.OnError += botHandler.OnError;
        bot.OnMessage += async (msg, type) => await botHandler.OnMessage(msg);
        bot.OnUpdate += async (update) => await botHandler.OnUpdate(update);

        Console.WriteLine($"@{me.Username} is running... Press Enter to terminate");
        Console.ReadLine();
        cts.Cancel();
    }
}
