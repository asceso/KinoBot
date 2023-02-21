using KinopoiskApi;
using Models.App;
using Models.Enums;
using Models.KinopoiskApi;
using Services;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using CronSTD;
using DatabaseAdapter.Controllers;
using Models.Database;

namespace KinoBot
{
    internal class Program
    {
        private static readonly CronDaemon cronDaemon = new();
        private static ConfigModel config;
        private static TelegramBotClient botClient;

        private static void Main()
        {
            config = ConfigManager.ReadConfig();
            botClient = new(config.TelegramBotToken);
            Handler handler = new(config);
            botClient.StartReceiving(handler);
            if (config.RunCronAtStartup)
            {
                Task.Run(async () => await OnChronExecute());
            }
            cronDaemon.AddJob(config.ScheduleNotificationsCron, async () => await OnChronExecute());
            cronDaemon.Start();
            Console.WriteLine("Бот запущен, нажмите Enter для выхода");
            Console.ReadLine();
            cronDaemon.Stop();
        }

        private static async Task OnChronExecute()
        {
            List<UserModel> targetUsers = (await UsersController.GetUsersAsync()).Where(u => u.IsSubscribedForNotifications).ToList();
            if (!targetUsers.Any())
            {
                Console.WriteLine($"Попытка рассылки в {DateTime.Now:dd.MM.yy HH:mm:ss}, нет подходящих пользователей");
                return;
            }

            List<FilmModel> foundedFilms = ApiExecutor.GetTop100Films(config.ApiKinopoiskToken);
            List<FilmModel> firstFilms = foundedFilms.GetFirstElements(5);
            if (!firstFilms.Any())
            {
                Console.WriteLine($"Попытка рассылки в {DateTime.Now:dd.MM.yy HH:mm:ss}, не найдены фильмы, возможно не отвечает API");
                return;
            }

            List<IAlbumInputMedia> photos = new();
            List<List<InlineKeyboardButton>> markupButtons = new();
            List<InlineKeyboardButton> rowForMarkup = new();
            foreach (var film in firstFilms)
            {
                string filmUrl = ApiExecutor.CreateSSLinkForFilm(film.FilmId.ToString());
                photos.Add(new InputMediaPhoto(new InputMedia(ApiExecutor.GetFullPosterUrl(film.PosterUrl)))
                {
                    Caption = film.GetPreferName() + "\n" + filmUrl,
                    ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
                });
                rowForMarkup.Add(new InlineKeyboardButton(film.GetPreferName())
                {
                    Url = filmUrl
                });
                markupButtons.Add(new List<InlineKeyboardButton>(rowForMarkup));
                rowForMarkup.Clear();
            }
            InlineKeyboardMarkup lookUpMarkup = new(markupButtons);

            foreach (UserModel user in targetUsers)
            {
                try
                {
                    await botClient.SendMediaGroupAsync(user.Id, photos);
                    await botClient.SendTextMessageAsync(user.Id, "Еженедельная подборка топовых фильмов.\r\nЧтобы отписаться от рассылки отправь команду /unsubscribe", replyMarkup: lookUpMarkup);
                }
                catch (Exception)
                {
                    continue;
                }
            }
            Console.WriteLine($"Рассылка выполнена в {DateTime.Now:dd.MM.yy HH:mm:ss}, для {targetUsers.Count} пользователей");
        }
    }

    public class Handler : IUpdateHandler
    {
        private readonly ReplyKeyboardMarkup mainKeyboard;
        private readonly ReplyKeyboardMarkup cancelKeyboard;
        private readonly ConfigModel config;
        private readonly List<GenreConfig> enabledGenres;
        private readonly List<OperationModel> operations;

        public Handler(ConfigModel config)
        {
            //сохраняем конфиг в обработчик
            this.config = config;
            //создаем коллекцию операций
            operations = new();

            //Создаем клавиатуру с кнопкой отмены
            cancelKeyboard = new("Отмена 🚫")
            {
                ResizeKeyboard = true
            };

            //Создаем клавиатуру с кнопками ТОП поиск и включенными из конфига
            List<List<KeyboardButton>> mainKeyboardRows = new();
            List<KeyboardButton> firstRowMainKeyboard = new()
            {
                //new KeyboardButton("ТОП недели ⚡️"),
                new KeyboardButton("Найти 🔍")
            };
            mainKeyboardRows.Add(firstRowMainKeyboard);
            int inRowCounter = 0;
            List<KeyboardButton> nextRowButtons = new();
            enabledGenres = config.EnabledGenres.Where(eg => eg.IsEnabled).ToList();
            foreach (GenreConfig genreButton in enabledGenres)
            {
                nextRowButtons.Add(new KeyboardButton(genreButton.ButtonName));
                inRowCounter++;
                if (inRowCounter == 2)
                {
                    mainKeyboardRows.Add(nextRowButtons);
                    nextRowButtons = new();
                    inRowCounter = 0;
                }
            }
            if (nextRowButtons.Any())
            {
                mainKeyboardRows.Add(nextRowButtons);
            }
            mainKeyboard = new(mainKeyboardRows)
            {
                ResizeKeyboard = true
            };
        }

