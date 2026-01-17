using Squash.Identity.Services.Email;

namespace Squash.Web.Services.Email
{
    public class NullEmailSender : IEmailSender
    {
        public Task SendRegistrationVerificationAsync(string email, string verificationUrl, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(string email, string resetUrl, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
