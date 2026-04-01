using api_app.DTOs.Users;
using api_app.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace api_app.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<UserResponseDto>>> GetAll(CancellationToken cancellationToken)
        {
            var users = await _userService.GetAllAsync(cancellationToken);
            return Ok(users);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserResponseDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var user = await _userService.GetByIdAsync(id, cancellationToken);
            return user is null ? NotFound() : Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<UserResponseDto>> Create([FromBody] CreateUserDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var created = await _userService.CreateAsync(dto, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserResponseDto>> Login([FromBody] LoginUserDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userService.LoginAsync(dto, cancellationToken);
                return user is null ? Unauthorized("Invalid email or password.") : Ok(user);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("profile")]
        [HttpPost("profile")]
        public async Task<ActionResult<UserResponseDto>> UpdateProfile([FromBody] UpdateUserProfileDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetActorUserId();
                if (!userId.HasValue)
                {
                    return BadRequest("Missing X-User-Id header.");
                }

                var updated = await _userService.UpdateProfileAsync(userId.Value, dto, cancellationToken);
                return updated is null ? NotFound("User not found.") : Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private int? GetActorUserId()
        {
            if (!Request.Headers.TryGetValue("X-User-Id", out var value))
            {
                return null;
            }

            return int.TryParse(value.ToString(), out var userId) ? userId : null;
        }
    }
}
