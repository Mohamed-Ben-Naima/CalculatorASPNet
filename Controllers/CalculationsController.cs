using CalculatorASPNet.Contracts;
using CalculatorASPNet.Data;
using CalculatorASPNet.Models;
using CalculatorASPNet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CalculatorASPNet.Controllers;

[ApiController]
[Route("api/calculations")]
public class CalculationsController : ControllerBase
{
    private const int DefaultLimit = 50;
    private const int MaxLimit = 200;

    private readonly CalculatorDbContext _db;
    private readonly CalculatorService _calculator;

    public CalculationsController(CalculatorDbContext db, CalculatorService calculator)
    {
        _db = db;
        _calculator = calculator;
    }

    /// <summary>Evaluates an arithmetic expression and stores it in the history.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CalculationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CalculationResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Evaluate([FromBody] CalculationRequest request, CancellationToken cancellationToken)
    {
        var result = _calculator.Evaluate(request?.Expression);

        if (!result.Succeeded)
        {
            return BadRequest(new CalculationResponseDto
            {
                Expression = request?.Expression,
                Succeeded = false,
                Error = result.Error,
                ErrorCode = result.ErrorCode
            });
        }

        var history = new CalculationHistory
        {
            Expression = request!.Expression!,
            Result = result.Value.ToString("0.#############################"),
            CreatedAt = DateTime.UtcNow
        };

        _db.Calculations.Add(history);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new CalculationResponseDto
        {
            Expression = history.Expression,
            Result = history.Result,
            Succeeded = true
        });
    }

    /// <summary>Returns the most recent calculations (latest first).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<HistoryItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory([FromQuery] int limit = DefaultLimit, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, MaxLimit);
        var items = await _db.Calculations
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .Select(c => new HistoryItemDto
            {
                Id = c.Id,
                Expression = c.Expression,
                Result = c.Result,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    /// <summary>Returns a single stored calculation.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(HistoryItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _db.Calculations
            .Where(c => c.Id == id)
            .Select(c => new HistoryItemDto
            {
                Id = c.Id,
                Expression = c.Expression,
                Result = c.Result,
                CreatedAt = c.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Deletes every stored calculation.</summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAll(CancellationToken cancellationToken)
    {
        _db.Calculations.RemoveRange(_db.Calculations);
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>Deletes a single stored calculation.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var item = await _db.Calculations.FindAsync([id], cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        _db.Calculations.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}