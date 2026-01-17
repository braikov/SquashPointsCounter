namespace Squash.Web.Areas.Public.Models
{
    public class SitemapEntryItem
    {
        public string Url { get; set; } = string.Empty;
        public DateTime? LastModifiedUtc { get; set; }
        public string? ChangeFrequency { get; set; }
        public decimal? Priority { get; set; }
    }
}
