using Microsoft.AspNetCore.Http;

namespace FlexBackend.Core.Interfaces.Abstractions
{
    public interface IImageStorage
    {
        Task<string> UploadImageAsync(IFormFile file, string folder = "products");
    }
}
