using Azure;
using Azure.Communication.Email;
using Presentation.Interfaces;
using Presentation.Models;
using Presentation.Data.Contexts;
using Presentation.Data.Entities;


namespace Presentation.Services;

//ChatGPT hjälpte mig att förbättra och skriva koden.
public class VerificationService : IVerificationService
{
    private readonly IConfiguration _configuration;
    private readonly EmailClient _emailClient;
    private readonly DataContext _context;
    private static readonly Random _random = new();
    private readonly HttpClient _httpClient;

    public VerificationService(IConfiguration configuration, EmailClient emailClient, DataContext context, HttpClient httpClient)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _emailClient = emailClient ?? throw new ArgumentNullException(nameof(emailClient));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        var accountServiceUrl = configuration["AccountService:Url"];
        if (string.IsNullOrEmpty(accountServiceUrl))
        {
            throw new InvalidOperationException("AccountService:Url is not configured.");
        }

        _httpClient.BaseAddress = new Uri(accountServiceUrl);
    }

    public async Task<VerificationServiceResult> SendVerificationCodeAsync(SendVerificationCodeRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
                return new VerificationServiceResult { Succeeded = false, Error = "Invalid request." };

            var email = request.Email.ToLowerInvariant();

            var response = await _httpClient.GetAsync($"/api/accounts/check-email?email={email}");
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new VerificationServiceResult { Succeeded = false, Error = errorContent };
            }

            var emailCheckResult = await response.Content.ReadFromJsonAsync<CheckEmailExistsResponse>();
            if (emailCheckResult == null || emailCheckResult.Exists)
                return new VerificationServiceResult { Succeeded = false, Error = emailCheckResult?.Message ?? "Email already exists." };

            var verificationCode = _random.Next(100000, 999999).ToString();
            var expirationTime = DateTime.UtcNow.AddMinutes(5);

            await InvalidateExistingCodesAsync(email);

            var verificationEntity = new VerificationCodeEntity
            {
                Email = email,
                Code = verificationCode,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expirationTime
            };

            _context.VerificationCodes.Add(verificationEntity);
            await _context.SaveChangesAsync();

            var subject = $"Your verification code is {verificationCode}";
            var plainTextContent = $"Your verification code is {verificationCode}";
            var htmlContent = HtmlContent(verificationCode, email);

            var emailMessage = new EmailMessage(
                senderAddress: _configuration["AzureCommunicationService:SenderAddress"],
                recipients: new EmailRecipients([new EmailAddress(email)]),
                content: new EmailContent(subject)
                {
                    PlainText = plainTextContent,
                    Html = htmlContent
                }
            );

            await _emailClient.SendAsync(WaitUntil.Started, emailMessage);

            return new VerificationServiceResult { Succeeded = true, Message = "Verification code sent successfully." };
        }
        catch (Exception ex)
        {
            return new VerificationServiceResult { Succeeded = false, Error = $"Failed to send verification code: {ex.Message}" };
        }
    }

    public VerificationServiceResult VerifyVerificationCode(VerifyVerificationCodeRequest request)
    {
        try
        {
            var email = request.Email.ToLowerInvariant();

            var verificationCode = _context.VerificationCodes
                .FirstOrDefault(v => v.Email == email && v.Code == request.Code);

            if (verificationCode == null || verificationCode.ExpiresAt < DateTime.UtcNow)
            {
                return new VerificationServiceResult { Succeeded = false, Error = "Invalid or expired verification code." };
            }

            _context.VerificationCodes.Remove(verificationCode);
            _context.SaveChanges();

            return new VerificationServiceResult { Succeeded = true, Message = "Verification successful." };
        }
        catch (Exception ex)
        {
            return new VerificationServiceResult { Succeeded = false, Error = $"Verification failed: {ex.Message}" };
        }
    }

    private async Task InvalidateExistingCodesAsync(string email)
    {
        var existingCodes = _context.VerificationCodes
            .Where(v => v.Email == email && v.ExpiresAt > DateTime.UtcNow)
            .ToList();

        if (existingCodes.Count != 0)
        {
            _context.VerificationCodes.RemoveRange(existingCodes);
            await _context.SaveChangesAsync();
        }
    }

    private static string HtmlContent(string code, string email)
    {
        return $@"
        <!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='UTF-8'>
            <title>Your verification code</title>
        </head>
        <body style='font-family: Arial, sans-serif; background-color:#F9F9F9; padding:20px;'>
            <div style='background-color:#FFFFFF; border-radius:8px; padding:20px; max-width:600px; margin:auto;'>
                <h2 style='color:#333;'>Verify Your Email Address</h2>
                <p>Hello,</p>
                <p>To complete your verification, please use the following code:</p>
                <div style='font-size:24px; color:#333; font-weight:bold; background-color:#EFEFEF; padding:10px; border-radius:4px; text-align:center;'>{code}</div>
                <p>Or click the button below to verify your email:</p>
                <a href='https://domain.com/verify?email={email}&token={code}' style='display:inline-block; background-color:#007BFF; color:#FFFFFF; padding:12px 24px; border-radius:4px; text-decoration:none; font-weight:bold;'>Verify Email</a>
                <p>If you did not initiate this request, please ignore this email.</p>
                <p>Thank you,</p>
                <p>Your Company Team</p>
            </div>
        </body>
        </html>";
    }
}