        /// <summary>
        /// Обработчик событий телеграм
        /// </summary>
        /// <param name="bot">клиент телеграм бота</param>
        /// <param name="update">событие</param>
        /// <param name="cancellationToken">токен для отмены</param>
        /// <returns></returns>
        public async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
        {
            TempTelegramData temp = new(update);
            //проверяем есть ли пользователь в БД
            UserModel dbUser = await UsersController.GetUserByIdAsync(temp.Uid);
            if (dbUser == null)
            {
                //постим нового юзера если не существует
                UserModel postUser = new()
                {
                    Id = temp.Uid,
                    Username = temp.Username,
                    Firstname = temp.Firstname,
                    Lastname = temp.Lastname,
                    RegistrationDate = DateTime.Now,
                    IsSubscribedForNotifications = true
                };
                await UsersController.PostUserAsync(postUser);
                dbUser = await UsersController.GetUserByIdAsync(temp.Uid);
            }

            if (dbUser == null)
            {
                return;
            }
            temp.Operation = operations.FirstOrDefault(o => o.UserId == temp.Uid);
            try
            {
                //Смотрим есть ли у пользователя операция
                if (temp.Operation != null)
                {
                    //кнопка отмены
                    if (temp.Message == "Отмена 🚫")
                    {
                        operations.Remove(temp.Operation);
                        await bot.SendTextMessageAsync(temp.Uid, "Добро пожаловать в кино-бота, используйте клавиатуру для работы!", replyMarkup: mainKeyboard, cancellationToken: cancellationToken);
                        return;
                    }

                    //проверяем ввод ключевого слова
                    if (temp.Operation.OperationType == Operation.OperationType.WaitKeywordForSearch)
                    {
                        List<FilmModel> foundedFilms = ApiExecutor.GetFilmsByKeyword(temp.Message, config.ApiKinopoiskToken);
                        List<FilmModel> firstFilms = foundedFilms.GetFirstElements(5);

                        await bot.SendTextMessageAsync(temp.Uid, "Высылаю первые 5 результатов!", replyMarkup: mainKeyboard, cancellationToken: cancellationToken);
                        foreach (FilmModel film in firstFilms)
                        {
                            InlineKeyboardMarkup lookUpMarkup = new(new InlineKeyboardButton("Смотреть 🥰")
                            {
                                Url = ApiExecutor.CreateSSLinkForFilm(film.KinopoiskId.ToString()),
                            });

                            await bot.SendPhotoAsync(
                                temp.Uid,
                                new InputOnlineFile(film.PosterUrl),
                                film.GetPostCaption(),
                                replyMarkup: lookUpMarkup,
                                cancellationToken: cancellationToken);
                        }
                        operations.Remove(temp.Operation);
                        return;
                    }
                }
                //Если нет проверяем сообщение
                else
                {
                    //обработка сообщения старт
                    if (temp.Message == "/start" || temp.Message == "Отмена 🚫")
                    {
                        await bot.SendTextMessageAsync(temp.Uid, "Добро пожаловать в кино-бота, используйте клавиатуру для работы!", replyMarkup: mainKeyboard, cancellationToken: cancellationToken);
                        return;
                    }

                    //обработка сообщения найти
                    if (temp.Message == "Найти 🔍")
                    {
                        operations.Add(new(temp.Uid, Operation.OperationType.WaitKeywordForSearch));
                        await bot.SendTextMessageAsync(temp.Uid, "Введите ключевую фразу для поиска, или нажмите отмену", replyMarkup: cancelKeyboard, cancellationToken: cancellationToken);
                        return;
                    }

                    //обработка отказа от рассылки
                    if (temp.Message == "/unsubscribe")
                    {
                        dbUser.IsSubscribedForNotifications = false;
                        await UsersController.PutUserAsync(dbUser);
                        await bot.SendTextMessageAsync(
                            temp.Uid,
                            "Вы успешно отписались от рассылки, если хотите снова видеть крутые новинки отправьте боту команду /subscribe",
                            cancellationToken: cancellationToken
                            );
                        return;
                    }

                    //обработка подписания на рассылку
                    if (temp.Message == "/subscribe")
                    {
                        dbUser.IsSubscribedForNotifications = true;
                        await UsersController.PutUserAsync(dbUser);
                        await bot.SendTextMessageAsync(
                            temp.Uid,
                            "Вы успешно подписались на рассылку крутых новинок, чтобы отписаться от рассылки отправьте боту команду /unsubscribe",
                            cancellationToken: cancellationToken
                            );
                        return;
                    }

                    //обработка фильмов по жанрам
                    GenreConfig foundedGenre = enabledGenres.FirstOrDefault(eg => eg.ButtonName == temp.Message);
                    if (foundedGenre != null)
                    {
                        List<FilmModel> foundedFilms = ApiExecutor.GetFilmsByGenre(foundedGenre.Id, config.ApiKinopoiskToken);
                        FilmModel randomFilm = foundedFilms.GetRandom();
                        InlineKeyboardMarkup lookUpMarkup = new(new InlineKeyboardButton("Смотреть 🥰")
                        {
                            Url = ApiExecutor.CreateSSLinkForFilm(randomFilm.KinopoiskId.ToString()),
                        });

                        await bot.SendPhotoAsync(
                            temp.Uid,
                            new InputOnlineFile(randomFilm.PosterUrl),
                            randomFilm.GetPostCaption(),
                            replyMarkup: lookUpMarkup,
                            cancellationToken: cancellationToken);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Обработчик ошибок телеграм
        /// </summary>
        /// <param name="bot">клиент телеграм бота</param>
        /// <param name="exception">ошибка</param>
        /// <param name="cancellationToken">токен для отмены</param>
        /// <returns></returns>
        public Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(exception.Message);
            Console.ReadLine();
            Environment.Exit(-1);
            return Task.CompletedTask;
        }
    }
}