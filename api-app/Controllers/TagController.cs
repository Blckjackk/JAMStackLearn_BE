using api_app.DTOs.Tasks;
using api_app.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace api_app.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TagController : ControllerBase
    {
        private readonly ITagService _tagService;

        public TagController(ITagService tagService)
        {
            _tagService = tagService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<TagDto>>> GetAll(CancellationToken cancellationToken)
        {
            var tags = await _tagService.GetAllAsync(cancellationToken);
            return Ok(tags);
        }
    }
}
