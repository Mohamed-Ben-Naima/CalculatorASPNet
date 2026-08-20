using CalculatorASPNet.Models;

namespace CalculatorASPNet.Contracts;

/// <summary>Body of a calculation request.</summary>
public sealed class CalculationRequest
{
    public string? Expression { get; set; }
}

/// <summary>Result of evaluating an expression.</summary>
public sealed class CalculationResponseDto
{
    public string? Expression { get; init; }
    public string? Result { get; init; }
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
    public string? ErrorCode { get; init; }
}

/// <summary>Serializable history entry.</summary>
public sealed class HistoryItemDto
{
    public int Id { get; init; }
    public string Expression { get; init; } = string.Empty;
    public string Result { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}