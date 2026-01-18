using Squash.Identity.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Squash.Identity
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<ShortCodeToToken> ShortCodeToTokens { get; set; } = null!;

        public DbSet<LoginShortCode> LoginShortCodes { get; set; } = null!;

        public DbSet<AccountEvent> AccountEvents { get; set; } = null!;

        public override int SaveChanges()
        {
            foreach (var entry in base.ChangeTracker.Entries<EntityBase>()
                .Where(q => q.State == EntityState.Added || q.State == EntityState.Modified))
            {
                entry.Entity.DateUpdated = DateTime.UtcNow;
                //#warning setting user is not implemented
                //entry.Entity.ModifiedBy = _userService.UserId;
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.DateCreated = DateTime.UtcNow;
                    //entry.Entity.CreatedBy = _userService.UserId;
                }
            }

            return base.SaveChanges();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ShortCodeToToken>()
                .HasIndex(x => new { x.Email, x.Code });

            modelBuilder.Entity<LoginShortCode>()
                .HasIndex(x => new { x.Email, x.Code });

            modelBuilder.Entity<AccountEvent>()
                .HasIndex(x => new { x.Email, x.EventType, x.DateCreated });

            modelBuilder.Entity<AccountEvent>()
                .HasIndex(x => new { x.UserId, x.EventType, x.DateCreated });

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
