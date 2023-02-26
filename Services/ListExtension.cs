using Models.KinopoiskApi;

namespace Services
{
    /// <summary>
    /// Расширение для списков
    /// </summary>
    public static class ListExtension
    {
        /// <summary>
        /// Получить рандомный элемент из списка
        /// </summary>
        /// <param name="source">список</param>
        /// <returns>рандомный элемент</returns>
        public static FilmModel GetRandom(this List<FilmModel> source)
        {
            Random rnd = new();
            return source[rnd.Next(source.Count)];
        }

        /// <summary>
        /// Отрезать первые N элементов из списка
        /// </summary>
        /// <param name="source">список - источник</param>
        /// <param name="count">кол-во элементов для отрезания</param>
        /// <returns>первые N элементов из списка</returns>
        public static List<TData> GetFirstElements<TData>(this List<TData> source, int count)
        {
            List<TData> result = new();
            int counter = 0;
            foreach (TData model in source)
            {
                result.Add(model);
                counter++;
                if (counter >= count)
                {
                    break;
                }
            }
            return result;
        }
    }
}