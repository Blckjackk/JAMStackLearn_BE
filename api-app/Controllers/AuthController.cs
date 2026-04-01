using api_app.DTOs.Users;
using api_app.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace api_app.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IFirebaseAuthService _firebaseAuthService;

    public AuthController(IFirebaseAuthService firebaseAuthService)
    {
        _firebaseAuthService = firebaseAuthService;
    }

    [HttpPost("firebase")]
    public async Task<ActionResult<UserResponseDto>> FirebaseLogin(
        [FromBody] FirebaseLoginRequestDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await _firebaseAuthService.LoginWithFirebaseAsync(dto.Token, cancellationToken);
            return Ok(user);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }
}
