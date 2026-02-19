using ASIB.Models;
using ASIB.Shared.DTOs;

namespace ASIB.Core.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> ValidateUserAsync(string email, string password);
    Task<LoginResponse> ValidateAdminAsync(string email, string password);
    Task<RegisterResponse> CreateUserAsync(RegisterRequest request);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> EnrollmentNumberExistsAsync(string enrollmentNumber);
    Task<User?> GetUserByEmailAsync(string email);
    Task<Admin?> GetAdminByEmailAsync(string email);
    Task<string?> GetRoleNameByUserIdAsync(long userId);
    Task<List<Role>> GetRolesAsync();
    Task LogoutUserAsync(long userId);
    Task<bool> IsPasswordChangeRequiredAsync(long userId);
    Task<bool> ChangePasswordAsync(long userId, string newPassword);
}
