using TicketDesk.Api.Domain;

namespace TicketDesk.Api.Services;

public record TriageResult(string Category, string Priority, string Sentiment, string Summary, string Source);

/// <summary>
/// Offline fallback so the app works with no API key (and when the AI provider is down / rate-limited).
/// </summary>
public static class RuleBasedAi
{
    private static readonly (string Category, string[] Words)[] CategoryRules =
    {
        ("Billing", new[] { "invoice", "refund", "charge", "charged", "payment", "billing", "subscription", "receipt", "gst", "deducted", "price" }),
        ("Account", new[] { "password", "login", "log in", "sign in", "locked", "account", "reset", "otp", "2fa", "username" }),
        ("Technical", new[] { "error", "crash", "bug", "exception", "not working", "500", "down", "slow", "timeout", "failed", "broken", "freeze", "loading" }),
        ("Feature Request", new[] { "feature", "would like", "suggest", "wish", "enhancement", "add support", "dark mode", "integration", "request:" }),
    };

    private static readonly string[] Urgent = { "urgent", "asap", "outage", "production", "critical", "data loss", "security", "hacked", "breach", "all users" };
    private static readonly string[] High = { "cannot", "can't", "cant", "unable", "failed", "error", "blocked", "crash", "locked", "twice", "500", "not working" };
    private static readonly string[] Medium = { "slow", "issue", "problem", "wrong", "missing", "incorrect", "delay" };
    private static readonly string[] Negative = { "angry", "terrible", "worst", "frustrated", "unacceptable", "disappointed", "annoying", "still not", "waiting", "ridiculous", "useless", "again" };
    private static readonly string[] Positive = { "thanks", "thank you", "great", "love", "appreciate", "awesome", "excellent" };

    public static TriageResult Triage(string title, string description)
    {
        var text = $"{title} {description}".ToLowerInvariant();

        var category = "General";
        var best = 0;
        foreach (var (name, words) in CategoryRules)
        {
            var hits = words.Count(w => text.Contains(w));
            if (hits > best) { best = hits; category = name; }
        }

        var priority = Has(text, Urgent) ? "Urgent"
                     : Has(text, High) ? "High"
                     : Has(text, Medium) ? "Medium" : "Low";
        if (category == "Feature Request" && priority != "Urgent") priority = "Low";

        var sentiment = Has(text, Negative) ? "Negative" : Has(text, Positive) ? "Positive" : "Neutral";

        return new TriageResult(category, priority, sentiment, Summarize(title, description), "Local rules");
    }

    public static string Reply(Ticket t, IEnumerable<KbArticle> kb)
    {
        var first = (t.CreatedBy?.FullName ?? "there").Split(' ')[0];
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Hi {first},");
        sb.AppendLine();
        sb.Append($"Thank you for contacting us about \"{t.Title}\". ");
        if (t.Sentiment == "Negative") sb.Append("I'm sorry for the trouble this has caused. ");
        sb.AppendLine(t.Priority is "Urgent" or "High"
            ? $"We've marked this as {t.Priority} priority and are looking into it right away."
            : "We're looking into this and will update you shortly.");

        var list = kb.Take(3).ToList();
        if (list.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("These help-centre articles may help in the meantime:");
            foreach (var a in list) sb.AppendLine($"- {a.Title}");
        }

        sb.AppendLine();
        sb.AppendLine("If you can share any extra details (exact error message, screenshots, or the steps you took), we can resolve this faster.");
        sb.AppendLine();
        sb.AppendLine("Best regards,");
        sb.Append("TicketDesk Support");
        return sb.ToString();
    }

    public static string Summarize(string title, string description)
    {
        var first = description.Split(new[] { '.', '!', '?', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                               .FirstOrDefault();
        var s = string.IsNullOrWhiteSpace(first) ? title : first;
        return s.Length > 140 ? s[..137] + "..." : s;
    }

    private static bool Has(string text, string[] words) => words.Any(text.Contains);
}
