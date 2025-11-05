using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tHerdBackend.Core.DTOs.MKT;

namespace tHerdBackend.Core.Interfaces.MKT
{
    public interface IMktPromotionService
    {
        Task<PromotionCalculateResponseDto> CalculatePromotionAsync(PromotionCalculateRequestDto dto);
    }

}
