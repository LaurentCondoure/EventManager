using EventManager.Api.Auth.Authentication;
using EventManager.Domain.Identity.Constants;
using EventManager.Domain.Identity.DTOs;
using EventManager.Domain.Identity.Interfaces;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventManager.Api.Controllers;

[ApiController]
[Route("auth")]
[EnableRateLimiting("fixed")]
public class AuthenticationController(IAuthenticationService authenticationService, ILogger<AuthenticationController> logger) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginInput request)
    {
        LoginResult result = await authenticationService.LoginAsync(request.Email, request.Password);

        Response.Cookies.Append(
            AuthCookieNames.AccessToken, result.Tokens.AccessToken, AuthCookieOptions.Create(result.Tokens.AccessTokenExpiresAt));
        Response.Cookies.Append(
            AuthCookieNames.RefreshToken, result.Tokens.RefreshToken, AuthCookieOptions.Create(result.Tokens.RefreshTokenExpiresAt));

        logger.LogInformation("Login succeeded, role {Role}", result.Role);

        return Ok(new LoginResponseDto(result.Role.ToRoleName(), result.MustResetPassword));
    }
}
