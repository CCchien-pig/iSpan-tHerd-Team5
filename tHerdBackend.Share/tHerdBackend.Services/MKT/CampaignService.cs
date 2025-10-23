using tHerdBackend.Core.DTOs.MKT;
using tHerdBackend.Core.Interfaces.MKT;
using tHerdBackend.Core.Interfaces.Repositories.MKT;

namespace tHerdBackend.Infrastructure.Services.MKT
{
    public class CampaignService : ICampaignService
    {
        private readonly ICampaignRepository _repo;

        public CampaignService(ICampaignRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<MKTCampaignDto>> GetAllAsync()
        {
            return await _repo.GetAllAsync();
        }

        public async Task<IEnumerable<MKTCampaignDto>> GetActiveAsync()
        {
            return await _repo.GetActiveAsync();
        }

        public async Task<MKTCampaignDto?> GetByIdAsync(int id)
        {
            return await _repo.GetByIdAsync(id);
        }
    }
}
