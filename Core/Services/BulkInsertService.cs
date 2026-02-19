using ASIB.Core.Interfaces;
using ASIB.Models;
using ASIB.Models.ViewModels;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace ASIB.Core.Services;

public class BulkInsertService : IBulkInsertService
{
    private readonly AsibContext _context;
    private readonly IConfiguration _configuration;

    public BulkInsertService(AsibContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<BulkInsertResult> ProcessBulkInsertAsync(long adminId, IFormFile file, string loginUrl, string roleName, int? batchYear)
    {
        var result = new BulkInsertResult();

        if (file == null || file.Length == 0)
        {
            result.Errors.Add("File upload failed.");
            return result;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".csv" && extension != ".xlsx" && extension != ".xls")
        {
            result.Errors.Add("Invalid file type. Only .csv, .xlsx, .xls allowed.");
            return result;
        }

        var allowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "student",
            "alumni",
            "faculty"
        };

        var normalizedRole = (roleName ?? "").Trim().ToLowerInvariant();
        if (!allowedRoles.Contains(normalizedRole))
        {
            result.Errors.Add("Invalid role selected.");
            return result;
        }

        var requiresBatchYear = normalizedRole == "student" || normalizedRole == "alumni";
        if (requiresBatchYear && (!batchYear.HasValue || batchYear.Value <= 0))
        {
            result.Errors.Add("Batch year is required for Student or Alumni.");
            return result;
        }

        var roleMap = await _context.Roles
            .Where(r => r.Role1 != null)
            .ToListAsync();

        var roleLookup = roleMap
            .Where(r => allowedRoles.Contains(r.Role1 ?? ""))
            .ToDictionary(r => r.Role1!.ToLowerInvariant(), r => r.RoleId);

        var rows = extension == ".csv"
            ? ReadCsv(file)
            : ReadExcel(file);

        foreach (var row in rows)
        {
            try
            {
                if (row.Count < 4)
                {
                    result.FailedCount++;
                    continue;
                }

                if (row.Count > 4 && row.Skip(4).Any(x => !string.IsNullOrWhiteSpace(x)))
                {
                    result.FailedCount++;
                    continue;
                }

                var firstName = row[0].Trim();
                var middleName = row[1].Trim();
                var lastName = row[2].Trim();
                var email = row[3].Trim();

                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(email))
                {
                    result.FailedCount++;
                    continue;
                }

                if (!roleLookup.TryGetValue(normalizedRole, out var roleId))
                {
                    result.FailedCount++;
                    await LogAdminActionAsync(adminId, "bulk_invalid_role", $"Invalid role for {email}: {normalizedRole}");
                    continue;
                }

                var exists = await _context.Users.AnyAsync(u => u.Email == email);
                if (exists)
                {
                    result.SkippedDuplicateCount++;
                    await LogAdminActionAsync(adminId, "bulk_skip_duplicate", $"Duplicate email skipped: {email}");
                    continue;
                }

                var tempPassword = GenerateRandomPassword(10);
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

                var user = new User
                {
                    FirstName = firstName,
                    MiddleName = middleName,
                    LastName = lastName,
                    Email = email,
                    PasswordHash = passwordHash,
                    RoleRequested = roleId,
                    RoleId = roleId,
                    VerificationStatus = 1,
                    ContactNumber = 0,
                    Address = "N/A",
                    BatchYear = requiresBatchYear ? batchYear : null,
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                await LogAdminActionAsync(adminId, "bulk_add_user", $"Bulk inserted: {email}");

                var reset = new PasswordReset
                {
                    UserId = user.UserId,
                    OtpHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
                    ExpiresAt = DateTime.Now.AddYears(1),
                    Used = true,
                    CreatedAt = DateTime.Now
                };
                _context.PasswordResets.Add(reset);
                await _context.SaveChangesAsync();

                try
                {
                    SendUserEmail(email, tempPassword, loginUrl);
                }
                catch (Exception ex)
                {
                    result.EmailFailedCount++;
                    await LogAdminActionAsync(adminId, "bulk_email_failed", $"Email failed for {email}: {ex.Message}");
                }

                result.CreatedCount++;
            }
            catch
            {
                result.FailedCount++;
            }
        }

        return result;
    }

