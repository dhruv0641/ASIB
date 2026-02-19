# Authentication System - Quick Reference

## Routes

### Login
```
GET  /auth/login   - Display login form
GET  /login        - Display login form (alias)
POST /auth/login   - Process login
POST /login        - Process login (alias)
```

**Redirects:**
- Success (Admin): Admin Dashboard
- Success (User): User Dashboard  
- Pending: Show "Account pending approval"
- Blocked: Show block reason
- Already Logged In: Home page

### Registration
```
GET  /auth/register         - Display registration form
GET  /register              - Display registration form (alias)
POST /auth/register         - Process registration
POST /register              - Process registration (alias)
GET  /auth/register-success - Show success page
GET  /register-success      - Show success page (alias)
```

### Logout
```
GET  /auth/logout - Logout user
GET  /logout      - Logout user (alias)
POST /auth/logout - Logout user
POST /logout      - Logout user (alias)
```

### Forgot Password
```
GET /auth/forgot-password  - Display forgot password form
GET /forgot-password       - Display forgot password form (alias)
```

---

## Session Keys

### After Successful User Login
```csharp
HttpContext.Session.GetString("user_id")    // e.g., "123"
HttpContext.Session.GetString("user_email") // e.g., "user@example.com"
```

### After Successful Admin Login
```csharp
HttpContext.Session.GetString("admin_id")    // e.g., "1"
HttpContext.Session.GetString("admin_email") // e.g., "admin@example.com"
```

### Check if User Logged In
```csharp
if (HttpContext.Session.GetString("user_id") != null) {
    // User is logged in
}
```

---

## Form Bindings

### Login Form
```html
<input type="email" name="Email" required>
<input type="password" name="Password" required>
<input type="checkbox" name="RememberMe">
```

Maps to: `LoginRequest` model

### Registration Form
```html
<input type="text" name="FirstName" required>
<input type="text" name="MiddleName">
<input type="text" name="LastName" required>
<input type="email" name="Email" required>
<input type="tel" name="ContactNumber" required>
<textarea name="Address" required></textarea>
<select name="RoleRequested" required>
<input type="text" name="BatchYear" id="batchWrap">
<input type="text" name="EnrollmentNumber" id="enrollWrap">
<input type="password" name="Password" required>
<input type="password" name="PasswordConfirm" required>
```

Maps to: `RegisterRequest` model

---

## AuthService Methods

### Validate Login (User)
```csharp
var response = await authService.ValidateUserAsync(email, password);
if (response.Success) {
    // Login successful
    // response.RedirectUrl = "/user/dashboard"
} else {
    // response.Message contains error
    // response.ErrorType = "error"|"pending"|"blocked"
}
```

### Validate Login (Admin)
```csharp
var response = await authService.ValidateAdminAsync(email, password);
if (response.Success) {
    // Login successful
} else {
    // response.Message contains error
}
```

### Create User (Register)
```csharp
var request = new RegisterRequest { ... };
var response = await authService.CreateUserAsync(request);
if (response.Success) {
    // User created, pending approval
} else {
    // response.Errors["FieldName"] = "error message"
}
```

### Get User by Email
```csharp
var user = await authService.GetUserByEmailAsync(email);
if (user != null) {
    long userId = user.UserId;
}
```

### Get All Roles
```csharp
var roles = await authService.GetRolesAsync();
// Returns List<Role> ordered by role name
```

### Logout User
```csharp
await authService.LogoutUserAsync(userId);
// Sets is_online = 0, updates last_seen
```

---

## Verification Status Values

```csharp
// User.VerificationStatus
null or false  = Pending admin approval (0)
true           = Approved (1)

// For blocked status, check AdminAction table
action_type = "block_user"
```

### Login Logic
```csharp
// 1. Check if blocked in admin_actions
if (isBlocked) return "blocked";

// 2. Check verification_status
if (status == true) return "approved";       // Allow login
if (status == false || null) return "pending"; // Show pending message
```

---

## Remember Me

### Setting
```csharp
if (request.RememberMe) {
    Response.Cookies.Append("login_email", email, new CookieOptions {
        Expires = DateTimeOffset.UtcNow.AddDays(30),
        HttpOnly = true,
        IsEssential = true,
        SameSite = SameSiteMode.Lax
    });
}
```

