using Microsoft.EntityFrameworkCore;
using Squash.DataAccess.Entities;

namespace Squash.DataAccess
{
    public interface IDataContext : IDisposable
    {
        DbSet<Tournament> Tournaments { get; set; }
        DbSet<TournamentDay> TournamentDays { get; set; }
        DbSet<Draw> Draws { get; set; }
        DbSet<Round> Rounds { get; set; }
        DbSet<Court> Courts { get; set; }
        DbSet<Nationality> Nationalities { get; set; }
        DbSet<Player> Players { get; set; }
        DbSet<User> Users { get; set; }
        DbSet<Match> Matches { get; set; }
        DbSet<MatchGame> MatchGames { get; set; }
        DbSet<MatchGameEventLog> MatchGameEventLogs { get; set; }
        DbSet<GameLog> GameLogs { get; set; }
        DbSet<ReferredMatch> ReferredMatches { get; set; }

        int SaveChanges();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
