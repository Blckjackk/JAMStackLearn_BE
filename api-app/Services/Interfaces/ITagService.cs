using api_app.DTOs.Tasks;

namespace api_app.Services.Interfaces;

public interface ITagService
{
    Task<IReadOnlyList<TagDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
