using TicketDesk.Api.Domain;

namespace TicketDesk.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (db.Users.Any()) return;

        User U(string name, string email, string pwd, string role) => new()
        {
            FullName = name, Email = email, Role = role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(pwd)
        };

        var admin = U("Admin User", "admin@ticketdesk.com", "Admin@123", Roles.Admin);
        var agent = U("Aarav Mehta", "agent@ticketdesk.com", "Agent@123", Roles.Agent);
        var agent2 = U("Priya Sharma", "priya@ticketdesk.com", "Agent@123", Roles.Agent);
        var cust = U("Rahul Verma", "customer@ticketdesk.com", "Customer@123", Roles.Customer);
        var cust2 = U("Sneha Kulkarni", "sneha@ticketdesk.com", "Customer@123", Roles.Customer);
        db.Users.AddRange(admin, agent, agent2, cust, cust2);
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;

        Ticket T(string title, string desc, string status, string priority, string category, string sentiment,
                 string summary, User creator, User? assignee, double daysAgo, double? resolveAfterHours = null)
        {
            var created = now.AddDays(-daysAgo);
            return new Ticket
            {
                Title = title, Description = desc, Status = status, Priority = priority, Category = category,
                Sentiment = sentiment, AiSummary = summary, AiSource = "Seed",
                CreatedById = creator.Id, AssignedToId = assignee?.Id,
                CreatedAt = created, UpdatedAt = created.AddHours(resolveAfterHours ?? 1),
                ResolvedAt = resolveAfterHours.HasValue ? created.AddHours(resolveAfterHours.Value) : null
            };
        }

        var tickets = new List<Ticket>
        {
            T("Payment deducted twice for my subscription",
              "I was charged twice for this month's subscription (two debits of the same amount on the 3rd). This is really frustrating, please refund the extra charge.",
              "Open", "High", "Billing", "Negative", "Customer was charged twice for the monthly subscription and wants a refund.", cust, null, 0.2),
            T("Cannot login after password reset",
              "I reset my password using the email link, but now I get 'invalid credentials' every time I try to sign in. I'm blocked from my account.",
              "InProgress", "High", "Account", "Neutral", "Login fails with invalid credentials after a password reset.", cust2, agent, 1.1),
            T("App crashes when exporting report to PDF",
              "Whenever I click Export to PDF on the monthly report, the page freezes and shows an error. Happens on Chrome and Edge.",
              "InProgress", "High", "Technical", "Neutral", "PDF export crashes the page on monthly reports in Chrome and Edge.", cust, agent2, 2.4),
            T("Request: dark mode for the dashboard",
              "Love the product! It would be great to have a dark mode option for the dashboard, my eyes get tired at night.",
              "Open", "Low", "Feature Request", "Positive", "Customer would like a dark mode option for the dashboard.", cust2, null, 3.0),
            T("Production API returning 500 errors for all users",
              "Our production integration started returning HTTP 500 about 20 minutes ago for every request. This is critical, our customers are impacted. Please escalate ASAP.",
              "Open", "Urgent", "Technical", "Negative", "Production API is returning 500 errors for all users; customer needs urgent escalation.", cust, null, 0.05),
            T("How do I change my billing email?",
              "Could you tell me where I can update the email address that receives invoices?",
              "Resolved", "Low", "Billing", "Neutral", "Customer asks how to change the invoice email address.", cust2, agent, 4.5, 3),
            T("Invoice PDF is missing GST number",
              "The invoice I downloaded does not show our GST number, which we need for filing. Can you fix this?",
              "Resolved", "Medium", "Billing", "Neutral", "Downloaded invoice does not include the customer's GST number.", cust, agent2, 5.2, 20),
            T("Dashboard loading is very slow",
              "The dashboard takes more than 30 seconds to load since yesterday. Still waiting on a fix, this is getting annoying.",
              "Closed", "Medium", "Technical", "Negative", "Dashboard load time exceeds 30 seconds since yesterday.", cust2, agent, 6.1, 8),
            T("Account locked after multiple login attempts",
              "My account got locked after I typed the wrong password a few times. How can I unlock it?",
              "Resolved", "High", "Account", "Neutral", "Account locked after repeated failed login attempts.", cust, agent2, 2.9, 2),
            T("Add Excel export to the reports page",
              "We'd like to export the reports as Excel files in addition to PDF. Would be a great enhancement for our finance team.",
              "Open", "Low", "Feature Request", "Neutral", "Customer requests Excel export for reports.", cust2, null, 1.6),
        };
        db.Tickets.AddRange(tickets);
        await db.SaveChangesAsync();

        // a few conversation threads
        db.Comments.AddRange(
            new Comment { TicketId = tickets[1].Id, AuthorId = agent.Id, Body = "Hi Sneha, sorry about that. I've cleared the stale session on our side. Could you try logging in again in a private window?", CreatedAt = now.AddDays(-1.0) },
            new Comment { TicketId = tickets[1].Id, AuthorId = cust2.Id, Body = "Trying now, will let you know.", CreatedAt = now.AddDays(-0.95) },
            new Comment { TicketId = tickets[1].Id, AuthorId = agent.Id, IsInternal = true, Body = "Looks like the reset token was reused. Raise a bug with the auth team if it repeats.", CreatedAt = now.AddDays(-0.9) },
            new Comment { TicketId = tickets[2].Id, AuthorId = agent2.Id, Body = "Thanks Rahul, we can reproduce this on large reports. Engineering is investigating.", CreatedAt = now.AddDays(-2.0) },
            new Comment { TicketId = tickets[5].Id, AuthorId = agent.Id, Body = "You can change it under Settings > Billing > Invoice email. Let us know if you can't find it!", CreatedAt = now.AddDays(-4.4) }
        );

        db.KbArticles.AddRange(
            new KbArticle { Title = "How to reset your password", Tags = "password login account reset locked",
                Content = "Go to the sign-in page and choose 'Forgot password'. Enter your registered email and open the reset link within 30 minutes. If login still fails after a reset, clear your browser cache or try a private window. After five failed attempts an account is locked for 15 minutes." },
            new KbArticle { Title = "Refunds and duplicate charges", Tags = "billing refund charge payment duplicate subscription",
                Content = "If you were charged twice, share the transaction dates and the last four digits of the card. Duplicate charges are reversed by the billing team within 5-7 business days once verified." },
            new KbArticle { Title = "Changing your billing email and invoice details", Tags = "billing invoice email gst receipt",
                Content = "Open Settings > Billing. You can update the invoice email, company name and GST number. New details apply to future invoices; contact support to regenerate past invoices." },
            new KbArticle { Title = "Troubleshooting slow pages and crashes", Tags = "technical slow crash error loading freeze browser",
                Content = "Hard refresh the page (Ctrl+F5), disable browser extensions, and try another browser. If the problem remains, send us the exact error message, a screenshot and the time it happened so engineering can trace it." },
            new KbArticle { Title = "API errors and status codes", Tags = "api 500 error integration production outage",
                Content = "A 500 response means an unexpected server error. Check the status page first. For production-impacting incidents, mark the ticket urgent and include request IDs and timestamps so we can escalate to on-call engineers." },
            new KbArticle { Title = "Exporting reports", Tags = "export pdf excel csv report",
                Content = "Reports can be exported to PDF from the report toolbar. CSV export is available for tables. Excel export is on our roadmap; vote for it by raising a feature request." }
        );

        await db.SaveChangesAsync();
    }
}
