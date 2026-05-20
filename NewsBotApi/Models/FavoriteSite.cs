namespace NewsBotApi.Models
{
    public class FavoriteSite
    {
        public int Id { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public string SiteUrl { get; set; } = string.Empty;
    }
}