    private static List<List<string>> ReadCsv(IFormFile file)
    {
        var rows = new List<List<string>>();
        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var parts = line.Split(',');
            rows.Add(parts.Select(p => p.Trim()).ToList());
        }
        return rows;
    }

    private static List<List<string>> ReadExcel(IFormFile file)
    {
        var rows = new List<List<string>>();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var stream = file.OpenReadStream();
        using var reader = ExcelReaderFactory.CreateReader(stream);
        do
        {
            while (reader.Read())
            {
                var row = new List<string>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row.Add(reader.GetValue(i)?.ToString() ?? "");
                }
                rows.Add(row);
            }
            break;
        } while (reader.NextResult());
        return rows;
    }

    private async Task LogAdminActionAsync(long adminId, string actionType, string description)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO admin_actions (admin_id, action_type, description, action_time) VALUES ({adminId}, {actionType}, {description}, NOW())"
        );
    }

    private void SendUserEmail(string toEmail, string tempPassword, string loginUrl)
    {
        var smtp = _configuration.GetSection("Smtp");
        var host = smtp["Host"] ?? "";
        var user = smtp["User"] ?? "";
        var pass = smtp["Pass"] ?? "";
        var from = smtp["From"] ?? "";
        var fromName = smtp["FromName"] ?? "";
        var port = int.TryParse(smtp["Port"], out var p) ? p : 587;

        var body = GetEmailBody(toEmail, tempPassword, loginUrl);
        var textBody = $"Hello,\n\nYour account has been provisioned.\n\nEmail: {toEmail}\nTemporary Password: {tempPassword}\nLogin URL: {loginUrl}\n\nYou are required to change your password after first login.";

        using var message = new MailMessage();
        message.From = new MailAddress(from, fromName);
        message.ReplyToList.Add(new MailAddress(from, fromName));
        message.To.Add(new MailAddress(toEmail));
        message.Subject = "Account Access Details";
        message.Body = body;
        message.IsBodyHtml = true;
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(textBody, Encoding.UTF8, "text/plain"));

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(user, pass),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };

        client.Send(message);
    }

    private static string GetEmailBody(string email, string tempPassword, string loginUrl)
    {
        return @"
    <div style=""font-family: Arial, sans-serif; font-size: 14px; color: #333; line-height: 1.5;"">
        <p>Hello,</p>
        <p>Your account has been provisioned. Please find your access credentials below.</p>
        <div style=""margin: 15px 0; padding: 15px; background-color: #f8f9fa; border-left: 3px solid #004182;"">
            <p style=""margin: 0 0 5px 0;""><strong>Email:</strong> " + WebUtility.HtmlEncode(email) + @"</p>
            <p style=""margin: 0 0 5px 0;""><strong>Temporary Password:</strong> " + WebUtility.HtmlEncode(tempPassword) + @"</p>
            <p style=""margin: 0;""><strong>Login URL:</strong> " + WebUtility.HtmlEncode(loginUrl) + @"</p>
        </div>
        <p>You are required to change your password after first login.</p>
        <p style=""color: #888; font-size: 12px; margin-top: 30px;"">
            ASIB Admin System<br>
            <i>Automated Notification</i>
        </p>
    </div>";
    }

    private static string GenerateRandomPassword(int length)
    {
        var bytes = new byte[(int)Math.Ceiling(length / 2.0)];
        RandomNumberGenerator.Fill(bytes);
        var hex = Convert.ToHexString(bytes).ToLowerInvariant();
        return hex.Substring(0, length);
    }
}
