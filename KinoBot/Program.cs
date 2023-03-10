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
using static Models.Enums.Operation;
using Telegram.Bot.Types.Enums;
using System.Runtime.CompilerServices;
using System.Threading;

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
            botClient.StartReceiving(
                handler,
                receiverOptions: new ReceiverOptions()
                {
                    AllowedUpdates = new UpdateType[]
                    {
                        UpdateType.Message,
                        UpdateType.CallbackQuery
                    }
                });
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
                    ParseMode = ParseMode.Html
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
        private readonly ReplyKeyboardMarkup yearsKeyboard;
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
            //Создаем клавиатуру с кнопками годов
            List<List<KeyboardButton>> yearsKeyboardRows = new();
            List<KeyboardButton> firstRowYearsKeyboard = new()
            {
                new KeyboardButton("80-е"),
                new KeyboardButton("90-е")
            };
            yearsKeyboardRows.Add(firstRowYearsKeyboard);
            List<KeyboardButton> secondRowYearsKeyboard = new()
            {
                new KeyboardButton(DateTime.Now.Year.ToString()),
                new KeyboardButton(DateTime.Now.AddYears(-1).Year.ToString())
            };
            yearsKeyboardRows.Add(secondRowYearsKeyboard);
            List<KeyboardButton> thirdRowYearsKeyboard = new()
            {
                new KeyboardButton("Я знаю точный год")
            };
            yearsKeyboardRows.Add(thirdRowYearsKeyboard);
            List<KeyboardButton> cancelRowYearsKeyboard = new()
            {
                new KeyboardButton("Отмена 🚫")
            };
            yearsKeyboardRows.Add(cancelRowYearsKeyboard);
            yearsKeyboard = new(yearsKeyboardRows)
            {
                ResizeKeyboard = true
            };

            //Создаем клавиатуру с кнопками ТОП поиск и включенными из конфига
            List<List<KeyboardButton>> mainKeyboardRows = new();
            List<KeyboardButton> firstRowMainKeyboard = new()
            {
                new KeyboardButton("Найти по фразе 🔍"),
                new KeyboardButton("Найти по актеру 🔍")
            };
            mainKeyboardRows.Add(firstRowMainKeyboard);
            List<KeyboardButton> secondRowMainKeyboard = new()
            {
                new KeyboardButton("Найти по годам 🔍")
            };
            mainKeyboardRows.Add(secondRowMainKeyboard);
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

            if (!string.IsNullOrEmpty(temp.Callback))
            {
                try
                {
                    string[] callbackParams = temp.Callback.Split('|');
                    CallbackType callbackType = (CallbackType)int.Parse(callbackParams[0]);

                    if (callbackType == CallbackType.SearchByActor)
                    {
                        string actorId = callbackParams[1];
                        List<FilmModel> foundedFilms = ApiExecutor.GetFilmsByActor(config.ApiKinopoiskToken, actorId);
                        List<FilmModel> firstFilms = foundedFilms.Where(f => f.General).DistinctBy(f => f.FilmId).ToList().GetFirstElements(5);

                        if (!firstFilms.Any())
                        {
                            await bot.SendTextMessageAsync(temp.Uid, "Не удалоось найти фильмы по актеру, повторите попытку снова или позднее!", cancellationToken: cancellationToken);
                            return;
                        }

                        string mainMessage = "Список самых популярных фильмов где выбранный актер в главных ролях:\r\n";
                        foreach (FilmModel film in firstFilms)
                        {
                            mainMessage += $"[{film.GetPreferName()}]({ApiExecutor.CreateSSLinkForFilm(film.FilmId.ToString())})\r\n";
                        }
                        mainMessage = mainMessage.Replace("-", "\\-");
                        await bot.SendTextMessageAsync(temp.Uid, mainMessage, parseMode: ParseMode.MarkdownV2);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return;
                }
                return;
            }

            temp.Operation = operations.FirstOrDefault(o => o.UserId == temp.Uid);
            try
            {
                //Смотрим есть ли у пользователя операция
                if (temp.Operation != null)
                {
                    string userArgument = temp.Message;

                    //кнопка отмены
                    if (userArgument == "Отмена 🚫")
                    {
                        operations.Remove(temp.Operation);
                        await bot.SendTextMessageAsync(temp.Uid, "Добро пожаловать в кино-бота, используйте клавиатуру для работы!", replyMarkup: mainKeyboard, cancellationToken: cancellationToken);
                        return;
                    }

                    //проверяем ввод имени актера
                    if (temp.Operation.OperationType == OperationType.WaitActorInfoForSearch)
                    {
                        List<ActorModel> foundedActors = ApiExecutor.FindPersons(userArgument, config.ApiKinopoiskToken);
                        List<ActorModel> firstActors = foundedActors.GetFirstElements(5);

                        await bot.SendTextMessageAsync(temp.Uid, "Высылаю первые 5 результатов!", replyMarkup: mainKeyboard, cancellationToken: cancellationToken);
                        foreach (ActorModel actor in firstActors)
                        {
                            List<List<InlineKeyboardButton>> markups = new()
                            {
                                new List<InlineKeyboardButton>()
                                {
                                    new InlineKeyboardButton("Профиль на кинопоиске 👤")
                                    {
                                        Url = actor.WebUrl
                                    }
                                },
                                new List<InlineKeyboardButton>()
                                {
                                    new InlineKeyboardButton("Искать фильмы с актером 🎬")
                                    {
                                        CallbackData = $"{(int)CallbackType.SearchByActor}|{actor.KinopoiskId}"
                                    }
                                }
                            };
                            InlineKeyboardMarkup lookUpMarkup = new(markups);

                            await bot.SendPhotoAsync(
                                temp.Uid,
                                new InputOnlineFile(ApiExecutor.GetFullPosterUrl(actor.PosterUrl)),
                                actor.GetPreferName(),
                                replyMarkup: lookUpMarkup,
                                cancellationToken: cancellationToken);
                        }
                        operations.Remove(temp.Operation);
                        return;
                    }

                    //проверяем ввод ключевого слова
                    if (temp.Operation.OperationType == OperationType.WaitKeywordForSearch)
                    {
                        List<FilmModel> foundedFilms = ApiExecutor.GetFilmsByKeyword(userArgument, config.ApiKinopoiskToken);
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
                                new InputOnlineFile(ApiExecutor.GetFullPosterUrl(film.PosterUrl)),
                                film.GetPostCaption(),
                                replyMarkup: lookUpMarkup,
                                cancellationToken: cancellationToken);
                        }
                        operations.Remove(temp.Operation);
                        return;
                    }

                    //проверяем ввод годов для поиска
                    if (temp.Operation.OperationType == OperationType.WaitYearsForSearch)
                    {
                        string currentYear = DateTime.Now.Year.ToString();
                        string previousYear = DateTime.Now.AddYears(-1).Year.ToString();

                        if (userArgument == currentYear || userArgument == previousYear)
                        {
                            await ProcessYearsSearchForUserAsync(bot, temp.Uid, userArgument, userArgument, temp.Operation);
                            return;
                        }
                        else
                        {
                            switch (userArgument)
                            {
                                case "80-е":
                                    await ProcessYearsSearchForUserAsync(bot, temp.Uid, "1980", "1990", temp.Operation);
                                    return;

                                case "90-е":
                                    await ProcessYearsSearchForUserAsync(bot, temp.Uid, "1990", "2000", temp.Operation);
                                    return;

                                case "Я знаю точный год":
                                    operations.Remove(temp.Operation);
                                    operations.Add(new(temp.Uid, OperationType.WaitAccuracyYearToSearch));
                                    await bot.SendTextMessageAsync(
                                        temp.Uid,
                                        "Пожалуйста введите нужный год, или нажмите отмену",
                                        replyMarkup: cancelKeyboard,
                                        cancellationToken: cancellationToken
                                        );
                                    return;

                                default:
                                    await bot.SendTextMessageAsync(
                                        temp.Uid,
                                        "Пожалуйста выберите вариант из клавиатуры, или нажмите отмену",
                                        replyMarkup: yearsKeyboard,
                                        cancellationToken: cancellationToken
                                        );
                                    return;
                            }
                        }
                    }

                    //проверяем ввод точного года для поиска
                    if (temp.Operation.OperationType == OperationType.WaitAccuracyYearToSearch)
                    {
                        await ProcessYearsSearchForUserAsync(bot, temp.Uid, userArgument, userArgument, temp.Operation);
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

                    //обработка сообщения найти по фразе
                    if (temp.Message == "Найти по фразе 🔍")
                    {
                        operations.Add(new(temp.Uid, OperationType.WaitKeywordForSearch));
                        await bot.SendTextMessageAsync(temp.Uid, "Введите ключевую фразу для поиска, или нажмите отмену", replyMarkup: cancelKeyboard, cancellationToken: cancellationToken);
                        return;
                    }
                    //обработка сообщения найти по актеру
                    if (temp.Message == "Найти по актеру 🔍")
                    {
                        operations.Add(new(temp.Uid, OperationType.WaitActorInfoForSearch));
                        await bot.SendTextMessageAsync(temp.Uid, "Введите имя или фамилию актера для поиска, или нажмите отмену", replyMarkup: cancelKeyboard, cancellationToken: cancellationToken);
                        return;
                    }
                    //обработка сообщения найти по годам
                    if (temp.Message == "Найти по годам 🔍")
                    {
                        operations.Add(new(temp.Uid, OperationType.WaitYearsForSearch));
                        await bot.SendTextMessageAsync(temp.Uid, "Выберите вариант из клавиатуры, или нажмите отмену", replyMarkup: yearsKeyboard, cancellationToken: cancellationToken);
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

        private async Task ProcessYearsSearchForUserAsync(ITelegramBotClient bot, long userId, string yearFrom, string yearTo, OperationModel operation)
        {
            List<FilmModel> foundedFilms = ApiExecutor.GetFilmsByYears(yearFrom, yearTo, config.ApiKinopoiskToken);
            List<FilmModel> firstFilms = foundedFilms.GetFirstElements(5);

            await bot.SendTextMessageAsync(userId, "Высылаю первые 5 результатов!", replyMarkup: mainKeyboard);
            foreach (FilmModel film in firstFilms)
            {
                InlineKeyboardMarkup lookUpMarkup = new(new InlineKeyboardButton("Смотреть 🥰")
                {
                    Url = ApiExecutor.CreateSSLinkForFilm(film.KinopoiskId.ToString()),
                });

                await bot.SendPhotoAsync(
                    userId,
                    new InputOnlineFile(ApiExecutor.GetFullPosterUrl(film.PosterUrl)),
                    film.GetPostCaption(),
                    replyMarkup: lookUpMarkup
                    );
            }
            operations.Remove(operation);
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