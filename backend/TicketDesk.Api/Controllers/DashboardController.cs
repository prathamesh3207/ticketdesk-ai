using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketDesk.Api.Data;
using TicketDesk.Api.Dtos;

namespace TicketDesk.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Admin,Agent")]
public class DashboardController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<DashboardDto> Get()
    {
        var rows = await db.Tickets.AsNoTracking()
            .Select(t => new { t.Status, t.Priority, t.Category, t.Sentiment, t.CreatedAt, t.ResolvedAt })
            .ToListAsync();

        var resolved = rows.Where(r => r.ResolvedAt != null).ToList();
        var avgHours = resolved.Count == 0
            ? 0
            : Math.Round(resolved.Average(r => (r.ResolvedAt!.Value - r.CreatedAt).TotalHours), 1);

        var active = rows.Where(r => r.Status is "Open" or "InProgress").ToList();

        var today = DateTime.UtcNow.Date;
        var last7 = Enumerable.Range(0, 7).Select(i => today.AddDays(i - 6))
            .Select(d => new LabelCount(d.ToString("ddd"), rows.Count(r => r.CreatedAt.Date == d)))
            .ToList();

        return new DashboardDto(
            Total: rows.Count,
            Open: rows.Count(r => r.Status == "Open"),
            InProgress: rows.Count(r => r.Status == "InProgress"),
            Resolved: rows.Count(r => r.Status == "Resolved"),
            Closed: rows.Count(r => r.Status == "Closed"),
            AvgResolutionHours: avgHours,
            UrgentOpen: active.Count(r => r.Priority == "Urgent"),
            NegativeOpen: active.Count(r => r.Sentiment == "Negative"),
            ByCategory: rows.GroupBy(r => r.Category).Select(g => new LabelCount(g.Key, g.Count())).OrderByDescending(x => x.Count).ToList(),
            ByPriority: new[] { "Urgent", "High", "Medium", "Low" }
                .Select(p => new LabelCount(p, rows.Count(r => r.Priority == p))).ToList(),
            Last7Days: last7);
    }
}
