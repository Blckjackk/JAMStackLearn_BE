using api_app.DTOs.Tasks;
using api_app.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace api_app.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TaskController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TaskResponseDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var task = await _taskService.GetByIdAsync(id, cancellationToken);
            return task is null ? NotFound() : Ok(task);
        }

        [HttpGet("project/{projectId:int}")]
        public async Task<ActionResult<IReadOnlyList<TaskResponseDto>>> GetByProjectId(int projectId, CancellationToken cancellationToken)
        {
            var tasks = await _taskService.GetByProjectIdAsync(projectId, cancellationToken);
            return Ok(tasks);
        }

        [HttpPost]
        public async Task<ActionResult<TaskResponseDto>> Create([FromBody] CreateTaskDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var actorUserId = GetActorUserId();
                if (!actorUserId.HasValue)
                {
                    return BadRequest("Missing X-User-Id header.");
                }

                var created = await _taskService.CreateAsync(dto, actorUserId.Value, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
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
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var actorUserId = GetActorUserId();
                if (!actorUserId.HasValue)
                {
                    return BadRequest("Missing X-User-Id header.");
                }

                var updated = await _taskService.UpdateAsync(id, dto, actorUserId.Value, cancellationToken);
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
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var deleted = await _taskService.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
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
