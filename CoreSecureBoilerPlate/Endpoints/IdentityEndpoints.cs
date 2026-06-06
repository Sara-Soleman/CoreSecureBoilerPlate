using Domain.Entities;
using Infrastructure;
using Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace CoreSecureBoilerPlate.Endpoints
{

   
    public static class IdentityEndpoints
    {
        
        //public record RegisterRequest(string Email, string Password, string FirstName, string LastName, string Role);
        //public record LoginRequest(string Email, string Password);
        ////public record AuthResponse(string AccessToken, string Email, string FirstName);
        public record AuthResponse(string AccessToken);


        //public class VerifyOtpRequest { public string Email { get; set; } = ""; public string OtpCode { get; set; } = ""; }

        public class RegisterRequest
        {
            [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
            [JsonPropertyName("password")] public string Password { get; set; } = string.Empty;
            [JsonPropertyName("firstName")] public string FirstName { get; set; } = string.Empty;
            [JsonPropertyName("lastName")] public string LastName { get; set; } = string.Empty;
            [JsonPropertyName("role")] public string Role { get; set; } = string.Empty;
        }

        public class LoginRequest
        {
            [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
            [JsonPropertyName("password")] public string Password { get; set; } = string.Empty;
        }

        public class VerifyOtpRequest
        {
            [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
            [JsonPropertyName("otpCode")] public string OtpCode { get; set; } = string.Empty;
        }

        public static void MapIdentityEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/auth").WithTags("Authentication");

            group.MapPost("/register", async (
            [FromBody] RegisterRequest request,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager) =>
            {
                if (string.IsNullOrWhiteSpace(request.Email))
                    return Results.BadRequest(new { Error = "Email is Required!" });

                if (!await roleManager.RoleExistsAsync(request.Role))
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid>(request.Role));
                }

                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded) return Results.BadRequest(result.Errors);

                await userManager.AddToRoleAsync(user, request.Role);

                return Results.Ok(new { Message = $"The account was successfully registered as {request.Role}!" });
            });

            // 🔑 2. خطوة تسجيل الدخول الأولى (Login - توليد وإرسال الـ OTP)
            group.MapPost("/login", async (
                HttpContext context,
                [FromBody] LoginRequest request,
                UserManager<ApplicationUser> userManager,
                IEmailSender emailSender) => // حقن واجهة الإرسال هنا
            {
                var user = await userManager.FindByEmailAsync(request.Email);
                if (user == null || !user.IsActive)
                    return Results.Json(new { Error = "Incorrect login data" }, statusCode: 401);

                var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);
                if (!isPasswordValid)
                {
                    await userManager.AccessFailedAsync(user);
                    return Results.Json(new { Error = "Incorrect login data" }, statusCode: 401);
                }

                await userManager.ResetAccessFailedCountAsync(user);

                var otpCode = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
                var emailBody = $"""
                <div style="font-family: Arial, sans-serif; direction: rtl; text-align: right; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;">
                    <h2 style="color: #1e3a8a;">Digital Security Shield - Secure Bank()</h2>
                    <p>Dear customer, a login attempt to your bank account has been detected.</p>
                    <p>Your One-Time Password (OTP) is:</p>
                    <div style="font-size: 28px; font-weight: bold; color: #10b981; letter-spacing: 5px; background: #f3f4f6; padding: 10px; display: inline-block; border-radius: 5px; margin: 15px 0;">
                        {otpCode}
                    </div>
                    <p style="color: #ef4444; font-size: 12px;">This code will expire in 3 minutes. Do not share this code with anyone.</p>
                </div>
            """;

                await emailSender.SendEmailAsync(user.Email!, "Temporary Bank Verification Code (OTP)", emailBody);

                return Results.Ok(new { RequiresTwoFactor = true, Message = "The OTP code has been successfully sent to your email address." });
            })
            .RequireRateLimiting("LoginPolicy");

            group.MapPost("/verify-otp", async (
                HttpContext context,
                [FromBody] VerifyOtpRequest request,
                UserManager<ApplicationUser> userManager,
                JwtTokenGenerator tokenGenerator,
                GeolocationService geoService) =>
            {
                var user = await userManager.FindByEmailAsync(request.Email);
                if (user == null || !user.IsActive)
                    return Results.Json(new { Error = "Invalid Request" }, statusCode: 401);

                
                var isOtpValid = await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, request.OtpCode);
                if (!isOtpValid)
                {
                    return Results.Json(new { Error = "The OTP code is incorrect or has expired." }, statusCode: 401);
                }

                //  Access Token
                var roles = await userManager.GetRolesAsync(user);
                var accessToken = tokenGenerator.GenerateAccessToken(user, roles);
                // Tracking and analyzing the geographic location of the current IP
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var location = await geoService.GetLocationByIpAsync(ipAddress);

                // Generate and store the Refresh Token integrated with the website
                var refreshToken = await tokenGenerator.GenerateAndSaveRefreshTokenAsync(user.Id, ipAddress, location);

                //  cookies implanted in the user's device to blind the session
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(7)
                };
                context.Response.Cookies.Append("X-Refresh-Token", refreshToken, cookieOptions);

                return Results.Ok(new AuthResponse(accessToken));
            })
            .RequireRateLimiting("LoginPolicy");

            // Token renewal point with geo-impossible travel check (Refresh Token)
            group.MapPost("/refresh-token", async (
                HttpContext context,
                ApplicationDbContext dbContext,
                UserManager<ApplicationUser> userManager,
                JwtTokenGenerator tokenGenerator,
                GeolocationService geoService) =>
            {
                if (!context.Request.Cookies.TryGetValue("X-Refresh-Token", out var clientToken) || string.IsNullOrEmpty(clientToken))
                {
                    return Results.Json(new { Error = "Access not authorized." }, statusCode: 401);
                }

                var storedToken = await dbContext.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.Token == clientToken);
                if (storedToken == null || !storedToken.IsActive)
                {
                    return Results.Json(new { Error = "Invalid session." }, statusCode: 401);
                }

                var currentIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var currentLocation = await geoService.GetLocationByIpAsync(currentIp);

                // Detecting a geographically impossible travel vulnerability and burning all sessions in case of attack
                if (storedToken.Country != "Unknown" && currentLocation.Country != "Unknown" && storedToken.Country != currentLocation.Country)
                {
                    var timeElapsed = DateTime.UtcNow - storedToken.CreatedAtUtc;
                    if (timeElapsed < TimeSpan.FromHours(2))
                    {
                        var activeTokens = await dbContext.RefreshTokens.Where(t => t.UserId == storedToken.UserId && t.RevokedAtUtc == null).ToListAsync();
                        foreach (var token in activeTokens) token.RevokedAtUtc = DateTime.UtcNow;

                        storedToken.User.IsActive = false; // Freeze user account
                        await dbContext.SaveChangesAsync();
                        context.Response.Cookies.Delete("X-Refresh-Token");

                        return Results.Json(new { Error = "Suspicious activity and impossible geographic relocation detected! Your account has been locked for your protection." }, statusCode: 401);
                    }
                }

                // 
                storedToken.RevokedAtUtc = DateTime.UtcNow;
                var roles = await userManager.GetRolesAsync(storedToken.User);
                var newAccessToken = tokenGenerator.GenerateAccessToken(storedToken.User, roles);
                var newRefreshToken = await tokenGenerator.GenerateAndSaveRefreshTokenAsync(storedToken.User.Id, currentIp, currentLocation);

                var cookieOptions = new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = DateTime.UtcNow.AddDays(7) };
                context.Response.Cookies.Append("X-Refresh-Token", newRefreshToken, cookieOptions);

                return Results.Ok(new AuthResponse(newAccessToken));
            })
            .RequireRateLimiting("LoginPolicy");

            // Secure logout and reset browser and server (Logout)
            group.MapPost("/logout", async (HttpContext context, ApplicationDbContext dbContext) =>
            {
                if (context.Request.Cookies.TryGetValue("X-Refresh-Token", out var clientToken) && !string.IsNullOrEmpty(clientToken))
                {
                    var storedToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.Token == clientToken && t.RevokedAtUtc == null);
                    if (storedToken != null)
                    {
                        storedToken.RevokedAtUtc = DateTime.UtcNow;
                        await dbContext.SaveChangesAsync();
                    }
                }

                context.Response.Cookies.Append("X-Refresh-Token", "", new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = DateTime.UtcNow.AddDays(-1) });
                return Results.Ok(new { Message = "The session was successfully logged out and invalidated.." });
            });
        

        group.MapPost("/refresh-token", async (
    HttpContext context,
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    JwtTokenGenerator tokenGenerator,
    GeolocationService geoService) => 
            {
                // Get the token from the cookie
                if (!context.Request.Cookies.TryGetValue("X-Refresh-Token", out var clientToken) || string.IsNullOrEmpty(clientToken))
                {
                    return Results.Json(new { Error = "Access not authorized." }, statusCode: 401);
                }

                // Token Database check
                var storedToken = await dbContext.RefreshTokens
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Token == clientToken);

                if (storedToken == null || !storedToken.IsActive)
                {
                    return Results.Json(new { Error = "Invalid session." }, statusCode: 401);
                }

                // Retrieve the current geographic location of the request (instantaneously).
                var currentIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var currentLocation = await geoService.GetLocationByIpAsync(currentIp);

                // Firewall against "Impossible Travel Detection"
                if (storedToken.Country != "Unknown" && currentLocation.Country != "Unknown"
                    && storedToken.Country != currentLocation.Country)
                {
                    // Calculating the time elapsed since the session was created
                    var timeElapsed = DateTime.UtcNow - storedToken.CreatedAtUtc;

                    if (timeElapsed < TimeSpan.FromHours(2)) //If you change the country in less than two hours
                    {
                        //Confirmed attack! We are burning all current user sessions for absolute security.
                        var activeTokens = await dbContext.RefreshTokens
                            .Where(t => t.UserId == storedToken.UserId && t.RevokedAtUtc == null)
                            .ToListAsync();

                        foreach (var token in activeTokens)
                        {
                            token.RevokedAtUtc = DateTime.UtcNow; // Deactivate all devices
                        }

                        // The user's account was forcibly locked in the system for protection.
                        storedToken.User.IsActive = false;
                        await dbContext.SaveChangesAsync();

                        // Cleaning cookies from a hacker's device
                        context.Response.Cookies.Delete("X-Refresh-Token");

                        return Results.Json(new
                        {
                            Error = "Suspicious activity and an impossible geographical transfer were detected. " +
                            "The session was terminated and the account temporarily locked for your protection. Please contact your bank."
                        }, statusCode: 401);
                    }
                }

                // If the change is normal (or the country hasn't changed), we renew the token as usual.
                storedToken.RevokedAtUtc = DateTime.UtcNow; // Rejected old tokens

                var roles = await userManager.GetRolesAsync(storedToken.User);
                var newAccessToken = tokenGenerator.GenerateAccessToken(storedToken.User, roles);

                // Generating a new token with the new geographic location (session rotation and location)
                var newRefreshToken = await tokenGenerator.GenerateAndSaveRefreshTokenAsync(storedToken.User.Id, currentIp, currentLocation);

                var cookieOptions = new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = DateTime.UtcNow.AddDays(7) };
                context.Response.Cookies.Append("X-Refresh-Token", newRefreshToken, cookieOptions);

                return Results.Ok(new AuthResponse(newAccessToken));
            })
                .RequireRateLimiting("LoginPolicy");


            // Establishing a connection point for secure logout and session burning
            group.MapPost("/logout", async (
                HttpContext context,
                ApplicationDbContext dbContext) =>
            {
               
                if (context.Request.Cookies.TryGetValue("X-Refresh-Token", out var clientToken) && !string.IsNullOrEmpty(clientToken))
                {
                   
                    var storedToken = await dbContext.RefreshTokens
                        .FirstOrDefaultAsync(t => t.Token == clientToken && t.RevokedAtUtc == null);

                    if (storedToken != null)
                    {
                        
                        storedToken.RevokedAtUtc = DateTime.UtcNow;
                        await dbContext.SaveChangesAsync();
                    }
                }

                //  The browser was instructed to completely delete the cookie from its device by resetting its validity.
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(-1) //The date format in the past forces the browser to delete it immediately.
                };
                context.Response.Cookies.Append("X-Refresh-Token", "", cookieOptions);

                return Results.Ok(new { Message = "The logout was successful and the session was safely terminated." });
            });
        }

    }

}
