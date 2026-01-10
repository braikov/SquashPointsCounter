namespace Squash.DataAccess.Entities
{
    public enum MatchGameEvent
    {
        AServersFirst = 1,
        BServersFirst = 2,
        AServersFirstOnLeft = 3,
        BServersFirstOnLeft = 4,

        PointA = 5,
        PointB = 6,
        Let = 7,
        StrokeA = 8,
        StrokeB = 9,
        Undo = 10,        
        ARequestReview = 11,
        BRequestReview = 12,

        LockMatch = 13,
        InjuryTimeoutA = 14,
        InjuryTimeoutB = 15,
        EquipmentIssue = 16,
        ARetires = 17,
        BRetires = 18,

        WarningA = 19,
        WarningB = 20,
        ConductStrokeA = 21,
        ConductStrokeB = 22,
        EndGame = 23,
        EndMatch = 24

    }
}
