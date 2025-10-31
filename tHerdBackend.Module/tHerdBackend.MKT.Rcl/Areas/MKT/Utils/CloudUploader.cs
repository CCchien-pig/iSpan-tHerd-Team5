using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.MKT.Rcl.Areas.MKT.Utils
{
    public static class CloudUploader
    {
        private static readonly Cloudinary _cloudinary;

        static CloudUploader()
        {
            // ✅ 你需設定自己的 Cloudinary 帳號資訊
            var account = new Account(
                "你的_cloud_name",
                "你的_api_key",
                "你的_api_secret"
            );
            _cloudinary = new Cloudinary(account);
        }

        public static async Task<string> UploadAsync(IFormFile file)
        {
            using (var stream = file.OpenReadStream())
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "tHerd/uploads/ads"
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                return uploadResult.SecureUrl.ToString(); // ✅ 回傳雲端圖片 URL
            }
        }
    }
}

