namespace Squash.Web.Areas.Administration.Models
{
    public class TournamentHeaderViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ActiveTab { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
    }
}
