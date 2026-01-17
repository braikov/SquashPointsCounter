namespace Squash.Identity.Services.Email
{
    public interface IEmailSender
    {
        Task SendRegistrationVerificationAsync(string email, string verificationUrl, CancellationToken cancellationToken);
        Task SendPasswordResetAsync(string email, string resetUrl, CancellationToken cancellationToken);
    }
}
