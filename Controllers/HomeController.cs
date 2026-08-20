using CalculatorASPNet.Data;
using CalculatorASPNet.Models;
using CalculatorASPNet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;

namespace CalculatorASPNet.Controllers;

public class HomeController : Controller
{
    private const int HistoryPageSize = 20;
    private static readonly NumberFormatInfo Invariant = CultureInfo.InvariantCulture.NumberFormat;

    private readonly CalculatorDbContext _db;
    private readonly CalculatorService _calculator;
    private readonly ILogger<HomeController> _logger;

    public HomeController(CalculatorDbContext db, CalculatorService calculator, ILogger<HomeController> logger)
    {
        _db = db;
        _calculator = calculator;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new CalculatorPageViewModel
        {
            History = await GetHistoryAsync(cancellationToken)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Evaluate([FromForm] string expression, CancellationToken cancellationToken)
    {
        var result = _calculator.Evaluate(expression);

        if (result.Succeeded)
        {
            await SaveCalculationAsync(expression, result.Value, cancellationToken);
        }
        else
        {
            _logger.LogWarning("Evaluation failed for '{Expression}': {Error}", expression, result.Error);
        }

        return Json(new
        {
            success = result.Succeeded,
            result = result.Succeeded ? Format(result.Value) : null,
            error = result.Error,
            errorCode = result.ErrorCode,
            history = await ToDtosAsync(GetHistoryAsync(cancellationToken))
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteHistory(CancellationToken cancellationToken)
    {
        _db.Calculations.RemoveRange(_db.Calculations);
        await _db.SaveChangesAsync(cancellationToken);

        return Json(new { history = await ToDtosAsync(GetHistoryAsync(cancellationToken)) });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private async Task SaveCalculationAsync(string expression, decimal value, CancellationToken cancellationToken)
    {
        _db.Calculations.Add(new CalculationHistory
        {
            Expression = expression,
            Result = Format(value),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<CalculationHistory>> GetHistoryAsync(CancellationToken cancellationToken)
        => await _db.Calculations
            .OrderByDescending(c => c.CreatedAt)
            .Take(HistoryPageSize)
            .ToListAsync(cancellationToken);

    private static async Task<object[]> ToDtosAsync(Task<IReadOnlyList<CalculationHistory>> task)
    {
        var items = await task;
        return items.Select(c => new
        {
            id = c.Id,
            expression = c.Expression,
            result = c.Result,
            createdAt = c.CreatedAt
        }).ToArray();
    }

    private static string Format(decimal value) => value.ToString("0.#############################", Invariant);
}