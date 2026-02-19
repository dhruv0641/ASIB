using ASIB.Core.Interfaces;
using ASIB.Models;
using ASIB.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ASIB.Core.Services;

public class AuthService : IAuthService
{
    private readonly AsibContext _context;

    public AuthService(AsibContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Validates admin credentials
    /// </summary>
    public async Task<LoginResponse> ValidateAdminAsync(string email, string password)
    {
        var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Email == email);

        if (admin == null)
        {
            return new LoginResponse
            {
                Success = false,
                Message = "❌ No account found with this email.",
                ErrorType = "error"
            };
        }

        if (!BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash))
        {
            return new LoginResponse
            {
                Success = false,
                Message = "❌ Invalid admin password.",
                ErrorType = "error"
            };
        }

        return new LoginResponse
        {
            Success = true,
            Message = "Admin login successful",
            ErrorType = "success",
            RedirectUrl = "/admin/dashboard"
        };
    }

    /// <summary>
    /// Validates user credentials with verification status checks
    /// </summary>
    public async Task<LoginResponse> ValidateUserAsync(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            return new LoginResponse
            {
                Success = false,
                Message = "❌ No account found with this email.",
                ErrorType = "error"
            };
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return new LoginResponse
            {
                Success = false,
                Message = "❌ Invalid email or password.",
                ErrorType = "error"
            };
        }

        // Check verification status
        // Blocking admins stores 2, approved is true, pending is false (0), rejected is -1 (but stored as int in admin_actions)
        // VerificationStatus is int?, where 0 = pending, 1 = approved, 2 = blocked, -1 = rejected
        
        // For blocked status, we check admin_actions
        var isBlocked = await _context.AdminActions
            .AnyAsync(a => a.TargetUserId == user.UserId && a.ActionType == "block_user");

        if (isBlocked)
        {
            var blockReason = await GetBlockReasonAsync(user.UserId);
            return new LoginResponse
            {
                Success = false,
                Message = $"❌ Your account has been blocked by an administrator. <br><strong>Reason:</strong> {blockReason}",
                ErrorType = "error"
            };
        }

        if (user.VerificationStatus == 1) // 1 = Approved
        {
            // Update is_online and last_seen
            user.IsOnline = true;
            user.LastSeen = DateTime.Now;
            await _context.SaveChangesAsync();

            return new LoginResponse
            {
                Success = true,
                Message = "Login successful",
                ErrorType = "success",
                RedirectUrl = "/user/dashboard"
            };
        }

        if (user.VerificationStatus == 0 || user.VerificationStatus == null) // 0 = Pending
        {
            return new LoginResponse
            {
                Success = false,
                Message = "⚠️ Your account is pending admin approval.",
                ErrorType = "pending"
            };
        }

        return new LoginResponse
        {
            Success = false,
            Message = "❌ Invalid account status.",
            ErrorType = "error"
        };
    }

    /// <summary>
    /// Creates a new user with validation
    /// </summary>
    public async Task<RegisterResponse> CreateUserAsync(RegisterRequest request)
    {
        var errors = new Dictionary<string, string>();

        // Validation
        if (string.IsNullOrWhiteSpace(request.FirstName) || request.FirstName.Length > 100)
            errors["FirstName"] = request.FirstName == "" ? "First name is required." : "Max 100 characters.";

        if (!string.IsNullOrWhiteSpace(request.MiddleName) && request.MiddleName.Length > 100)
            errors["MiddleName"] = "Max 100 characters.";

        if (string.IsNullOrWhiteSpace(request.LastName) || request.LastName.Length > 100)
            errors["LastName"] = request.LastName == "" ? "Last name is required." : "Max 100 characters.";

        // Email validation
        if (string.IsNullOrWhiteSpace(request.Email) || !IsValidEmail(request.Email))
            errors["Email"] = "Enter valid email.";
        else if (await EmailExistsAsync(request.Email))
            errors["Email"] = "Email already registered.";

        if (string.IsNullOrWhiteSpace(request.Address))
            errors["Address"] = "Address is required.";

        if (string.IsNullOrWhiteSpace(request.ContactNumber) || !System.Text.RegularExpressions.Regex.IsMatch(request.ContactNumber, @"^\d{10}$"))
            errors["ContactNumber"] = "10 digits required.";

        // Role validation
        if (request.RoleRequested <= 0)
            errors["RoleRequested"] = "Select a valid role.";

        // Get role to determine batch/enrollment requirements
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == request.RoleRequested);
        var roleName = role?.Role1?.ToLower() ?? "";
        var needsBatch = roleName.Contains("student") || roleName.Contains("alumni");
        var isStudent = roleName.Contains("student");

        // Batch year validation
        if (needsBatch)
        {
            if (request.BatchYear == null || !System.Text.RegularExpressions.Regex.IsMatch(request.BatchYear.Value.ToString(), @"^\d{4}$"))
                errors["BatchYear"] = "Select valid batch year.";
        }
        else
        {
            request.BatchYear = null;
        }

        // Enrollment number validation (students only)
        if (isStudent)
        {
            if (string.IsNullOrWhiteSpace(request.EnrollmentNumber))
                errors["EnrollmentNumber"] = "Enrollment Number is required for students.";
            else if (await EnrollmentNumberExistsAsync(request.EnrollmentNumber))
                errors["EnrollmentNumber"] = "Enrollment Number already registered.";
        }
        else
        {
            request.EnrollmentNumber = null;
        }

        // Password validation
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            errors["Password"] = "Password min 6 chars.";

        if (string.IsNullOrWhiteSpace(request.PasswordConfirm) || request.Password != request.PasswordConfirm)
            errors["PasswordConfirm"] = "Passwords must match.";

        if (errors.Any())
        {
            return new RegisterResponse
            {
                Success = false,
                Message = "Validation failed",
                Errors = errors
            };
        }

        // Create user
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = new User
        {
            Email = request.Email,
            PasswordHash = passwordHash,
            FirstName = request.FirstName,
            MiddleName = request.MiddleName,
            LastName = request.LastName,
            Address = request.Address,
            ContactNumber = long.Parse(request.ContactNumber),
            RoleRequested = request.RoleRequested,
            BatchYear = request.BatchYear,
            EnrollmentNumber = request.EnrollmentNumber,
            VerificationStatus = 0, // 0 = Pending admin approval
            CreatedAt = DateTime.Now
        };

        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new RegisterResponse
            {
                Success = true,
                Message = "Registration successful! Your account is pending admin approval.",
                Errors = new()
            };
        }
        catch (Exception ex)
        {
            return new RegisterResponse
            {
                Success = false,
                Message = $"Database error: {ex.Message}",
                Errors = new() { { "__db", ex.Message } }
            };
        }
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task<bool> EnrollmentNumberExistsAsync(string enrollmentNumber)
    {
        return await _context.Users.AnyAsync(u => u.EnrollmentNumber == enrollmentNumber);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<Admin?> GetAdminByEmailAsync(string email)
    {
        return await _context.Admins.FirstOrDefaultAsync(a => a.Email == email);
    }

    public async Task<string?> GetRoleNameByUserIdAsync(long userId)
    {
        return await _context.Users
            .Where(u => u.UserId == userId)
            .Join(_context.Roles, u => u.RoleId, r => r.RoleId, (u, r) => r.Role1)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Role>> GetRolesAsync()
    {
        return await _context.Roles.OrderBy(r => r.Role1).ToListAsync();
    }

    public async Task LogoutUserAsync(long userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.IsOnline = false;
            user.LastSeen = DateTime.Now;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> IsPasswordChangeRequiredAsync(long userId)
    {
        var now = DateTime.Now;
        return await _context.PasswordResets.AnyAsync(p => p.UserId == userId && p.Used == true && p.ExpiresAt > now);
    }

    public async Task<bool> ChangePasswordAsync(long userId, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _context.SaveChangesAsync();

        var resets = await _context.PasswordResets.Where(p => p.UserId == userId && p.Used == true).ToListAsync();
        foreach (var r in resets)
            r.Used = false;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Fetches block reason from admin_actions table
    /// </summary>
    private async Task<string> GetBlockReasonAsync(long userId)
    {
        var blockAction = await _context.AdminActions
            .Where(a => a.TargetUserId == userId && a.ActionType == "block_user")
            .OrderByDescending(a => a.ActionTime)
            .FirstOrDefaultAsync();

        return !string.IsNullOrWhiteSpace(blockAction?.Reason) 
            ? blockAction.Reason 
            : "No reason provided.";
    }

    /// <summary>
    /// Simple email validation
    /// </summary>
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
