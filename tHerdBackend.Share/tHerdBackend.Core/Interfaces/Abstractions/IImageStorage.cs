using Microsoft.AspNetCore.Http;

namespace tHerdBackend.Core.Interfaces.Abstractions
{
    public interface IImageStorage
    {
        Task<string> UploadImageAsync(IFormFile file, string folder = "products");
    }
}
