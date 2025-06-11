namespace win11configurador.coses
{
    public class ConfigurationItem
    {
        public string Category { get; set; }
        public int Id { get; set; }
        public string Title { get; set; }
        public string Command { get; set; }
        public bool AlreadyDone { get; set; }

        public string GetDisplayId() => $"{Category.ToLower()[0]}{Id:00}";
    }
}
