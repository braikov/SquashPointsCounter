namespace Squash.Shared.Utils
{
    public static class ImaIdGenerator
    {
        public static string GenerateForPlayerId(int playerId)
        {
            if (playerId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playerId));
            }

            return $"IMA{playerId:D8}";
        }
    }
}
