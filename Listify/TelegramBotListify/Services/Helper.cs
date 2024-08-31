using System;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotListify.Services
{
	public class Helper
	{
        private readonly ITelegramBotClient _bot;

        public Helper(ITelegramBotClient bot)
        {
            _bot = bot;
        }

        /// <summary>
        /// Sends a text message to a specified chat and deletes it after a delay.
        /// </summary>
        /// <param name="chatId">The chat ID where the message will be sent.</param>
        /// <param name="text">The text of the message to be sent.</param>
        /// <param name="delayMilliseconds">The delay in milliseconds before the message is deleted. Default is 5000 milliseconds.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SendAndDeleteMessageAsync(ChatId chatId, string text, int delayMilliseconds = 1000)
        {
            var sentMessage = await _bot.SendTextMessageAsync(chatId, text);
            await Task.Delay(delayMilliseconds);
            await _bot.DeleteMessageAsync(chatId, sentMessage.MessageId);
        }

        public async Task DeleteAllMessagesExceptFirstAsync(long chatId)
        {
            var offset = -1;
            var messages = new List<Message>();

            // Step 1: Fetch all messages in the chat
            while (true)
            {
                var updates = await _bot.GetUpdatesAsync(offset + 1);
                if (updates.Length == 0) break;

                foreach (var update in updates)
                {
                    if (update.Message != null && update.Message.Chat.Id == chatId)
                    {
                        messages.Add(update.Message);
                        offset = update.Id;
                    }
                }
            }

            // Step 2: Delete all messages except the first one
            if (messages.Count > 1)
            {
                for (int i = 1; i < messages.Count; i++)
                {
                    try
                    {
                        await _bot.DeleteMessageAsync(chatId, messages[i].MessageId);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting message {messages[i].MessageId}: {ex.Message}");
                    }
                }
            }
        }


        public static InlineKeyboardMarkup mainMenuKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Content Menu"), InlineKeyboardButton.WithCallbackData("Tag Menu") }
        });

        public static InlineKeyboardMarkup contentMenuKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Show Contents"), InlineKeyboardButton.WithCallbackData("Show with filter") },
            new[] { InlineKeyboardButton.WithCallbackData("Add Content"), InlineKeyboardButton.WithCallbackData("Delete Content") },
            new[] { InlineKeyboardButton.WithCallbackData("Main Menu") }
        });
    }
}

