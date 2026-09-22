using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeVault.Server.Data;
using SafeVault.Server.Models;
using SafeVault.Shared.DTOs.Vault;

namespace SafeVault.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "User,Admin")]
public class VaultController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public VaultController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpPost]
    public async Task<ActionResult<VaultRecordResponse>> Create(
        CreateVaultRecordRequest request)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized();
        }

        var now = DateTime.UtcNow;

        var record = new VaultRecord
        {
            OwnerId = user.Id,
            Title = request.Title,
            Institution = request.Institution,
            AccountType = request.AccountType,
            LastFourDigits = request.LastFourDigits,
            Notes = request.Notes,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.VaultRecords.Add(record);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = record.Id },
            ToResponse(record));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VaultRecordResponse>>> GetAll()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized();
        }

        var records = await _context.VaultRecords
            .AsNoTracking()
            .Where(record => record.OwnerId == user.Id)
            .OrderByDescending(record => record.UpdatedAt)
            .Select(record => ToResponse(record))
            .ToListAsync();

        return Ok(records);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VaultRecordResponse>> GetById(int id)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized();
        }

        var record = await _context.VaultRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(record =>
                record.Id == id &&
                record.OwnerId == user.Id);

        if (record is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(record));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateVaultRecordRequest request)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized();
        }

        var record = await _context.VaultRecords
            .FirstOrDefaultAsync(record =>
                record.Id == id &&
                record.OwnerId == user.Id);

        if (record is null)
        {
            return NotFound();
        }

        record.Title = request.Title;
        record.Institution = request.Institution;
        record.AccountType = request.AccountType;
        record.LastFourDigits = request.LastFourDigits;
        record.Notes = request.Notes;
        record.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized();
        }

        var record = await _context.VaultRecords
            .FirstOrDefaultAsync(record =>
                record.Id == id &&
                record.OwnerId == user.Id);

        if (record is null)
        {
            return NotFound();
        }

        _context.VaultRecords.Remove(record);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static VaultRecordResponse ToResponse(VaultRecord record)
    {
        return new VaultRecordResponse
        {
            Id = record.Id,
            Title = record.Title,
            Institution = record.Institution,
            AccountType = record.AccountType,
            LastFourDigits = record.LastFourDigits,
            Notes = record.Notes,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };
    }
}