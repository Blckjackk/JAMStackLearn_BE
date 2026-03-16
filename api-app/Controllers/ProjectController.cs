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
                var created = await _projectService.CreateAsync(dto, cancellationToken);
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
    }
}
