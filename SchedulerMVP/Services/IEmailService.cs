namespace SchedulerMVP.Services;

public interface IEmailService
{
    Task SendInvitationEmailAsync(string email, string confirmationToken, string baseUrl);
}
