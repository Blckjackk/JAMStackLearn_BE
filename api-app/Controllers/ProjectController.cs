using api_app.DTOs.Projects;
using api_app.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace api_app.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProjectResponseDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var project = await _projectService.GetByIdAsync(id, cancellationToken);
            return project is null ? NotFound() : Ok(project);
        }

        [HttpGet("user/{userId:int}")]
        public async Task<ActionResult<IReadOnlyList<ProjectResponseDto>>> GetByUserId(int userId, CancellationToken cancellationToken)
        {
            var projects = await _projectService.GetByUserIdAsync(userId, cancellationToken);
            return Ok(projects);
        }

        [HttpPost]
        public async Task<ActionResult<ProjectResponseDto>> Create([FromBody] CreateProjectDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetActorUserId();
                if (!userId.HasValue)
                {
                    return BadRequest("Missing X-User-Id header.");
                }

                var created = await _projectService.CreateAsync(dto, userId.Value, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var updated = await _projectService.UpdateAsync(id, dto, cancellationToken);
                return updated ? NoContent() : NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var deleted = await _projectService.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }

        [HttpGet("{projectId:int}/members")]
        public async Task<ActionResult<IReadOnlyList<ProjectMemberDto>>> GetMembers(int projectId, CancellationToken cancellationToken)
        {
            var members = await _projectService.GetMembersAsync(projectId, cancellationToken);
            return Ok(members);
        }

        [HttpPost("{projectId:int}/members")]
        public async Task<ActionResult<ProjectMemberDto>> AddMember(int projectId, [FromBody] AddProjectMemberDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var actorUserId = GetActorUserId();
                if (!actorUserId.HasValue)
                {
                    return BadRequest("Missing X-User-Id header.");
                }

                var member = await _projectService.AddMemberAsync(projectId, actorUserId.Value, dto, cancellationToken);
                return Ok(member);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPut("{projectId:int}/members/{userId:int}")]
        public async Task<IActionResult> UpdateMemberRole(int projectId, int userId, [FromBody] UpdateProjectMemberRoleDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var actorUserId = GetActorUserId();
                if (!actorUserId.HasValue)
                {
                    return BadRequest("Missing X-User-Id header.");
                }

                var updated = await _projectService.UpdateMemberRoleAsync(projectId, userId, actorUserId.Value, dto, cancellationToken);
                return updated ? NoContent() : NotFound();
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpDelete("{projectId:int}/members/{userId:int}")]
        public async Task<IActionResult> RemoveMember(int projectId, int userId, CancellationToken cancellationToken)
        {
            try
            {
                var actorUserId = GetActorUserId();
                if (!actorUserId.HasValue)
                {
                    return BadRequest("Missing X-User-Id header.");
                }

                var removed = await _projectService.RemoveMemberAsync(projectId, userId, actorUserId.Value, cancellationToken);
                return removed ? NoContent() : NotFound();
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
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
