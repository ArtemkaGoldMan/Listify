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

