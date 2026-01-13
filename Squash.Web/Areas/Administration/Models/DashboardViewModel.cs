using System.Collections.Generic;

namespace Squash.Web.Areas.Administration.Models
{
    public class DashboardViewModel
    {
        public long TotalUsers { get; set; }
        public long TotalTournaments { get; set; }
        public long NewUsersLast30Days { get; set; }
        public long NewTournamentsLast30Days { get; set; }
        public Dictionary<string, long> UserSignupsByDay { get; set; } = new();
        public Dictionary<string, long> TournamentCreationsByDay { get; set; } = new();
    }
}
