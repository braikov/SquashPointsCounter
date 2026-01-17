using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Squash.DataAccess.Entities
{
    [Index(nameof(IsEnabled))]
    [Index(nameof(Culture))]
    public class SitemapEntry : EntityBase
    {
        [MaxLength(2048)]
        public string Url { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? Culture { get; set; }

        [MaxLength(20)]
        public string? ChangeFrequency { get; set; }

        public decimal? Priority { get; set; }

        public bool IsEnabled { get; set; } = true;
    }
}
