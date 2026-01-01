namespace SchedulerMVP.Services;

public interface IEmailService
{
    Task SendInvitationEmailAsync(string email, string confirmationToken, string baseUrl);
    Task SendPasswordResetEmailAsync(string email, string resetToken, string baseUrl);
}
