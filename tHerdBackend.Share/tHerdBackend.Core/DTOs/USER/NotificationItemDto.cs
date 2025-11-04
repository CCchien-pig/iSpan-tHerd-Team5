namespace tHerdBackend.Core.DTOs.USER;

public record NotificationItemDto(
	int Id,
	string Type,
	string Title,
	string? Content,
	string? Channel,
	DateTime SentAt,
	string Status,
	string? ModuleId,
	bool IsRead
);
