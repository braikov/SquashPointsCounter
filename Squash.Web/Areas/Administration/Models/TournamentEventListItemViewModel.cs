using Squash.DataAccess.Entities;

namespace Squash.Web.Areas.Administration.Models
{
    public class TournamentEventListItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DataAccess.Entities.MatchType MatchType { get; set; }
        public int Age { get; set; }
        public Direction Direction { get; set; }
    }
}
