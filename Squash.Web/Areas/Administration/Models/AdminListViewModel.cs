namespace Squash.Web.Areas.Administration.Models
{
    public class AdminListViewModel
    {
        public required string Id { get; set; }
        public required string UserName { get; set; }
        public required string Email { get; set; }
        public required string Locked { get; set; }
    }
}
