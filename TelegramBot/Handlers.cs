using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using System.Text;
using Telegram.Bot.Types.InputFiles;

namespace TelegramBot
{
    public class Handlers
    {
        public static string Path { get; } =  Directory.GetCurrentDirectory() + @"\Загрузки\";
        /// <summary>
        /// Обработчик обновлений, пришедших от бота.
        /// </summary>
        /// <param name="botClient">Телеграм-бот.</param>
        /// <param name="update">Объект-обновление.</param>
        /// <param name="cancellationToken">Завершающий токен.</param>
        /// <returns></returns>
        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message)
            {
                if(update.Message.Text != null)
                {
                    await HandleMessageAsync(botClient, update.Message);
                    return;
                }
                else if(update.Message.Video != null || update.Message.Audio != null || update.Message.Document != null)
                {
                    await HandleDigitalContentAsync(botClient, update.Message, Path);
                    return;
                }
                
            }            
        }

        /// <summary>
        /// Обработчик сообщений.
        /// </summary>
        /// <param name="botClient">Телеграм-бот.</param>
        /// <param name="message">Пришедшее сообщение.</param>
        /// <returns></returns>
        public static async Task HandleMessageAsync(ITelegramBotClient botClient, Message message)
        {
            Console.WriteLine($"Пользователь {message.Chat.FirstName} (id: {message.Chat.Id}) написал(а): {message.Text}");
            switch (message.Text)
            {
                case "/start":
                    await StartMessageReplyAsync(botClient, message);
                    break;
                case "/files":
                    await ShowUploadFilesAsync(botClient, message, Path);
                    break;
                default:
                    await SentFileToUserAsync(botClient, message, Path);
                    break;
            }
        }

        /// <summary>
        /// Метод осуществляет отправку пользовтелю файла, если файл существует.
        /// </summary>
        /// <param name="botClient">Телеграм-бот.</param>
        /// <param name="message">Сообщение от пользователя.</param>
        /// <param name="basePath">Базовый путь к папке с загрузками.</param>
        public static async Task SentFileToUserAsync(ITelegramBotClient botClient, Message message, string basePath)
        {
            string pathFile = basePath + $@"{message.From.FirstName} {message.From.LastName}\{message.Text}";
            if(System.IO.File.Exists(pathFile))
            {
                await using Stream stream = System.IO.File.OpenRead(pathFile);
                await botClient.SendDocumentAsync(message.Chat.Id,
                                                document: new InputOnlineFile(content: stream,
                                                fileName: pathFile.Split(@"\").Last()));
            }
        }

        /// <summary>
        /// Ответ на сообщение /start.
        /// </summary>
        /// <param name="botClient">Телеграм-бот.</param>
        /// <param name="message">Сообщение от пользователя.</param>
        /// <returns></returns>
        public static async Task StartMessageReplyAsync(ITelegramBotClient botClient, Message message)
        {
            string replyText = $"Привет, {message.From.FirstName}!\n" +
                        $"Этот бот может использоваться как файлообменник:\n" +
                        $"сохранять и загружать аудио- и видеофайлы, документы, фото (в виде документов);\n" +
                        $"\n" +
                        $"Чтобы просмотерть список ранее отправленных файлов введите:\n" +
                        $"/files\n" +
                        $"Чтобы получить ранее отправленный файл введите его имя.";
            await botClient.SendTextMessageAsync(message.Chat.Id, replyText);
            return;
            
        }

        /// <summary>
        /// Обработка получения контента (аудио, фото, видео).
        /// </summary>
        /// <param name="botClient">Телеграм-бот.</param>
        /// <param name="message">Сообщение от пользователя.</param>
        /// <returns></returns>
        public static async Task HandleDigitalContentAsync(ITelegramBotClient botClient, Message message, string basePath)
        {
            Console.WriteLine($"{DateTime.Now.ToLongDateString()} {DateTime.Now.ToLongTimeString()}." +
                $" От пользователя {message.From.FirstName} {message.From.LastName} получен файл типа {message.Type}.");
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
            string path = basePath + @$"{message.From.FirstName} {message.From.LastName}\";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            await Helpers.FileDownloader(botClient, message, path);
        }

        /// <summary>
        /// Отправка файла, запрашиваемого пользователем.
        /// </summary>
        /// <param name="botClient">Телеграм-бот.</param>
        /// <param name="message">Сообщение от пользователя.</param>
        /// <param name="basePath"></param>
        /// <returns></returns>
        public static async Task ShowUploadFilesAsync(ITelegramBotClient botClient, Message message, string basePath)
        {
            string path = basePath + @$"{message.From.FirstName} {message.From.LastName}\";
            if (Directory.Exists(path))
            {
                List<string> downloadedFiles = Helpers.GetListOfDownloadedFiles(path);
                StringBuilder answerString = new StringBuilder();
                if (downloadedFiles.Count != 0)
                {
                    answerString.Append("Список загруженных файлов:\n\n");
                    foreach (string downloadedFile in downloadedFiles)
                    {
                        answerString.Append(downloadedFile);
                        answerString.Append("\n");
                    }
                }
                else
                {
                    answerString.Append("Загруженных файлов нет.");
                }
                await botClient.SendTextMessageAsync(message.Chat.Id, answerString.ToString());
            }
            else
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Загруженных файлов нет.");
            }
        }

        /// <summary>
        /// Обработчик ошибок
        /// </summary>
        /// <param name="botClient">Телеграм-бот.</param>
        /// <param name="exception">Пойманное исключение.</param>
        /// <param name="cancellationToken">Завершающий токен.</param>
        /// <returns></returns>
        public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Ошибка телеграм API:\n{apiRequestException.ErrorCode}\n{apiRequestException.Message}",
                _ => exception.ToString(),
            };
            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    }
}
