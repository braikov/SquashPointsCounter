namespace Squash.Identity.Entities
{
    public static class AccountEventType
    {
        public const string RegistrationRequested = "RegistrationRequested";
        public const string EmailConfirmationSent = "EmailConfirmationSent";
        public const string EmailConfirmed = "EmailConfirmed";
        public const string PasswordResetRequested = "PasswordResetRequested";
        public const string PasswordResetSent = "PasswordResetSent";
        public const string PasswordResetCompleted = "PasswordResetCompleted";
        public const string LoginSucceeded = "LoginSucceeded";
        public const string LoginFailed = "LoginFailed";
        public const string LoginBlockedUnconfirmed = "LoginBlockedUnconfirmed";
        public const string ResendConfirmationRequested = "ResendConfirmationRequested";
    }
}
