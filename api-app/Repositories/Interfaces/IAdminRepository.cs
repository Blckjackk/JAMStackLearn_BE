using api_app.DTOs.Admin;

namespace api_app.Repositories.Interfaces;

public interface IAdminRepository
{
    Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
