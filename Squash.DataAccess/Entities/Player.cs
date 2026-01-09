namespace Squash.DataAccess.Entities
{
    public class Player : Squash.DataAccess.Entities.EntityBase
    {
        public int? ExternalPlayerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? MemberId { get; set; }

        public int? NationalityId { get; set; }
        public Nationality? Nationality { get; set; }
    }
}
