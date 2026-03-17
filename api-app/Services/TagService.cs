using api_app.DTOs.Tasks;
using api_app.Repositories.Interfaces;
using api_app.Services.Interfaces;

namespace api_app.Services;

public class TagService : ITagService
{
    private readonly ITagRepository _tagRepository;

    public TagService(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository;
    }

    public async Task<IReadOnlyList<TagDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tags = await _tagRepository.GetAllAsync(cancellationToken);

        return tags
            .Select(tag => new TagDto
            {
                Id = tag.Id,
                TagName = tag.TagName,
                ColorHex = tag.ColorHex
            })
            .ToList();
    }
}
