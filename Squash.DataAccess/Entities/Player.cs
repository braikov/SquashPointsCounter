namespace Squash.DataAccess.Entities
{
    public class Player : Squash.DataAccess.Entities.EntityBase
    {
        public int? UserId { get; set; }
        public User? User { get; set; }

        public int? ExternalPlayerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? EsfMemberId { get; set; }
        public EntitySource EntitySourceId { get; set; } = EntitySource.Native;

        public int? NationalityId { get; set; }
        public Nationality? Nationality { get; set; }

        public ICollection<PlayerTournament> PlayerTournaments { get; set; } = new List<PlayerTournament>();
    }
}
