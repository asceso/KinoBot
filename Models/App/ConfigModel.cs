namespace Models.App
{
    /// <summary>
    /// Конфиг бота
    /// </summary>
    public class ConfigModel
    {
        /// <summary>
        /// Токен телеграм
        /// </summary>
        public string TelegramBotToken { get; set; }

        /// <summary>
        /// Токен апи кинопоиска
        /// </summary>
        public string ApiKinopoiskToken { get; set; }

        /// <summary>
        /// Расписание для рассылки, https://crontab.guru/
        /// </summary>
        public string ScheduleNotificationsCron { get; set; }

        /// <summary>
        /// Запустить рассылку при старте
        /// </summary>
        public bool RunCronAtStartup { get; set; }

        /// <summary>
        /// Включенные кнопки жанров
        /// </summary>
        public List<GenreConfig> EnabledGenres { get; set; }

        public ConfigModel()
        {
            EnabledGenres = new();
        }
    }

    public class GenreConfig
    {
        /// <summary>
        /// ИД для апи
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// название в кнопке
        /// </summary>
        public string ButtonName { get; set; }

        /// <summary>
        /// включена ли кнопка
        /// </summary>
        public bool IsEnabled { get; set; }

        public GenreConfig(int id, string buttonName, bool isEnabled = true)
        {
            Id = id;
            ButtonName = buttonName;
            IsEnabled = isEnabled;
        }
    }
}