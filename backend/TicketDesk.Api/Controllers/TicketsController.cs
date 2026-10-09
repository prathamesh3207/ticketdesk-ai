using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketDesk.Api.Data;
using TicketDesk.Api.Domain;
using TicketDesk.Api.Dtos;
using TicketDesk.Api.Services;

namespace TicketDesk.Api.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize]
public class TicketsController(AppDbContext db, IAiService ai, KbSearchService kb) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue("sub")!);
    private bool IsStaff => User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Agent);

    // ---------------- list ----------------
    [HttpGet]
    public async Task<ActionResult<List<TicketListItem>>> List(
        [FromQuery] string? status, [FromQuery] string? q, [FromQuery] bool mine = false)
    {
        var uid = CurrentUserId;
        var staff = IsStaff;

        var query = db.Tickets.AsNoTracking().AsQueryable();
        if (!staff) query = query.Where(t => t.CreatedById == uid);
        else if (mine) query = query.Where(t => t.AssignedToId == uid);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(t => t.Status == status);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(t => t.Title.Contains(term) || t.Description.Contains(term));
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TicketListItem(
                t.Id, t.Title, t.Status, t.Priority, t.Category, t.Sentiment,
                t.CreatedBy!.FullName,
                t.AssignedTo == null ? null : t.AssignedTo.FullName,
                t.CreatedAt, t.UpdatedAt))
            .ToListAsync();
    }

    // ---------------- detail ----------------
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketDetailDto>> Get(int id)
    {
        var t = await LoadAsync(id);
        if (t is null) return NotFound(new { message = "Ticket not found." });
        return await ToDetailAsync(t);
    }

    // ---------------- create (AI triage happens here) ----------------
    [HttpPost]
    public async Task<ActionResult<TicketDetailDto>> Create(CreateTicketDto dto)
    {
        var triage = await ai.TriageAsync(dto.Title.Trim(), dto.Description.Trim());

        var ticket = new Ticket
        {
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            Category = triage.Category,
            Priority = triage.Priority,
            Sentiment = triage.Sentiment,
            AiSummary = triage.Summary,
            AiSource = triage.Source,
            CreatedById = CurrentUserId
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var created = await LoadAsync(ticket.Id);
        return CreatedAtAction(nameof(Get), new { id = ticket.Id }, await ToDetailAsync(created!));
    }

    // ---------------- status ----------------
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<TicketDetailDto>> UpdateStatus(int id, UpdateStatusDto dto)
    {
        var t = await LoadAsync(id);
        if (t is null) return NotFound(new { message = "Ticket not found." });

        var status = TicketValues.Statuses.FirstOrDefault(s => s.Equals(dto.Status, StringComparison.OrdinalIgnoreCase));
        if (status is null) return BadRequest(new { message = "Invalid status." });
        if (!IsStaff && status is not ("Closed" or "Open"))
            return Forbid();

        t.Status = status;
        t.UpdatedAt = DateTime.UtcNow;
        t.ResolvedAt = (status is "Resolved" or "Closed") ? (t.ResolvedAt ?? DateTime.UtcNow) : (DateTime?)null;
        await db.SaveChangesAsync();
        return await ToDetailAsync(t);
    }

    // ---------------- assign / priority / category (staff) ----------------
    [HttpPut("{id:int}/assign")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<TicketDetailDto>> Assign(int id, AssignDto dto)
    {
        var t = await LoadAsync(id);
        if (t is null) return NotFound(new { message = "Ticket not found." });

        if (dto.AgentId.HasValue)
        {
            var ok = await db.Users.AnyAsync(u => u.Id == dto.AgentId && (u.Role == Roles.Agent || u.Role == Roles.Admin));
            if (!ok) return BadRequest(new { message = "Agent not found." });
        }
        t.AssignedToId = dto.AgentId;
        t.AssignedTo = null; // force reload of navigation
        t.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await ToDetailAsync((await LoadAsync(id))!);
    }

    [HttpPut("{id:int}/meta")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<TicketDetailDto>> UpdateMeta(int id, UpdateMetaDto dto)
    {
        var t = await LoadAsync(id);
        if (t is null) return NotFound(new { message = "Ticket not found." });

        if (dto.Priority is not null)
        {
            var p = TicketValues.Priorities.FirstOrDefault(x => x.Equals(dto.Priority, StringComparison.OrdinalIgnoreCase));
            if (p is null) return BadRequest(new { message = "Invalid priority." });
            t.Priority = p;
        }
        if (dto.Category is not null)
        {
            var c = TicketValues.Categories.FirstOrDefault(x => x.Equals(dto.Category, StringComparison.OrdinalIgnoreCase));
            if (c is null) return BadRequest(new { message = "Invalid category." });
            t.Category = c;
        }
        t.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return await ToDetailAsync(t);
    }

    // ---------------- comments ----------------
    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult<TicketDetailDto>> AddComment(int id, AddCommentDto dto)
    {
        var t = await LoadAsync(id);
        if (t is null) return NotFound(new { message = "Ticket not found." });

        var internalNote = IsStaff && dto.IsInternal;
        db.Comments.Add(new Comment
        {
            TicketId = id,
            AuthorId = CurrentUserId,
            Body = dto.Body.Trim(),
            IsInternal = internalNote
        });

        t.UpdatedAt = DateTime.UtcNow;
        if (IsStaff && !internalNote)
        {
            if (t.Status == "Open") t.Status = "InProgress";
            t.AssignedToId ??= CurrentUserId;
        }
        else if (!IsStaff && (t.Status is "Resolved" or "Closed"))
        {
            t.Status = "Open";            // customer replied on a finished ticket: reopen it
            t.ResolvedAt = null;
        }

        await db.SaveChangesAsync();
        return await ToDetailAsync((await LoadAsync(id))!);
    }

    // ---------------- AI actions (staff) ----------------
    [HttpPost("{id:int}/suggest-reply")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<SuggestReplyResult>> SuggestReply(int id)
    {
        var t = await LoadAsync(id);
        if (t is null) return NotFound(new { message = "Ticket not found." });

        var articles = await kb.SuggestAsync($"{t.Title} {t.Description}");
        var (reply, source) = await ai.SuggestReplyAsync(t, t.Comments, articles);

        t.AiSuggestedReply = reply;
        await db.SaveChangesAsync();
        return new SuggestReplyResult(reply, source);
    }

    [HttpPost("{id:int}/retriage")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<TicketDetailDto>> Retriage(int id)
    {
        var t = await LoadAsync(id);
        if (t is null) return NotFound(new { message = "Ticket not found." });

        var r = await ai.TriageAsync(t.Title, t.Description);
        t.Category = r.Category;
        t.Priority = r.Priority;
        t.Sentiment = r.Sentiment;
        t.AiSummary = r.Summary;
        t.AiSource = r.Source;
        t.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return await ToDetailAsync(t);
    }

    // ---------------- helpers ----------------
    private async Task<Ticket?> LoadAsync(int id)
    {
        var t = await db.Tickets
            .Include(x => x.CreatedBy)
            .Include(x => x.AssignedTo)
            .Include(x => x.Comments).ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (t is null) return null;
        if (!IsStaff && t.CreatedById != CurrentUserId) return null;   // customers only see their own tickets
        return t;
    }

    private async Task<TicketDetailDto> ToDetailAsync(Ticket t)
    {
        var staff = IsStaff;

        var comments = t.Comments
            .Where(c => staff || !c.IsInternal)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentDto(c.Id, c.AuthorId, c.Author?.FullName ?? "Unknown", c.Author?.Role ?? "", c.Body, c.IsInternal, c.CreatedAt))
            .ToList();

        var suggestions = new List<KbSuggestionDto>();
        if (staff)
        {
            var found = await kb.SuggestAsync($"{t.Title} {t.Description}");
            suggestions = found
                .Select(a => new KbSuggestionDto(a.Id, a.Title, a.Content.Length > 160 ? a.Content[..157] + "..." : a.Content))
                .ToList();
        }

        return new TicketDetailDto(
            t.Id, t.Title, t.Description, t.Status, t.Priority, t.Category, t.Sentiment,
            t.AiSummary, staff ? t.AiSuggestedReply : "", t.AiSource,
            t.CreatedById, t.CreatedBy?.FullName ?? "", t.AssignedToId, t.AssignedTo?.FullName,
            t.CreatedAt, t.UpdatedAt, t.ResolvedAt, comments, suggestions);
    }
}
