using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.MKT;
using tHerdBackend.Core.Interfaces.Repositories.MKT;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Infrastructure.Repositories.MKT
{
    public class CampaignRepository : ICampaignRepository
    {
        private readonly tHerdDBContext _context;

        public CampaignRepository(tHerdDBContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MKTCampaignDto>> GetAllAsync()
        {
            return await _context.MktCampaigns
                .Select(c => new MKTCampaignDto
                {
                    CampaignId = c.CampaignId,
                    CampaignName = c.CampaignName,
                    CampaignType = c.CampaignType,
                    CampaignDescription = c.CampaignDescription,
                    ProductType = c.ProductType,
                    Status = c.Status,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    ImgId = c.ImgId,
                    IsActive = c.IsActive,
                    Creator = c.Creator,
                    CreatedDate = c.CreatedDate,
                    Reviser = c.Reviser,
                    RevisedDate = c.RevisedDate
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<MKTCampaignDto>> GetActiveAsync()
        {
            return await _context.MktCampaigns
                .Where(c => c.IsActive == true)
                .Select(c => new MKTCampaignDto
                {
                    CampaignId = c.CampaignId,
                    CampaignName = c.CampaignName,
                    CampaignType = c.CampaignType,
                    CampaignDescription = c.CampaignDescription,
                    ProductType = c.ProductType,
                    Status = c.Status,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    ImgId = c.ImgId,
                    IsActive = c.IsActive,
                    Creator = c.Creator,
                    CreatedDate = c.CreatedDate,
                    Reviser = c.Reviser,
                    RevisedDate = c.RevisedDate
                })
                .ToListAsync();
        }

        public async Task<MKTCampaignDto?> GetByIdAsync(int id)
        {
            return await _context.MktCampaigns
                .Where(c => c.CampaignId == id)
                .Select(c => new MKTCampaignDto
                {
                    CampaignId = c.CampaignId,
                    CampaignName = c.CampaignName,
                    CampaignType = c.CampaignType,
                    CampaignDescription = c.CampaignDescription,
                    ProductType = c.ProductType,
                    Status = c.Status,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    ImgId = c.ImgId,
                    IsActive = c.IsActive,
                    Creator = c.Creator,
                    CreatedDate = c.CreatedDate,
                    Reviser = c.Reviser,
                    RevisedDate = c.RevisedDate
                })
                .FirstOrDefaultAsync();
        }
    }
}
