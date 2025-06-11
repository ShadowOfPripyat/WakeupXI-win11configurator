using Newtonsoft.Json;

namespace win11configurador.plantillesjson
{
    public class ConfigurationItem
    {
        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("command")]
        public string Command { get; set; }

        [JsonProperty("check_command")]
        public string CheckCommand { get; set; }

        [JsonProperty("operation_type")]
        public string OperationType { get; set; }

        [JsonProperty("revert_command")]
        public string RevertCommand { get; set; }

        [JsonProperty("previouslyapplied")]
        public bool PreviouslyApplied { get; set; }

        public string ObtenirID() => $"{Category?.ToLower()[0]}{Id:00}";
    }
}
