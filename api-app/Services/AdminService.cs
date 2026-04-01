using api_app.DTOs.Admin;
using api_app.Repositories.Interfaces;
using api_app.Services.Interfaces;

namespace api_app.Services;

public class AdminService : IAdminService
{
    private readonly IAdminRepository _adminRepository;

    public AdminService(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    public Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        return _adminRepository.GetDashboardAsync(cancellationToken);
    }
}
