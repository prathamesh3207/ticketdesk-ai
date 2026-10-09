namespace TicketDesk.Api.Domain;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Agent = "Agent";
    public const string Customer = "Customer";
}

public static class TicketValues
{
    public static readonly string[] Statuses = { "Open", "InProgress", "Resolved", "Closed" };
    public static readonly string[] Priorities = { "Low", "Medium", "High", "Urgent" };
    public static readonly string[] Categories = { "Billing", "Technical", "Account", "Feature Request", "General" };
    public static readonly string[] Sentiments = { "Positive", "Neutral", "Negative" };
}

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = Roles.Customer;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Ticket
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "Open";
    public string Priority { get; set; } = "Medium";
    public string Category { get; set; } = "General";
    public string Sentiment { get; set; } = "Neutral";

    // AI output
    public string AiSummary { get; set; } = "";
    public string AiSuggestedReply { get; set; } = "";
    public string AiSource { get; set; } = "";   // "Gemini" | "Local rules" | "Seed"

    public int CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public int? AssignedToId { get; set; }
    public User? AssignedTo { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public List<Comment> Comments { get; set; } = new();
}

public class Comment
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    public int AuthorId { get; set; }
    public User? Author { get; set; }
    public string Body { get; set; } = "";
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class KbArticle
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string Tags { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
