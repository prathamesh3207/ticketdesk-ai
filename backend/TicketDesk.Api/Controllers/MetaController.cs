using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketDesk.Api.Data;
using TicketDesk.Api.Domain;
using TicketDesk.Api.Dtos;
using TicketDesk.Api.Services;

namespace TicketDesk.Api.Controllers;

[ApiController]
public class MetaController(IAiService ai, AppDbContext db) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("api/meta")]
    public MetaDto Get() => new(ai.Mode, "1.0");

    /// <summary>Agents and admins that tickets can be assigned to.</summary>
    [Authorize(Roles = "Admin,Agent")]
    [HttpGet("api/users/staff")]
    public async Task<List<UserDto>> Staff() =>
        await db.Users.AsNoTracking()
            .Where(u => u.Role == Roles.Agent || u.Role == Roles.Admin)
            .OrderBy(u => u.FullName)
            .Select(u => new UserDto(u.Id, u.FullName, u.Email, u.Role))
            .ToListAsync();
}
