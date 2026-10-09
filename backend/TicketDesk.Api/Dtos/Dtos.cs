using System.ComponentModel.DataAnnotations;
using TicketDesk.Api.Domain;

namespace TicketDesk.Api.Dtos;

// ---------- input ----------
public class RegisterDto
{
    [Required, MinLength(2), MaxLength(120)] public string FullName { get; set; } = "";
    [Required, EmailAddress, MaxLength(200)] public string Email { get; set; } = "";
    [Required, MinLength(6), MaxLength(100)] public string Password { get; set; } = "";
}

public class LoginDto
{
    [Required] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public class CreateTicketDto
{
    [Required, MinLength(5), MaxLength(200)] public string Title { get; set; } = "";
    [Required, MinLength(10), MaxLength(4000)] public string Description { get; set; } = "";
}

public class UpdateStatusDto { [Required] public string Status { get; set; } = ""; }
public class AssignDto { public int? AgentId { get; set; } }
public class UpdateMetaDto { public string? Priority { get; set; } public string? Category { get; set; } }

public class AddCommentDto
{
    [Required, MinLength(1), MaxLength(4000)] public string Body { get; set; } = "";
    public bool IsInternal { get; set; }
}

public class KbUpsertDto
{
    [Required, MinLength(3), MaxLength(200)] public string Title { get; set; } = "";
    [Required, MinLength(10), MaxLength(8000)] public string Content { get; set; } = "";
    [MaxLength(300)] public string Tags { get; set; } = "";
}

// ---------- output ----------
public record UserDto(int Id, string FullName, string Email, string Role);
public record AuthResponse(string Token, UserDto User);

public record TicketListItem(
    int Id, string Title, string Status, string Priority, string Category, string Sentiment,
    string CreatedByName, string? AssignedToName, DateTime CreatedAt, DateTime UpdatedAt);

public record CommentDto(int Id, int AuthorId, string AuthorName, string AuthorRole, string Body, bool IsInternal, DateTime CreatedAt);
public record KbSuggestionDto(int Id, string Title, string Snippet);

public record TicketDetailDto(
    int Id, string Title, string Description, string Status, string Priority, string Category, string Sentiment,
    string AiSummary, string AiSuggestedReply, string AiSource,
    int CreatedById, string CreatedByName, int? AssignedToId, string? AssignedToName,
    DateTime CreatedAt, DateTime UpdatedAt, DateTime? ResolvedAt,
    List<CommentDto> Comments, List<KbSuggestionDto> Suggestions);

public record SuggestReplyResult(string Reply, string Source);
public record KbArticleDto(int Id, string Title, string Content, string Tags, DateTime CreatedAt);
public record LabelCount(string Label, int Count);

public record DashboardDto(
    int Total, int Open, int InProgress, int Resolved, int Closed,
    double AvgResolutionHours, int UrgentOpen, int NegativeOpen,
    List<LabelCount> ByCategory, List<LabelCount> ByPriority, List<LabelCount> Last7Days);

public record MetaDto(string AiMode, string Version);

public static class Mapping
{
    public static UserDto ToDto(this User u) => new(u.Id, u.FullName, u.Email, u.Role);
    public static KbArticleDto ToDto(this KbArticle k) => new(k.Id, k.Title, k.Content, k.Tags, k.CreatedAt);
}
