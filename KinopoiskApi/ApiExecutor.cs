using Models.KinopoiskApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace KinopoiskApi
{
    /// <summary>
    /// Класс вызова АПИ
    /// </summary>
    public class ApiExecutor
    {
        /// <summary>
        /// Метод создает ссылку SS из кинопоиск ИД
        /// </summary>
        /// <param name="id">ИД фильма на кинопоиске</param>
        /// <returns>ссылка формата https://www.sspoisk.ru/film/@ID/</returns>
        public static string CreateSSLinkForFilm(string id) => "https://www.sspoisk.ru/film/@ID/".Replace("@ID", id);

        /// <summary>
        /// Найти фильмы по жанру
        /// </summary>
        /// <param name="genreId">ИД жанра</param>
        /// <param name="apiToken">АПИ ключ</param>
        /// <returns>Список фильмов</returns>
        public static List<FilmModel> GetFilmsByGenre(int genreId, string apiToken)
        {
            RestClient client = new("https://kinopoiskapiunofficial.tech/api/v2.2/films?genres=" + genreId);
            RestRequest request = new();
            request.Method = Method.Get;
            request.AddHeader("X-API-KEY", apiToken);
            RestResponse response = client.Execute(request);
            List<FilmModel> films = new();
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                try
                {
                    JObject jsonData = JObject.Parse(response.Content);
                    foreach (JToken filmJson in jsonData["items"].ToArray())
                    {
                        FilmModel model = JsonConvert.DeserializeObject<FilmModel>(filmJson.ToString());
                        films.Add(model);
                    }
                    return films;
                }
                catch (Exception)
                {
                    return new();
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Найти фильмы по ключевому слову
        /// </summary>
        /// <param name="keyword">ключевое слово</param>
        /// <param name="apiToken">АПИ ключ</param>
        /// <returns>Список фильмов</returns>
        public static List<FilmModel> GetFilmsByKeyword(string keyword, string apiToken)
        {
            RestClient client = new("https://kinopoiskapiunofficial.tech/api/v2.2/films?keyword=" + keyword);
            RestRequest request = new();
            request.Method = Method.Get;
            request.AddHeader("X-API-KEY", apiToken);
            RestResponse response = client.Execute(request);
            List<FilmModel> films = new();
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                try
                {
                    JObject jsonData = JObject.Parse(response.Content);
                    foreach (JToken filmJson in jsonData["items"].ToArray())
                    {
                        FilmModel model = JsonConvert.DeserializeObject<FilmModel>(filmJson.ToString());
                        films.Add(model);
                    }
                    return films;
                }
                catch (Exception)
                {
                    return new();
                }
            }
            else
            {
                return null;
            }
        }
    }
}