using Models.App;
using Newtonsoft.Json;

namespace Services
{
    /// <summary>
    /// Класс управляет конфигом бота
    /// </summary>
    public class ConfigManager
    {
        /// <summary>
        /// Считать конфиг
        /// </summary>
        /// <returns>конфиг</returns>
        public static ConfigModel ReadConfig()
        {
            using StreamReader reader = new(Environment.CurrentDirectory + "/config.json");
            return JsonConvert.DeserializeObject<ConfigModel>(reader.ReadToEnd());
        }

        /// <summary>
        /// Записать конфиг
        /// </summary>
        /// <param name="config">конфиг</param>
        public static void SaveConfig(ConfigModel config)
        {
            using StreamWriter writer = new(Environment.CurrentDirectory + "/config.json");
            writer.Write(JsonConvert.SerializeObject(config, Formatting.Indented));
            writer.Close();
        }
    }
}