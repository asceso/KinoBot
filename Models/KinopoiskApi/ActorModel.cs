using Newtonsoft.Json;

namespace Models.KinopoiskApi
{
    public class ActorModel
    {
        [JsonProperty("kinopoiskId")]
        public int KinopoiskId { get; set; }

        [JsonProperty("webUrl")]
        public string WebUrl { get; set; }

        [JsonProperty("nameRu")]
        public string NameRu { get; set; }

        [JsonProperty("nameEn")]
        public string NameEn { get; set; }

        [JsonProperty("sex")]
        public string Sex { get; set; }

        [JsonProperty("posterUrl")]
        public string PosterUrl { get; set; }

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
                return "Нет имени";
            }
        }
    }
}