### Reading
```csharp
var emailCookie = Request.Cookies["login_email"] ?? "";
ViewBag.EmailCookie = emailCookie;
ViewBag.RememberMe = !string.IsNullOrEmpty(emailCookie);
```

---

## Validation Rules

### Email
- Must be valid email format
- Must be unique in users table
- Case-insensitive comparison

### Contact Number
- Must be exactly 10 digits
- Pattern: `^\d{10}$`

### Password
- Minimum 6 characters
- Must match PasswordConfirm field
- Hashed with BCrypt before storage

### Batch Year (Students/Alumni only)
- Must be 4-digit year
- Must be selected if role is Student or Alumni
- Pattern: `^\d{4}$`

### Enrollment Number (Students only)
- Required if role is Student
- Must be unique in users table
- Can contain letters and numbers

---

## CSS Classes

```css
.header-logo       /* Logo at top of page */
.auth-box          /* Login form container */
.card              /* Registration form container */
.input-box         /* Single input field wrapper */
.row               /* Form field row */
.message           /* Alert message */
.message.error     /* Error message styling */
.message.pending   /* Pending message styling */
.message.success   /* Success message styling */
.button            /* Submit button */
.remember-me       /* Remember me checkbox wrapper */
.links-container   /* Links below form */
.login-link        /* "Already registered?" link */
.error-text        /* Field validation error */
.invalid           /* Invalid field styling */
```

---

## Database Queries

### Insert New User (via EF Core)
```csharp
var user = new User {
    Email = request.Email,
    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
    FirstName = request.FirstName,
    LastName = request.LastName,
    ContactNumber = long.Parse(request.ContactNumber),
    Address = request.Address,
    RoleRequested = request.RoleRequested,
    BatchYear = request.BatchYear,
    EnrollmentNumber = request.EnrollmentNumber,
    VerificationStatus = false, // Pending
    CreatedAt = DateTime.Now
};
_context.Users.Add(user);
await _context.SaveChangesAsync();
```

### Update User on Login
```csharp
user.IsOnline = true;
user.LastSeen = DateTime.Now;
await _context.SaveChangesAsync();
```

### Get Block Reason
```csharp
var blockAction = await _context.AdminActions
    .Where(a => a.TargetUserId == userId && a.ActionType == "block_user")
    .OrderByDescending(a => a.ActionTime)
    .FirstOrDefaultAsync();
string reason = blockAction?.Reason ?? "No reason provided.";
```

---

## Common Issues & Solutions

### Issue: "Email already registered" but user not visible
**Solution:** Check `users` table - email might be in DB with different case. Use COLLATE utf8mb4_general_ci.

### Issue: Batch year dropdown empty
**Solution:** Ensure `toggleBatch()` JS function is called on role change. Check browser console for errors.

### Issue: Remember Me not persisting
**Solution:** Verify cookies are enabled in browser. Check cookie settings - must have `IsEssential = true` and `SameSite = Lax`.

### Issue: "Invalid admin password" but password correct
**Solution:** Ensure admin password in DB is hashed with BCrypt. Use `BCrypt.Net.BCrypt.HashPassword()` to hash new passwords.

### Issue: Enrollment number validation always fails
**Solution:** Clear the field value from form submission. Check that `EnrollmentNumber` is NULL for non-students.

---

## Testing Login Scenarios

### Test Admin Login
```
Email: admin@example.com
Password: admin123
Expected: Redirect to /admin/dashboard
```

### Test User Login (Approved)
```
Email: user@example.com
Password: user123
VerificationStatus: true
Expected: Redirect to /user/dashboard, session set
```

### Test User Login (Pending)
```
Email: pending@example.com
Password: pass123
VerificationStatus: false
Expected: Show "⚠️ Your account is pending admin approval."
```

### Test User Login (Blocked)
```
Email: blocked@example.com
Password: pass123
AdminActions: action_type='block_user', reason='Spam'
Expected: Show "❌ Your account has been blocked... Reason: Spam"
```

---

## Next Implementation

**Phase 2:**
- [ ] User Dashboard controller
- [ ] Admin Dashboard controller
- [ ] Admin approve/reject endpoints
- [ ] Admin block user endpoint
- [ ] Password reset OTP email
- [ ] Account profile pages

**Phase 3:**
- [ ] Authorization middleware
- [ ] Role-based access control
- [ ] Two-factor authentication
- [ ] Session timeout handling
- [ ] IP-based login tracking
