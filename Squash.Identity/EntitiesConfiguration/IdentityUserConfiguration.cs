using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Squash.Identity.EntitiesConfiguration
{
    public class IdentityUserConfiguration : IEntityTypeConfiguration<IdentityUser>
    {
        public void Configure(EntityTypeBuilder<IdentityUser> builder)
        {
            // Seed user: miro@ima.bg / Password123!
            var miroId = "ae0b7d5a-8264-42f2-8c10-53472f8820c7";

            builder.HasData(new IdentityUser
            {
                Id = miroId,
                UserName = "miro@ima.bg",
                NormalizedUserName = "MIRO@IMA.BG",
                Email = "miro@ima.bg",
                NormalizedEmail = "MIRO@IMA.BG",
                EmailConfirmed = true,
                PasswordHash = "AQAAAAIAAYagAAAAENyErHdueV460gFcQ08NgDmVnQoAxT5ZX84MVPBVNTPkfQBc1RcVFYU8nOcWWDczgw==", // Password123!
                SecurityStamp = "550E8400-E29B-41D4-A716-446655440000",
                ConcurrencyStamp = "67905d45-5623-4537-814e-abc98234de12",
                PhoneNumberConfirmed = false,
                LockoutEnabled = true,
                AccessFailedCount = 0
            });

            // Seed admin user: miroslav.braikov@gmail.com / !@#qweASD1
            var adminUserId = "b1802c6b-6b27-466d-932b-319520866380";

            builder.HasData(new IdentityUser
            {
                Id = adminUserId,
                UserName = "miroslav.braikov@gmail.com",
                NormalizedUserName = "MIROSLAV.BRAIKOV@GMAIL.COM",
                Email = "miroslav.braikov@gmail.com",
                NormalizedEmail = "MIROSLAV.BRAIKOV@GMAIL.COM",
                EmailConfirmed = true,
                PasswordHash = "AQAAAAIAAYagAAAAELZTRZjB2vpyrauOI9VEz124fOQUchLqZJ/RNdc+b4S84pqYA6V1epsM4+q07SQ9ww==", 
                SecurityStamp = "B23E4567-E89B-12D3-A456-426614174000",
                ConcurrencyStamp = "a1b2c3d4-e5f6-47a8-b9c0-d1e2f3a4b5c6",
                PhoneNumberConfirmed = false,
                LockoutEnabled = true,
                AccessFailedCount = 0
            });

            // Seed user: boris.braikov@gmail.com
            var borisId = "c340d87e-9f37-4d7c-8e21-65483f9931d8";

            builder.HasData(new IdentityUser
            {
                Id = borisId,
                UserName = "boris.braikov@gmail.com",
                NormalizedUserName = "BORIS.BRAIKOV@GMAIL.COM",
                Email = "boris.braikov@gmail.com",
                NormalizedEmail = "BORIS.BRAIKOV@GMAIL.COM",
                EmailConfirmed = true,
                PasswordHash = "AQAAAAIAAYagAAAAEKAOlFahSZHqnOh08wRNaY2cSufSUzUqc+8a77Tqloj6q6XBk0bTH/QboopjDfv4hA==", 
                SecurityStamp = "C34F5678-F90C-23E4-B567-537725285000",
                ConcurrencyStamp = "b2c3d4e5-f6a7-58b9-c0d1-e2f3a4b5c6d7",
                PhoneNumberConfirmed = false,
                LockoutEnabled = true,
                AccessFailedCount = 0
            });
        }
    }
}
