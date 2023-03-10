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
        public static string GetFullPosterUrl(string url)
        {
            RestClient client = new(url);
            RestRequest request = new();
            request.Method = Method.Head;
            RestResponse response = client.Execute(request);
            if (response.ResponseUri == null)
            {
                return url;
            }
            return response.ResponseUri.ToString();
        }

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
        /// Найти фильмы по жанру
        /// </summary>
        /// <param name="yearFrom">год начала</param>
        /// <param name="yearTo">год конца</param>
        /// <param name="apiToken">АПИ ключ</param>
        /// <returns>Список фильмов</returns>
        public static List<FilmModel> GetFilmsByYears(string yearFrom, string yearTo, string apiToken)
        {
            RestClient client = new($"https://kinopoiskapiunofficial.tech/api/v2.2/films?yearFrom={yearFrom}&yearTo={yearTo}");
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

        public static List<FilmModel> GetTop100Films(string apiToken)
        {
            RestClient client = new("https://kinopoiskapiunofficial.tech/api/v2.2/films/top?type=TOP_100_POPULAR_FILMS");
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
                    foreach (JToken filmJson in jsonData["films"].ToArray())
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

        public static List<FilmModel> GetFilmsByActor(string apiToken, string actorId)
        {
            RestClient client = new("https://kinopoiskapiunofficial.tech/api/v1/staff/" + actorId);
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
                    foreach (JToken filmJson in jsonData["films"].ToArray())
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

        public static List<ActorModel> FindPersons(string name, string apiToken)
        {
            RestClient client = new("https://kinopoiskapiunofficial.tech/api/v1/persons?name=" + name);
            RestRequest request = new();
            request.Method = Method.Get;
            request.AddHeader("X-API-KEY", apiToken);
            RestResponse response = client.Execute(request);
            List<ActorModel> persons = new();
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                try
                {
                    JObject jsonData = JObject.Parse(response.Content);
                    foreach (JToken actorJson in jsonData["items"].ToArray())
                    {
                        ActorModel model = JsonConvert.DeserializeObject<ActorModel>(actorJson.ToString());
                        persons.Add(model);
                    }
                    return persons;
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