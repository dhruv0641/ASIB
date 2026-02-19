using ASIB.Core.Interfaces;
using ASIB.Models;
using ASIB.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace ASIB.Controllers;

public class UserProfileController : Controller
{
    private readonly AsibContext _context;
    private readonly IUploadStorageService _uploadStorage;

    public UserProfileController(AsibContext context, IUploadStorageService uploadStorage)
    {
        _context = context;
        _uploadStorage = uploadStorage;
    }

    [HttpGet("/Dashboard/UserProfile")]
    [HttpGet("user_profile")]
    
    public async Task<IActionResult> Index()
    {
        if (!TryGetUserId(out var userId))
            return Redirect("/Auth/Login");

        var model = await BuildModelAsync(userId);
        return View("Index", model);
    }

    [HttpPost("/Dashboard/UserProfile")]
    [HttpPost("user_profile")]
    
    public async Task<IActionResult> IndexPost()
    {
        if (!TryGetUserId(out var userId))
            return Redirect("/Auth/Login");

        var formType = (Request.Form["form_type"].ToString() ?? "").Trim();
        var flash = "";
        var flashPersonal = "";

        if (formType == "profile")
        {
            var headline = Request.Form["headline"].ToString();
            var bio = Request.Form["bio"].ToString();
            var skills = Request.Form["skills"].ToString();
            var experience = Request.Form["experience"].ToString();

            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
            var photoUrl = profile?.ProfilePhotoUrl ?? "";

            var photoFile = Request.Form.Files.FirstOrDefault(f => f.Name == "profile_photo");
            if (photoFile != null && photoFile.Length > 0)
            {
                var save = await _uploadStorage.SaveProfilePictureAsync(userId, photoFile);
                if (!save.Success)
                {
                    flash = save.Error ?? "Invalid file type. Please upload JPG, PNG, or GIF.";
                }
                else if (!string.IsNullOrEmpty(save.RelativePath))
                {
                    photoUrl = save.RelativePath;
                }
            }

            if (string.IsNullOrEmpty(flash))
            {
                if (profile == null)
                {
                    profile = new Profile
                    {
                        UserId = userId
                    };
                    _context.Profiles.Add(profile);
                }

                profile.Headline = headline;
                profile.Bio = bio;
                profile.Skills = skills;
                profile.Experience = experience;
                profile.ProfilePhotoUrl = photoUrl;

                await _context.SaveChangesAsync();
                return Redirect("/Dashboard/UserProfile");
            }
        }
        else if (formType == "personal_details")
        {
            var newEmail = (Request.Form["email"].ToString() ?? "").Trim();
            var newContact = (Request.Form["contact_number"].ToString() ?? "").Trim();

            if (!string.IsNullOrEmpty(newEmail)
                && !string.IsNullOrEmpty(newContact)
                && Regex.IsMatch(newEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                var emailExists = await _context.Users.AnyAsync(u => u.Email == newEmail && u.UserId != userId);
                if (emailExists)
                {
                    flashPersonal = "Error: That email address is already in use by another account.";
                }
                else if (!long.TryParse(newContact, out var contactNumber))
                {
                    flashPersonal = "Error: Please provide a valid email and contact number.";
                }
                else
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                    if (user != null)
                    {
                        user.Email = newEmail;
                        user.ContactNumber = contactNumber;
                        await _context.SaveChangesAsync();
                        return Redirect("/Dashboard/UserProfile");
                    }
                }
            }
            else
            {
                flashPersonal = "Error: Please provide a valid email and contact number.";
            }
        }

        var model = await BuildModelAsync(userId);
        model.FlashMessage = flash;
        model.FlashMessagePersonal = flashPersonal;
        return View("Index", model);
    }

    private bool TryGetUserId(out long userId)
    {
        var userIdStr = HttpContext.Session.GetString("user_id");
        return long.TryParse(userIdStr, out userId) && userId > 0;
    }

    private async Task<UserProfileViewModel> BuildModelAsync(long userId)
    {
        var userData = await _context.Users
            .Where(u => u.UserId == userId)
            .GroupJoin(_context.Roles, u => u.RoleId, r => r.RoleId, (u, r) => new { u, r })
            .SelectMany(x => x.r.DefaultIfEmpty(), (x, r) => new
            {
                x.u.FirstName,
                x.u.LastName,
                x.u.Email,
                x.u.ContactNumber,
                Role = r != null ? r.Role1 : ""
            })
            .FirstOrDefaultAsync();

        var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);

        var firstName = userData?.FirstName ?? "";
        var profilePhoto = !string.IsNullOrWhiteSpace(profile?.ProfilePhotoUrl)
            ? profile!.ProfilePhotoUrl!
            : $"https://placehold.co/150x150/6366f1/white?text={GetInitial(firstName)}";

        return new UserProfileViewModel
        {
            UserId = userId,
            FirstName = firstName,
            LastName = userData?.LastName ?? "",
            Email = userData?.Email ?? "",
            ContactNumber = userData?.ContactNumber.ToString() ?? "",
            Role = userData?.Role ?? "",
            ProfilePhoto = profilePhoto,
            Headline = profile?.Headline ?? "",
            Bio = profile?.Bio ?? "",
            Skills = profile?.Skills ?? "",
            Experience = profile?.Experience ?? ""
        };
    }

    private static string GetInitial(string firstName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            return "U";
        return firstName.Trim().Substring(0, 1).ToUpperInvariant();
    }
}

