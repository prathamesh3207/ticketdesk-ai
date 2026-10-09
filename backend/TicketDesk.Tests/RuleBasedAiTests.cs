using TicketDesk.Api.Domain;
using TicketDesk.Api.Services;
using Xunit;

namespace TicketDesk.Tests;

public class RuleBasedAiTests
{
    [Fact]
    public void Billing_ticket_is_categorised_as_billing()
    {
        var r = RuleBasedAi.Triage("Refund please", "I was charged twice for my subscription invoice.");
        Assert.Equal("Billing", r.Category);
    }

    [Fact]
    public void Production_outage_is_urgent()
    {
        var r = RuleBasedAi.Triage("API down", "Production is returning 500 errors for all users, critical!");
        Assert.Equal("Urgent", r.Priority);
        Assert.Equal("Technical", r.Category);
    }

    [Fact]
    public void Feature_request_is_low_priority()
    {
        var r = RuleBasedAi.Triage("Request: dark mode", "It would be great to have a dark mode feature, thanks!");
        Assert.Equal("Feature Request", r.Category);
        Assert.Equal("Low", r.Priority);
        Assert.Equal("Positive", r.Sentiment);
    }

    [Fact]
    public void Angry_customer_is_negative()
    {
        var r = RuleBasedAi.Triage("Terrible service", "This is unacceptable, I am frustrated and still waiting.");
        Assert.Equal("Negative", r.Sentiment);
    }

    [Fact]
    public void Summary_is_trimmed_to_140_chars()
    {
        var r = RuleBasedAi.Summarize("t", new string('a', 400));
        Assert.True(r.Length <= 140);
    }

    [Fact]
    public void Reply_greets_customer_by_first_name_and_lists_kb()
    {
        var t = new Ticket { Title = "Login issue", Priority = "High", Sentiment = "Negative", CreatedBy = new User { FullName = "Rahul Verma" } };
        var reply = RuleBasedAi.Reply(t, new[] { new KbArticle { Title = "How to reset your password" } });
        Assert.StartsWith("Hi Rahul", reply);
        Assert.Contains("How to reset your password", reply);
    }

    [Fact]
    public void Kb_tokenizer_drops_stop_words()
    {
        var tokens = KbSearchService.Tokens("How to reset the password").ToList();
        Assert.Contains("password", tokens);
        Assert.DoesNotContain("the", tokens);
    }
}
