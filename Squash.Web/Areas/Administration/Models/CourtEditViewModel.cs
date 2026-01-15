namespace Squash.Web.Areas.Administration.Models
{
    public class CourtEditViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
    }
}
