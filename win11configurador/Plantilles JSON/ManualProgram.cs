namespace win11configurador.plantillesjson
{
    public class ManualProgram
    {
        public string Category { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string FileName { get; set; }
        public string DownloadUrl { get; set; }  // ← NEW
        public bool PreviouslyInstalled { get; set; }

        public string ObtenirID() => $"{Category.ToLower()[0]}{Id:00}";
    }
}
