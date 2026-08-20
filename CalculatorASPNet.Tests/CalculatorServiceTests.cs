using CalculatorASPNet.Services;
using Xunit;

namespace CalculatorASPNet.Tests;

public class CalculatorServiceTests
{
    private readonly CalculatorService _calculator = new();

    [Theory]
    [InlineData("2+3", 5)]
    [InlineData("2-3", -1)]
    [InlineData("2*3", 6)]
    [InlineData("10/4", 2.5)]
    [InlineData("10%3", 1)]
    [InlineData("2^3", 8)]
    public void BasicArithmetic_Evaluates(string expression, decimal expected)
    {
        var result = _calculator.Evaluate(expression);

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("2+3*4", 14)]
    [InlineData("(2+3)*4", 20)]
    [InlineData("2*3+4*5", 26)]
    [InlineData("100/5/2", 10)]
    [InlineData("10-2-3", 5)]
    public void OperatorPrecedence_IsRespected(string expression, decimal expected)
    {
        var result = _calculator.Evaluate(expression);

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("-5+3", -2)]
    [InlineData("--5", 5)]
    [InlineData("2*-3", -6)]
    [InlineData("-(2+3)", -5)]
    public void UnaryMinus_IsHandled(string expression, decimal expected)
    {
        var result = _calculator.Evaluate(expression);

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("-2^2", -4)]          // -(2^2)
    [InlineData("2^-2", 0.25)]        // 2^(-2)
    [InlineData("2^3^2", 512)]        // right-associative: 2^(3^2)
    [InlineData("2^0", 1)]
    public void Exponentiation_IsRightAssociativeAndBindsTightly(string expression, decimal expected)
    {
        var result = _calculator.Evaluate(expression);

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("3,5+1,5", 5)]
    [InlineData("6×3", 18)]
    [InlineData("10÷4", 2.5)]
    [InlineData("7−2", 5)]
    [InlineData("  5  +  5 ", 10)]
    public void FriendlySymbolsAndWhitespace_AreAccepted(string expression, decimal expected)
    {
        var result = _calculator.Evaluate(expression);

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Sqrt_OfPerfectSquare_IsExact()
    {
        Assert.True(_calculator.Evaluate("sqrt(16)").Succeeded);
        Assert.Equal(4, _calculator.Evaluate("sqrt(16)").Value);
    }

    [Fact]
    public void Abs_ReturnsMagnitude()
    {
        Assert.Equal(7, _calculator.Evaluate("abs(-7)").Value);
        Assert.Equal(7, _calculator.Evaluate("abs(7)").Value);
    }

    [Theory]
    [InlineData("floor(2.7)", 2)]
    [InlineData("ceil(2.1)", 3)]
    [InlineData("round(2.5)", 3)]
    [InlineData("round(2.4)", 2)]
    public void FloorCeilRound_Work(string expression, decimal expected)
    {
        var result = _calculator.Evaluate(expression);

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void PiAndE_Constants_Resolve()
    {
        Assert.Equal((decimal)Math.PI, _calculator.Evaluate("pi").Value);
        Assert.Equal((decimal)Math.E, _calculator.Evaluate("e").Value);
        Assert.Equal((decimal)Math.PI * 2m, _calculator.Evaluate("2*pi").Value);
    }

    [Fact]
    public void NestedParentheses_AndFunctions_Work()
    {
        var result = _calculator.Evaluate("sqrt((3+1)*(6-2))");

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(4, result.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("abc")]
    [InlineData("2+")]
    [InlineData("*3")]
    [InlineData("(2+3")]
    [InlineData("2+3)")]
    [InlineData("5+5=")]
    public void MalformedExpressions_ReturnInvalid(string? expression)
    {
        var result = _calculator.Evaluate(expression);

        Assert.False(result.Succeeded);
        Assert.Equal(CalculatorService.ErrorInvalid, result.ErrorCode);
    }

    [Fact]
    public void DivisionByZero_ReturnsSpecificError()
    {
        var result = _calculator.Evaluate("1/0");

        Assert.False(result.Succeeded);
        Assert.Equal(CalculatorService.ErrorDivideByZero, result.ErrorCode);
    }

    [Fact]
    public void NegativeExponentOfZero_IsDivisionByZero()
    {
        var result = _calculator.Evaluate("0^-1");

        Assert.False(result.Succeeded);
        Assert.Equal(CalculatorService.ErrorDivideByZero, result.ErrorCode);
    }

    [Fact]
    public void SqrtOfNegative_ReturnsInvalid()
    {
        var result = _calculator.Evaluate("sqrt(-1)");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Overflow_ReturnsSpecificError()
    {
        var result = _calculator.Evaluate("9^999999");

        Assert.False(result.Succeeded);
        Assert.Equal(CalculatorService.ErrorOverflow, result.ErrorCode);
    }

    [Fact]
    public void LargeNumbers_AreHandled()
    {
        var result = _calculator.Evaluate("999999999999999 * 2");

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(1999999999999998m, result.Value);
    }
}