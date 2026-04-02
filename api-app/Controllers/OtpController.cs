using api_app.DTOs.Otp;
using api_app.DTOs.Users;
using api_app.Services.Interfaces;
using api_app.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace api_app.Controllers;

[Route("api/otp")]
[ApiController]
public class OtpController : ControllerBase
{
    private readonly IOtpService _otpService;
    private readonly IUserRepository _userRepository;

    public OtpController(IOtpService otpService, IUserRepository userRepository)
    {
        _otpService = otpService;
        _userRepository = userRepository;
    }

    [HttpPost("send")]
    public async Task<ActionResult<OtpSendResponseDto>> SendOtp(
        [FromBody] OtpSendRequestDto dto,
        CancellationToken cancellationToken)
    {
        var otpCode = GenerateOtpCode();
        await _otpService.SendOtpAsync(dto.PhoneNumber, otpCode, cancellationToken);

        return Ok(new OtpSendResponseDto
        {
            Message = "OTP terkirim.",
            ExpiresInSeconds = 300
        });
    }

    [HttpPost("verify")]
    public async Task<ActionResult<OtpVerifyResponseDto>> VerifyOtp(
        [FromBody] OtpVerifyRequestDto dto,
        CancellationToken cancellationToken)
    {
        var isValid = await _otpService.VerifyOtpAsync(dto.PhoneNumber, dto.Code, cancellationToken);
        if (!isValid)
        {
            return BadRequest(new OtpVerifyResponseDto
            {
                Message = "OTP salah atau kadaluwarsa."
            });
        }

        if (dto.UserId.HasValue)
        {
            var updated = await _userRepository.UpdateOtpVerificationAsync(
                dto.UserId.Value,
                dto.PhoneNumber,
                true,
                cancellationToken);

            if (updated is null)
            {
                return NotFound(new OtpVerifyResponseDto
                {
                    Message = "User tidak ditemukan."
                });
            }

            return Ok(new OtpVerifyResponseDto
            {
                Message = "OTP valid.",
                User = MapUser(updated)
            });
        }

        return Ok(new OtpVerifyResponseDto
        {
            Message = "OTP valid."
        });
    }

    private static UserResponseDto MapUser(Models.User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            UserCode = user.UserCode,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber,
            IsOtpVerified = user.IsOtpVerified
        };
    }

    private static string GenerateOtpCode()
    {
        const int otpLength = 6;
        var number = RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, otpLength));
        return number.ToString($"D{otpLength}");
    }
}
