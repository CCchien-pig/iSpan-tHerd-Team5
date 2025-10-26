using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.DTOs;
using tHerdBackend.Core.Interfaces.SYS;

namespace tHerdBackend.SYS.Rcl.Areas.SYS.Controllers
{
    /// <summary>
    /// 共用上傳控制器基底，處理 Cloudinary / 本地上傳邏輯
    /// </summary>
    public abstract class BaseUploadController : Controller
    {
        protected readonly ISysAssetFileService _fileService;
        protected readonly IWebHostEnvironment _env;

        protected BaseUploadController(ISysAssetFileService fileService, IWebHostEnvironment env)
        {
            _fileService = fileService;
            _env = env;
        }

        /// <summary>
        /// 上傳檔案（由子類指定 ModuleId / ProgId）
        /// </summary>
        protected async Task<IActionResult> HandleUploadAsync(string moduleId, string progId)
        {
            try
            {
                var form = await Request.ReadFormAsync();

                var uploadDto = new AssetFileUploadDto
                {
                    ModuleId = moduleId,
                    ProgId = progId,
                    IsExternal = bool.TryParse(form["IsExternal"], out var isExt) && isExt,
                    Meta = new List<AssetFileDetailsDto>()
                };

                for (int i = 0; i < form.Files.Count; i++)
                {
                    var file = form.Files[i];
                    uploadDto.Meta.Add(new AssetFileDetailsDto
                    {
                        File = file,
                        AltText = form[$"Meta[{i}].AltText"],
                        Caption = form[$"Meta[{i}].Caption"],
                        IsActive = true
                    });
                }

                // 驗證
                if (uploadDto.Meta.Count == 0)
                {
                    TempData["ErrorMessage"] = "請至少選擇一個檔案";
                    var files = await _fileService.GetFilesByProg(moduleId, progId);
                    return View("Index", files);
                }


                if (uploadDto.IsExternal)
                {
                    await _fileService.AddFilesAsync(uploadDto);
                    TempData["SuccessMessage"] = $"✅ 已成功上傳 {uploadDto.Meta.Count} 個檔案至 Cloudinary";
                }
                else
                {
                    var uploadRoot = Path.Combine(_env.WebRootPath, "uploads", moduleId, progId);
                    if (!Directory.Exists(uploadRoot))
                        Directory.CreateDirectory(uploadRoot);

                    foreach (var meta in uploadDto.Meta)
                    {
                        if (meta.File == null || meta.File.Length == 0)
                            continue;

                        var fileExt = Path.GetExtension(meta.File.FileName);
                        var fileName = $"{Guid.NewGuid()}{fileExt}";
                        var filePath = Path.Combine(uploadRoot, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                            await meta.File.CopyToAsync(stream);

                        var relativeUrl = $"/uploads/{moduleId}/{progId}/{fileName}";
                        await _fileService.AddLocalFileAsync(uploadDto, meta, relativeUrl);
                    }

                    TempData["SuccessMessage"] = $"✅ 已成功上傳 {uploadDto.Meta.Count} 個檔案至本地";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"❌ 上傳失敗：{ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}
