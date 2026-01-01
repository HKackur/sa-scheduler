using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SchedulerMVP.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendInvitationEmailAsync(string email, string confirmationToken, string baseUrl)
    {
        var confirmationLink = $"{baseUrl.TrimEnd('/')}/confirm-email?token={Uri.EscapeDataString(confirmationToken)}&email={Uri.EscapeDataString(email)}";
        
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(GetFromName(), GetFromEmail()));
        message.To.Add(new MailboxAddress("", email));
        message.Subject = "Välkommen till Sportadmins Schemaläggning";

        var bodyBuilder = new BodyBuilder
        {
            TextBody = $@"Hej!

Du har blivit inbjuden att använda Sportadmins Schemaläggning.

Klicka på länken nedan för att slutföra din registrering och skapa ditt lösenord:
{confirmationLink}

Länken är giltig i 7 dagar.

Vid frågor, kontakta produktägare henrik.kackur@sportadmin.se

Med vänliga hälsningar,
SportAdmin Team",
            HtmlBody = $@"<html>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <h2 style=""color: #1761A5;"">Välkommen till Sportadmins Schemaläggning</h2>
    <p>Hej!</p>
    <p>Du har blivit inbjuden att använda Sportadmins Schemaläggning.</p>
    <p>Klicka på knappen nedan för att slutföra din registrering och skapa ditt lösenord:</p>
    <p style=""margin: 24px 0;"">
        <a href=""{confirmationLink}"" style=""background-color: #1761A5; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block; font-weight: 600;"">Slutför registrering</a>
    </p>
    <p style=""font-size: 12px; color: #6b7280;"">Eller kopiera denna länk till din webbläsare:<br/>{confirmationLink}</p>
    <p style=""font-size: 12px; color: #6b7280; margin-top: 24px;"">Länken är giltig i 7 dagar.</p>
    <p style=""margin-top: 24px;"">Vid frågor, kontakta produktägare <a href=""mailto:henrik.kackur@sportadmin.se"">henrik.kackur@sportadmin.se</a></p>
    <p style=""margin-top: 24px;"">Med vänliga hälsningar,<br/>SportAdmin Team</p>
</body>
</html>"
        };

        message.Body = bodyBuilder.ToMessageBody();

        await SendEmailAsync(message, email, "invitation");
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetToken, string baseUrl)
    {
        var resetLink = $"{baseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(email)}";
        
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(GetFromName(), GetFromEmail()));
        message.To.Add(new MailboxAddress("", email));
        message.Subject = "Återställ ditt lösenord - Sportadmins Schemaläggning";

        var bodyBuilder = new BodyBuilder
        {
            TextBody = $@"Hej!

Du har begärt att återställa ditt lösenord för Sportadmins Schemaläggning.

Klicka på länken nedan för att återställa ditt lösenord:
{resetLink}

Länken är giltig i 1 timme.

Om du inte har begärt detta, kan du ignorera detta meddelande.

Vid frågor, kontakta produktägare henrik.kackur@sportadmin.se

Med vänliga hälsningar,
SportAdmin Team",
            HtmlBody = $@"<html>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <h2 style=""color: #1761A5;"">Återställ ditt lösenord</h2>
    <p>Hej!</p>
    <p>Du har begärt att återställa ditt lösenord för Sportadmins Schemaläggning.</p>
    <p>Klicka på knappen nedan för att återställa ditt lösenord:</p>
    <p style=""margin: 24px 0;"">
        <a href=""{resetLink}"" style=""background-color: #1761A5; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block; font-weight: 600;"">Återställ lösenord</a>
    </p>
    <p style=""font-size: 12px; color: #6b7280;"">Eller kopiera denna länk till din webbläsare:<br/>{resetLink}</p>
    <p style=""font-size: 12px; color: #6b7280; margin-top: 24px;"">Länken är giltig i 1 timme.</p>
    <p style=""margin-top: 24px; font-size: 14px; color: #6b7280;"">Om du inte har begärt detta, kan du ignorera detta meddelande.</p>
    <p style=""margin-top: 24px;"">Vid frågor, kontakta produktägare <a href=""mailto:henrik.kackur@sportadmin.se"">henrik.kackur@sportadmin.se</a></p>
    <p style=""margin-top: 24px;"">Med vänliga hälsningar,<br/>SportAdmin Team</p>
</body>
</html>"
        };

        message.Body = bodyBuilder.ToMessageBody();

        await SendEmailAsync(message, email, "password reset");
    }

    private string GetFromEmail()
    {
        return _configuration["Email:FromEmail"] ?? _configuration["Email__FromEmail"] ?? Environment.GetEnvironmentVariable("EMAIL_FROM_EMAIL") ?? "noreply@sportadmin.se";
    }

    private string GetFromName()
    {
        return _configuration["Email:FromName"] ?? _configuration["Email__FromName"] ?? Environment.GetEnvironmentVariable("EMAIL_FROM_NAME") ?? "Sportadmins Schemaläggning";
    }

    private async Task SendEmailAsync(MimeMessage message, string email, string emailType)
    {
        // Support both colon (:) and double underscore (__) for nested configuration
        // Azure App Settings uses double underscore, local config can use colon
        var smtpHost = _configuration["Email:SmtpHost"] ?? _configuration["Email__SmtpHost"] ?? Environment.GetEnvironmentVariable("EMAIL_SMTP_HOST");
        var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? _configuration["Email__SmtpPort"] ?? Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT") ?? "587");
        var smtpUser = _configuration["Email:SmtpUser"] ?? _configuration["Email__SmtpUser"] ?? Environment.GetEnvironmentVariable("EMAIL_SMTP_USER");
        var smtpPassword = _configuration["Email:SmtpPassword"] ?? _configuration["Email__SmtpPassword"] ?? Environment.GetEnvironmentVariable("EMAIL_SMTP_PASSWORD");

        // Debug logging to understand configuration reading
        _logger.LogInformation("SMTP Config check for {EmailType}: Host={Host}, User={User}, PasswordSet={HasPassword}", 
            emailType, 
            smtpHost ?? "NULL", 
            smtpUser ?? "NULL", 
            !string.IsNullOrEmpty(smtpPassword));
        
        // Additional debug: Check what configuration values are actually available
        _logger.LogInformation("Config check - Email:SmtpHost={ColonHost}, Email__SmtpHost={UnderscoreHost}", 
            _configuration["Email:SmtpHost"] ?? "NULL",
            _configuration["Email__SmtpHost"] ?? "NULL");

        if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
        {
            _logger.LogWarning("SMTP not configured. {EmailType} email not sent to {Email}. Host={Host}, User={User}, PasswordSet={HasPassword}", 
                emailType, email, smtpHost ?? "NULL", smtpUser ?? "NULL", !string.IsNullOrEmpty(smtpPassword));
            return;
        }

        try
        {
            using var client = new SmtpClient();
            // Accept all certificates for Brevo (they use valid certificates but validation might fail in some environments)
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            client.Timeout = 30000; // 30 second timeout to prevent hanging
            
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("{EmailType} email sent successfully to {Email}", emailType, email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending {EmailType} email to {Email}", emailType, email);
            throw;
        }
    }
}
