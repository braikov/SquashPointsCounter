namespace Squash.DataAccess.Entities
{
    public class Player : Squash.DataAccess.Entities.EntityBase
    {
        public int? UserId { get; set; }
        public User? User { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? PictureUrl { get; set; }
        public string? EsfMemberId { get; set; }
        public string? RankedinId { get; set; }
        public string? ImaId { get; set; }
        public EntitySource EntitySourceId { get; set; } = EntitySource.Native;

        public int? CountryId { get; set; }
        public Country? Country { get; set; }

        public ICollection<TournamentPlayer> TournamentPlayers { get; set; } = new List<TournamentPlayer>();
    }
}

