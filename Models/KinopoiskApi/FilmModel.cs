using Newtonsoft.Json;

namespace Models.KinopoiskApi
{
    public class FilmModel
    {
        [JsonProperty("filmId")]
        public int FilmId { get; set; }

        [JsonProperty("kinopoiskId")]
        public int KinopoiskId { get; set; }

        [JsonProperty("nameRu")]
        public string NameRu { get; set; }

        [JsonProperty("nameEn")]
        public string NameEn { get; set; }

        [JsonProperty("nameOriginal")]
        public string NameOriginal { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("year")]
        public string Year { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("filmLength")]
        public string FilmLength { get; set; }

        [JsonProperty("countries")]
        public CountryModel[] Countries { get; set; }

        [JsonProperty("genres")]
        public GenreModel[] Genres { get; set; }

        [JsonProperty("ratingKinopoisk")]
        public string RatingKinopoisk { get; set; }

        [JsonProperty("ratingImdb")]
        public string RatingImdb { get; set; }

        [JsonProperty("ratingVoteCount")]
        public int RatingVoteCount { get; set; }

        [JsonProperty("posterUrl")]
        public string PosterUrl { get; set; }

        [JsonProperty("posterUrlPreview")]
        public string PosterUrlPreview { get; set; }

        public string GetPreferName()
        {
            if (!string.IsNullOrEmpty(NameRu))
            {
                return NameRu;
            }
            else if (!string.IsNullOrEmpty(NameEn))
            {
                return NameEn;
            }
            else
            {
                return NameOriginal;
            }
        }

        public string GetPostCaption()
        {
            return $"Название: {GetPreferName()}\r\n" +
                   $"Рейтинг кинопоиск | IMDB: {RatingKinopoisk} | {RatingImdb}\r\n" +
                   $"Год: {Year}";
        }
    }

    public class CountryModel
    {
        [JsonProperty("country")]
        public string Country { get; set; }
    }

    public class GenreModel
    {
        [JsonProperty("genre")]
        public string Genre { get; set; }
    }
}