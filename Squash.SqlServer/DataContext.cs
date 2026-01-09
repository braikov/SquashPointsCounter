using Microsoft.EntityFrameworkCore;
using Squash.DataAccess;
using Squash.DataAccess.Entities;

namespace Squash.SqlServer
{
    public class DataContext(DbContextOptions<DataContext> options) : DbContext(options), IDataContext
    {
        public DbSet<Tournament> Tournaments { get; set; }
        public DbSet<TournamentDay> TournamentDays { get; set; }
        public DbSet<Draw> Draws { get; set; }
        public DbSet<Round> Rounds { get; set; }
        public DbSet<Court> Courts { get; set; }
        public DbSet<Nationality> Nationalities { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<MatchGame> MatchGames { get; set; }
        public DbSet<GameLog> GameLogs { get; set; }
        public DbSet<ReferredMatch> ReferredMatches { get; set; }

#if DEBUG
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors();
#endif

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
