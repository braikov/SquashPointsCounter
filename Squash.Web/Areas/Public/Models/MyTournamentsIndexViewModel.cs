using System.Collections.Generic;

namespace Squash.Web.Areas.Public.Models
{
    public class MyTournamentsIndexViewModel : PageViewModel
    {
        public string Culture { get; set; } = "en";
        public List<MyTournamentListItemViewModel> Items { get; set; } = new();
    }
}
