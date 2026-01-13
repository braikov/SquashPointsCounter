using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Squash.DataAccess.Entities;

namespace Squash.DataAccess.EntitiesConfiguration
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasIndex(u => u.IdentityUserId).IsUnique();
            builder.HasIndex(u => u.Email).IsUnique();

            builder.HasData(new User
            {
                Id = 1,
                IdentityUserId = "SYSTEM",
                Name = "System",
                Email = "system@squash.local",
                Phone = "N/A",
                BirthDate = new DateTime(2000, 1, 1),
                Zip = "0000",
                City = "System",
                Address = "System",
                Verified = true,
                EmailNotificationsEnabled = false,
                DateCreated = new DateTime(2000, 1, 1),
                DateUpdated = new DateTime(2000, 1, 1),
                LastOperationUserId = 0
            });
        }
    }
}
