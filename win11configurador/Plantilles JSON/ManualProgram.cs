using Newtonsoft.Json;

namespace win11configurador.plantillesjson
{
    public class ManualProgram
    {
        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("filename")]
        public string FileName { get; set; }

        [JsonProperty("direct_download_url")]
        public string DownloadUrl { get; set; }

        [JsonProperty("previously_installed")]
        public bool PreviouslyInstalled { get; set; }

        public string ObtenirID() => $"{Category?.ToLower()[0]}{Id:00}";
    }
}
