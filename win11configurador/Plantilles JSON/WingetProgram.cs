using Newtonsoft.Json;

namespace win11configurador.plantillesjson
{
    public class WingetProgram
    {
        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("winget_packageid")]
        public string WingetPackageId { get; set; }

        [JsonProperty("previously_installed")]
        public bool PreviouslyInstalled { get; set; }

        public string ObtenirID() => $"{Category.ToLower()[0]}{Id:00}";
    }
}
