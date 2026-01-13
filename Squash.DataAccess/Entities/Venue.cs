namespace Squash.DataAccess.Entities
{
    public class Venue : EntityBase
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double? Longitude { get; set; }
        public double? Latitude { get; set; }

        public ICollection<Court> Courts { get; set; } = new List<Court>();
        public ICollection<TournamentVenue> TournamentVenues { get; set; } = new List<TournamentVenue>();
    }
}
