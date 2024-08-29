using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using System.Net.Http.Json;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Identity;
using Telegram.Bot.Polling;
using TelegramBotListify.Services;

public class BotHandler
{
    private readonly TelegramBotClient _bot;
    private readonly HttpClient _httpClient;
    private readonly Helper _helper;
    private readonly ConcurrentDictionary<long, string> _userStates;
    private readonly ConcurrentDictionary<long, HashSet<int>> _userTagSelections;
    private readonly ContentManager _contentManager;
    private readonly TagManager _tagManager;
    private readonly UserManager _userManager;

    public BotHandler(TelegramBotClient bot, HttpClient httpClient, ConcurrentDictionary<long, string> userStates, ConcurrentDictionary<long, HashSet<int>> userTagSelections)
    {
        _bot = bot;
        _httpClient = httpClient;
        _userStates = userStates;
        _contentManager = new ContentManager(bot, httpClient, userStates, userTagSelections);
        _tagManager = new TagManager(bot, httpClient, userStates);
        _userManager = new UserManager(bot, httpClient, userStates);
        _helper = new Helper(_bot);
    }

    public async Task SetBotCommands()
    {
        var commands = new List<BotCommand>
        {
            new BotCommand { Command = "start", Description = "Start the bot and register it at the same time" },
            new BotCommand { Command = "delete", Description = "Delete your user from db" }
        };

        await _bot.SetMyCommandsAsync(commands);
    }

    public async Task OnError(Exception exception, HandleErrorSource source)
    {
        Console.WriteLine(exception);
    }

    public async Task OnMessage(Message msg)
    {
        if (_userStates.TryGetValue(msg.Chat.Id, out var state))
        {
            if (state == "awaitingContentName")
            {
                await _contentManager.HandleContentNameInput(msg);
            }
            else if (state == "awaitingTagName")
            {
                await _tagManager.HandleTagNameInput(msg);
            }
        }
        else
        {
            switch (msg.Text)
            {
                case "/start":
                    //await _bot.DeleteMessageAsync(msg.Chat.Id, msg.MessageId); // it is removed due to deleting chat by telegram because of no message((( 
                    await _userManager.HandleStartCommand(msg);
                    break;

                case "/delete":
                    await _bot.DeleteMessageAsync(msg.Chat.Id, msg.MessageId);
                    await _userManager.HandleDeleteCommand(msg);
                    break;

                default:
                    await _helper.SendAndDeleteMessageAsync(msg.Chat, "Do not do that");
                    await _bot.DeleteMessageAsync(msg.Chat.Id, msg.MessageId);
                    break;
            }
        }
    }

    public async Task OnUpdate(Update update)
    {
        if (update is { CallbackQuery: { } query })
        {
            await _bot.DeleteMessageAsync(query.Message!.Chat.Id, query.Message.MessageId);

            var callbackData = query.Data!.Split(':');
            var action = callbackData[0];

            switch (action)
            {
                //--------------------------Menus----------------
                case "Content Menu":
                    await _contentManager.ShowContentMenu(query.Message.Chat.Id);
                    break;

                case "Tag Menu":
                    await _tagManager.ShowTagMenu(query.Message.Chat.Id);
                    break;

                case "Main Menu":
                    await _userManager.ShowMainMenu(query.Message.Chat.Id);
                    break;

                //--------------------------TAGS----------------
                case "Create Tag":
                    await _tagManager.HandleCreateTag(query.Message.Chat.Id);
                    break;

                case "Delete Tag":
                    var tagId = int.Parse(callbackData[1]);
                    await _tagManager.HandleDeleteTag(query.Message.Chat.Id, tagId);
                    break;

                //--------------------------Contants----------------

                case "Show Contents":
                    await _contentManager.ShowContents(query.Message.Chat.Id);
                    break;

                case "One Content Managing":
                    var contentId = int.Parse(callbackData[1]);
                    await _contentManager.HandleContentOptions(query.Message.Chat.Id, contentId);
                    break;

                case "Add Content":
                    await _contentManager.AddContent(query.Message.Chat.Id);
                    break;

                case "Delete Content":
                    await _contentManager.ShowContentsForDeletion(query.Message.Chat.Id);
                    break;

                case "deleteContent":
                    contentId = int.Parse(callbackData[1]);
                    await _contentManager.HandleDeleteContent(query.Message.Chat.Id, contentId);
                    break;

                case "addTag":
                    contentId = int.Parse(callbackData[1]);
                    await _contentManager.ShowTagsForAdding(query.Message.Chat.Id, contentId);
                    break;

                case "addTagToContent":
                    contentId = int.Parse(callbackData[1]);
                    tagId = int.Parse(callbackData[2]);
                    await _contentManager.AddTagToContent(query.Message.Chat.Id, contentId, tagId);
                    break;

                case "showRemoveTag":
                    contentId = int.Parse(callbackData[1]);
                    await _contentManager.ShowRemoveTagMenu(query.Message.Chat.Id, contentId);
                    break;

                case "removeTagFromContent":
                    contentId = int.Parse(callbackData[1]);
                    tagId = int.Parse(callbackData[2]);
                    await _contentManager.RemoveTagFromContent(query.Message.Chat.Id, contentId, tagId);
                    break;
         
                case "Show with filter":
                    await _contentManager.ShowContentsToList(query.Message.Chat.Id);
                    break;

                // Handle tag selection for filtering
                case "countTag":
                    var tagIdToCount = int.Parse(callbackData[1]);
                    await _contentManager.HandleTagSelection(query.Message.Chat.Id, tagIdToCount, true);
                    break;

                case "uncountTag":
                    var tagIdToUncount = int.Parse(callbackData[1]);
                    await _contentManager.HandleTagSelection(query.Message.Chat.Id, tagIdToUncount, false);
                    break;


                default:
                    await _bot.AnswerCallbackQueryAsync(query.Id, $"Unknown command: {query.Data}");
                    break;
            }
        }
    }

}