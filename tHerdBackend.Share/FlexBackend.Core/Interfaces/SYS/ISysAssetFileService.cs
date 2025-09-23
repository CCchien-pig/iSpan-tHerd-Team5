using FlexBackend.Core.DTOs;
using Microsoft.AspNetCore.Http;

namespace FlexBackend.Core.Interfaces.SYS
{
    public interface ISysAssetFileService
    {
        Task<bool> AddImages(List<IFormFile> files);
    }
}
