using Microsoft.EntityFrameworkCore;

namespace CalculatorASPNet.Data;

public class CalculatorDbContext : DbContext
{
    public CalculatorDbContext(DbContextOptions<CalculatorDbContext> options)
        : base(options)
    {
    }

    public DbSet<Models.CalculationHistory> Calculations => Set<Models.CalculationHistory>();
}