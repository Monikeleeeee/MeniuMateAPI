using MeniuMate_API.Auth.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace MeniuMate_API.Auth
{
    public static class AuthEndpoints
    {
        public static void AddAuthApi(this WebApplication app)
        {
            //register
            app.MapPost("api/register", async (UserManager<ForumRestUser> userManager, RegisterUserDto registerUserDto) =>
            {
                var user = await userManager.FindByNameAsync(registerUserDto.UserName);

                if (user != null)
                    return Results.UnprocessableEntity("User name already taken.");

                var newUser = new ForumRestUser
                {
                    Email = registerUserDto.Email,
                    UserName = registerUserDto.UserName
                };

                var createUserResult = await userManager.CreateAsync(newUser, registerUserDto.Password);
                if (!createUserResult.Succeeded)
                    return Results.UnprocessableEntity();

                await userManager.AddToRoleAsync(newUser, ForumRoles.ForumUser);

                return Results.Created("api.login", new UserDto(newUser.Id, newUser.UserName, newUser.Email));
            });

            //login
            app.MapPost("api/login", async (UserManager<ForumRestUser> userManager, JwtTokenService jwtTokenService, LoginDto loginDto) =>
            {
                var user = await userManager.FindByNameAsync(loginDto.UserName);

                if (user == null)
                    return Results.UnprocessableEntity("Username or password was incorrect.");

                var isPasswordValid = await userManager.CheckPasswordAsync(user, loginDto.Password);

                if (!isPasswordValid)
                    return Results.UnprocessableEntity("Username or password was incorrect.");

                user.ForceRelogin = false;
                await userManager.UpdateAsync(user);

                var roles = await userManager.GetRolesAsync(user);

                var accessToken = jwtTokenService.CreateAccessToken(user.UserName, user.Id, roles);
                var refreshToken = jwtTokenService.CreateRefreshToken(user.Id);

                return Results.Ok(new SuccessfulLoginDto(accessToken, refreshToken));
            });

            //accessToken
            app.MapPost("api/accessToken", async (UserManager<ForumRestUser> userManager, JwtTokenService jwtTokenService, RefreshAccessTokenDto refreshAccessTokenDto) =>
            {
                if(!jwtTokenService.TryParseRefreshToken(refreshAccessTokenDto.RefreshToken, out var claims))
                {
                    return Results.UnprocessableEntity();
                }

                var userId = claims.FindFirstValue(JwtRegisteredClaimNames.Sub);

                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                    return Results.UnprocessableEntity("Invalid token");

                if(user.ForceRelogin)
                {
                    return Results.UnprocessableEntity();
                }

                var roles = await userManager.GetRolesAsync(user);

                var accessToken = jwtTokenService.CreateAccessToken(user.UserName, user.Id, roles);
                var refreshToken = jwtTokenService.CreateRefreshToken(user.Id);

                return Results.Ok(new SuccessfulLoginDto(accessToken, refreshToken));

            });

            //logout
            app.MapPost("/api/logout", async (JwtTokenService jwtTokenService, HttpContext httpContext, UserManager<ForumRestUser> userManager) =>
            {
                var authHeader = httpContext.Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                    return Results.Unauthorized();

                var token = authHeader.Substring("Bearer ".Length).Trim();

                if (!jwtTokenService.TryParseRefreshToken(token, out var claims))
                    return Results.Unauthorized();

                var userId = claims.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

                var user = await userManager.FindByIdAsync(userId);
                if (user == null) return Results.Unauthorized();

                user.ForceRelogin = true;
                await userManager.UpdateAsync(user);

                return Results.Ok("Logged out successfully.");
            });
        }
    }

    public record RegisterUserDto(string UserName, string Email, string Password);
    public record UserDto(string UserId, string UserName, string Email);
    public record LoginDto(string UserName, string Password);
    public record SuccessfulLoginDto(string AccessToken, string RefreshToken);
    public record RefreshAccessTokenDto(string RefreshToken);
}
