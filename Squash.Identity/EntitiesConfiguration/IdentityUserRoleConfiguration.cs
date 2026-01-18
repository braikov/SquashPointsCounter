using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Squash.Identity.EntitiesConfiguration
{
    public class IdentityUserRoleConfiguration : IEntityTypeConfiguration<IdentityUserRole<string>>
    {
        public void Configure(EntityTypeBuilder<IdentityUserRole<string>> builder)
        {
            var adminRoleId = "80959087-0131-4131-995f-3d1203597890";
            var adminUserId = "b1802c6b-6b27-466d-932b-319520866380";

            // Seed UserRole for Admin
            builder.HasData(new IdentityUserRole<string>
            {
                RoleId = adminRoleId,
                UserId = adminUserId
            });
        }
    }
}
