using FlexBackend.Core.DTOs;
using Microsoft.AspNetCore.Http;

namespace FlexBackend.Core.Interfaces.SYS
{
    public interface ISysAssetFileRepository
    {
        Task<bool> AddImages(List<IFormFile> files);
    }
}
