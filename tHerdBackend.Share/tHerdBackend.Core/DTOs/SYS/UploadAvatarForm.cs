// 可放在 tHerdBackend.Core.DTOs.SYS 或 USER 皆可
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

public class UploadAvatarForm
{
	[Required]
	public IFormFile File { get; set; } = default!;
}
