namespace CalculatorASPNet.Models;

/// <summary>A persisted calculation, stored after every successful evaluation.</summary>
public class CalculationHistory
{
    public int Id { get; set; }

    public string Expression { get; set; } = string.Empty;

    public string Result { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}