using System.Collections.Generic;
using System.Threading.Tasks;
using tHerdBackend.Core.DTOs.MKT;

namespace tHerdBackend.Core.Interfaces.MKT
{
    public interface ICampaignService
    {
        Task<IEnumerable<MKTCampaignDto>> GetAllAsync();
        Task<IEnumerable<MKTCampaignDto>> GetActiveAsync();
        Task<MKTCampaignDto?> GetByIdAsync(int id);
    }
}
