using Squash.Shared.Parsers.Esf;

namespace Squash.Processors
{
    public partial class Program
    {
        public static void DownloadEsfDay()
        {
            var days = new string[]{
            "https://esf.tournamentsoftware.com/tournament/438dcb7f-f5b3-4cae-a0f3-7d21e4a08f06/matches/20251024",
            "https://esf.tournamentsoftware.com/tournament/438dcb7f-f5b3-4cae-a0f3-7d21e4a08f06/matches/20251025",
            "https://esf.tournamentsoftware.com/tournament/438dcb7f-f5b3-4cae-a0f3-7d21e4a08f06/matches/20251026" ,
            "https://esf.tournamentsoftware.com/tournament/e29aff47-d6af-4fd6-a5ae-4d2e918b397a/matches/20250102",
            "https://esf.tournamentsoftware.com/tournament/e29aff47-d6af-4fd6-a5ae-4d2e918b397a/matches/20250103",
            "https://esf.tournamentsoftware.com/tournament/e29aff47-d6af-4fd6-a5ae-4d2e918b397a/matches/20250104",
            "https://esf.tournamentsoftware.com/tournament/e29aff47-d6af-4fd6-a5ae-4d2e918b397a/matches/20250105",
            "https://esf.tournamentsoftware.com/tournament/e29aff47-d6af-4fd6-a5ae-4d2e918b397a/matches/20250106",
            };

            Download.DownloadParseAndStoreMatches(days);
        }
    }
}
