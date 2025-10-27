using System.Collections.Generic;
using System.Threading.Tasks;
using tHerdBackend.Core.DTOs.MKT;

namespace tHerdBackend.Core.Interfaces.Repositories.MKT
{
    public interface ICampaignRepository
    {
        Task<IEnumerable<MKTCampaignDto>> GetAllAsync();
        Task<IEnumerable<MKTCampaignDto>> GetActiveAsync();
        Task<MKTCampaignDto?> GetByIdAsync(int id);
    }
}
