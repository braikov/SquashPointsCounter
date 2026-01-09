namespace Squash.DataAccess.Entities
{
    public class Tournament : Squash.DataAccess.Entities.EntityBase
    {
        public string ExternalCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? OrganizationCode { get; set; }

        public ICollection<TournamentDay> Days { get; set; } = new List<TournamentDay>();
        public ICollection<Draw> Draws { get; set; } = new List<Draw>();
        public ICollection<Match> Matches { get; set; } = new List<Match>();
        public ICollection<Court> Courts { get; set; } = new List<Court>();
    }
}