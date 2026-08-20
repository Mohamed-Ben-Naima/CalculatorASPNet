using CalculatorASPNet.Models;

namespace CalculatorASPNet.Models;

public sealed class CalculatorPageViewModel
{
    public IReadOnlyList<CalculationHistory> History { get; set; } = Array.Empty<CalculationHistory>();

    public string Expression { get; set; } = string.Empty;

    public string Result { get; set; } = string.Empty;

    public string? Error { get; set; }
}