using api_app.DTOs.Otp;
using api_app.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace api_app.Controllers;

[Route("api/otp")]
[ApiController]
public class OtpController : ControllerBase
{
    private readonly IOtpService _otpService;

    public OtpController(IOtpService otpService)
    {
        _otpService = otpService;
    }

    [HttpPost("send")]
    public async Task<ActionResult<OtpSendResponseDto>> SendOtp(
        [FromBody] OtpSendRequestDto dto,
        CancellationToken cancellationToken)
    {
        await _otpService.SendOtpAsync(dto.PhoneNumber, cancellationToken);

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

        return Ok(new OtpVerifyResponseDto
        {
            Message = "OTP valid."
        });
    }
}
