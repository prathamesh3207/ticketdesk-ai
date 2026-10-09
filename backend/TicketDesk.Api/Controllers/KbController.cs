using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketDesk.Api.Data;
using TicketDesk.Api.Domain;
using TicketDesk.Api.Dtos;

namespace TicketDesk.Api.Controllers;

[ApiController]
[Route("api/kb")]
[Authorize]
public class KbController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<List<KbArticleDto>> List([FromQuery] string? q)
    {
        var query = db.KbArticles.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(a => a.Title.Contains(term) || a.Content.Contains(term) || a.Tags.Contains(term));
        }
        var items = await query.OrderBy(a => a.Title).ToListAsync();
        return items.Select(a => a.ToDto()).ToList();
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<KbArticleDto>> Create(KbUpsertDto dto)
    {
        var a = new KbArticle { Title = dto.Title.Trim(), Content = dto.Content.Trim(), Tags = dto.Tags.Trim() };
        db.KbArticles.Add(a);
        await db.SaveChangesAsync();
        return a.ToDto();
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<KbArticleDto>> Update(int id, KbUpsertDto dto)
    {
        var a = await db.KbArticles.FindAsync(id);
        if (a is null) return NotFound(new { message = "Article not found." });
        a.Title = dto.Title.Trim();
        a.Content = dto.Content.Trim();
        a.Tags = dto.Tags.Trim();
        await db.SaveChangesAsync();
        return a.ToDto();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var a = await db.KbArticles.FindAsync(id);
        if (a is null) return NotFound(new { message = "Article not found." });
        db.KbArticles.Remove(a);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
