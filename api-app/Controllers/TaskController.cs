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
                var created = await _taskService.CreateAsync(dto, cancellationToken);
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
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var updated = await _taskService.UpdateAsync(id, dto, cancellationToken);
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
            var deleted = await _taskService.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
    }
}
