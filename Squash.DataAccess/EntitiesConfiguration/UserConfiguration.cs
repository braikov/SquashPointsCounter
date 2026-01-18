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
                Gender = "Unknown",
                PreferredLanguage = "bg-BG",
                Zip = "0000",
                City = "System",
                Address = "System",
                Verified = true,
                EmailNotificationsEnabled = false,
                DateCreated = new DateTime(2000, 1, 1),
                DateUpdated = new DateTime(2000, 1, 1),
                LastOperationUserId = 0
            });

            // Seed user: miro@ima.bg
            builder.HasData(new User
            {
                Id = 2,
                IdentityUserId = "ae0b7d5a-8264-42f2-8c10-53472f8820c7", // Must match IdentityUser.Id
                Name = "Miro",
                FirstName = "Miro",
                LastName = "Ima",
                Email = "miro@ima.bg",
                Phone = "1234567890",
                BirthDate = new DateTime(1980, 1, 1),
                Gender = "Male",
                PreferredLanguage = "bg-BG",
                Zip = "1000",
                City = "Sofia",
                Address = "Test Street 1",
                Verified = true,
                EmailNotificationsEnabled = true,
                DateCreated = new DateTime(2025, 1, 1),
                DateUpdated = new DateTime(2025, 1, 1),
                LastOperationUserId = 0
            });

            // Seed admin user: miroslav.braikov@gmail.com
            builder.HasData(new User
            {
                Id = 3,
                IdentityUserId = "b1802c6b-6b27-466d-932b-319520866380",
                Name = "Miroslav Braikov",
                FirstName = "Miroslav",
                LastName = "Braikov",
                Email = "miroslav.braikov@gmail.com",
                Phone = "1234567890",
                BirthDate = new DateTime(1980, 1, 1),
                Gender = "Male",
                PreferredLanguage = "en-GB",
                Zip = "1000",
                City = "Sofia",
                Address = "Admin Address",
                Verified = true,
                EmailNotificationsEnabled = true,
                DateCreated = new DateTime(2025, 1, 1),
                DateUpdated = new DateTime(2025, 1, 1),
                LastOperationUserId = 0
            });

            // Seed user: boris.braikov@gmail.com
            builder.HasData(new User
            {
                Id = 4,
                IdentityUserId = "c340d87e-9f37-4d7c-8e21-65483f9931d8",
                Name = "Boris Braykov",
                FirstName = "Boris",
                LastName = "Braykov",
                Email = "boris.braikov@gmail.com",
                Phone = "+359885038308",
                BirthDate = new DateTime(2012, 12, 19),
                Gender = "Male",
                PreferredLanguage = "bg-BG",
                CountryId = 28, // BUL
                City = "Sofia", // Assumed from context
                Address = "Not Provided", // Placeholder
                Zip = "1000",
                Verified = true,
                EmailNotificationsEnabled = true,
                PlayerId = 1, // Linked to seeded Player
                DateCreated = new DateTime(2025, 1, 1),
                DateUpdated = new DateTime(2025, 1, 1),
                LastOperationUserId = 0
            });
        }
    }
}
