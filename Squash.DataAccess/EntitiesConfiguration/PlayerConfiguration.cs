using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Squash.DataAccess.Entities;

namespace Squash.DataAccess.EntitiesConfiguration
{
    public class PlayerConfiguration : IEntityTypeConfiguration<Player>
    {
        public void Configure(EntityTypeBuilder<Player> builder)
        {
            // Seed Player: Boris Braykov
            builder.HasData(new Player
            {
                Id = 1,
                UserId = 4, // Will be linked to User Id 4
                Name = "Boris Braykov",
                EntitySourceId = EntitySource.Native,
                CountryId = 28, // BUL
                EsfMemberId = "ES793398940",
                DateCreated = new DateTime(2025, 1, 1),
                DateUpdated = new DateTime(2025, 1, 1),
                LastOperationUserId = 0, 
                PictureUrl = "/uploads/avatars/player-1-d324714cd7844b81b92dcc694464ccbb.jpg"
            });
        }
    }
}
