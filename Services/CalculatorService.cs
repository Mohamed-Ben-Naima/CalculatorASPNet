using System.Globalization;

namespace CalculatorASPNet.Services;

/// <summary>Outcome of an expression evaluation.</summary>
public sealed record CalculationResult(bool Succeeded, decimal Value, string Error = "", string? ErrorCode = null)
{
    public static CalculationResult Ok(decimal value) => new(true, value);
    public static CalculationResult Fail(string message, string code) => new(false, 0m, message, code);
}

/// <summary>
/// A dependency-free arithmetic expression engine supporting + - * / % ^ parentheses,
/// unary operators, functions (sqrt, abs, floor, ceil, round) and constants (pi, e).
/// </summary>
public sealed class CalculatorService
{
    public const string ErrorInvalid = "INVALID_EXPRESSION";
    public const string ErrorDivideByZero = "DIVISION_BY_ZERO";
    public const string ErrorOverflow = "OVERFLOW";

    private static readonly NumberFormatInfo Invariant = CultureInfo.InvariantCulture.NumberFormat;

    public CalculationResult Evaluate(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return CalculationResult.Fail("The expression is empty.", ErrorInvalid);
        }

        try
        {
            var parser = new Parser(Normalize(expression));
            if (!parser.TryParse(out var value))
            {
                return CalculationResult.Fail("The expression contains invalid characters or is malformed.", ErrorInvalid);
            }

            return CalculationResult.Ok(value);
        }
        catch (DivideByZeroException)
        {
            return CalculationResult.Fail("Division by zero is not allowed.", ErrorDivideByZero);
        }
        catch (OverflowException)
        {
            return CalculationResult.Fail("The result is outside the supported numeric range.", ErrorOverflow);
        }
    }

    /// <summary>Accepts friendly keyboard symbols: × ÷ − and both decimal separators.</summary>
    private static string Normalize(string input)
    {
        var sb = new System.Text.StringBuilder(input.Length);
        foreach (var ch in input)
        {
            switch (ch)
            {
                case '×': sb.Append('*'); break;
                case '÷': sb.Append('/'); break;
                case '−': sb.Append('-'); break;
                case '—': sb.Append('-'); break;
                case ',': sb.Append('.'); break;
                default: sb.Append(ch); break;
            }
        }

        return sb.ToString();
    }

    private sealed class Parser
    {
        private readonly string _text;
        private int _pos;

        public Parser(string text) => _text = text;

        public bool TryParse(out decimal value)
        {
            value = 0m;
            SkipWhitespace();
            if (!TryParseExpression(out value))
            {
                return false;
            }

            SkipWhitespace();
            return _pos >= _text.Length;
        }

        // expr    := term (('+' | '-') term)*
        private bool TryParseExpression(out decimal value)
        {
            if (!TryParseTerm(out value))
            {
                return false;
            }

            while (true)
            {
                SkipWhitespace();
                if (!TryConsume('+'))
                {
                    if (!TryConsume('-'))
                    {
                        return true;
                    }

                    if (!TryParseTerm(out var right))
                    {
                        return false;
                    }

                    value = checked(value - right);
                }
                else
                {
                    if (!TryParseTerm(out var right))
                    {
                        return false;
                    }

                    value = checked(value + right);
                }
            }
        }

        // term    := unary (('*' | '/' | '%') unary)*
        private bool TryParseTerm(out decimal value)
        {
            if (!TryParseUnary(out value))
            {
                return false;
            }

            while (true)
            {
                SkipWhitespace();
                if (TryConsume('*'))
                {
                    if (!TryParseUnary(out var right))
                    {
                        return false;
                    }

                    value = checked(value * right);
                }
                else if (TryConsume('/'))
                {
                    if (!TryParseUnary(out var right))
                    {
                        return false;
                    }

                    value = checked(value / right); // throws DivideByZeroException
                }
                else if (TryConsume('%'))
                {
                    if (!TryParseUnary(out var right))
                    {
                        return false;
                    }

                    value = checked(value % right);
                }
                else
                {
                    return true;
                }
            }
        }

        // power   := primary ('^' unary)?   (right-associative, binds tighter than unary minus)
        private bool TryParsePower(out decimal value)
        {
            if (!TryParsePrimary(out value))
            {
                return false;
            }

            SkipWhitespace();
            if (!TryConsume('^'))
            {
                return true;
            }

            if (!TryParseUnary(out var exponent))
            {
                return false;
            }

            if (value == 0m && exponent < 0m)
            {
                throw new DivideByZeroException();
            }

            value = checked((decimal)Math.Pow((double)value, (double)exponent));
            return true;
        }

        // unary   := ('+' | '-') unary | power
        private bool TryParseUnary(out decimal value)
        {
            SkipWhitespace();
            if (TryConsume('+'))
            {
                return TryParseUnary(out value);
            }

            if (TryConsume('-'))
            {
                if (!TryParseUnary(out value))
                {
                    return false;
                }

                value = checked(-value);
                return true;
            }

            return TryParsePower(out value);
        }

        // primary := number | '(' expr ')' | func '(' expr ')' | constant
        private bool TryParsePrimary(out decimal value)
        {
            SkipWhitespace();
            if (_pos >= _text.Length)
            {
                value = 0m;
                return false;
            }

            var ch = _text[_pos];
            if (char.IsDigit(ch) || ch == '.')
            {
                return TryParseNumber(out value);
            }

            if (ch == '(')
            {
                _pos++;
                if (!TryParseExpression(out value))
                {
                    return false;
                }

                SkipWhitespace();
                return TryConsume(')');
            }

            if (char.IsLetter(ch))
            {
                return TryParseIdentifier(out value);
            }

            value = 0m;
            return false;
        }

        private bool TryParseNumber(out decimal value)
        {
            var start = _pos;
            while (_pos < _text.Length && (char.IsDigit(_text[_pos]) || _text[_pos] == '.'))
            {
                _pos++;
            }

            var token = _text[start.._pos];
            value = 0m;
            return decimal.TryParse(token, NumberStyles.Number, Invariant, out value);
        }

        private bool TryParseIdentifier(out decimal value)
        {
            var start = _pos;
            while (_pos < _text.Length && char.IsLetter(_text[_pos]))
            {
                _pos++;
            }

            var name = _text[start.._pos].ToLowerInvariant();
            SkipWhitespace();

            switch (name)
            {
                case "pi":
                    value = (decimal)Math.PI;
                    return true;
                case "e":
                    value = (decimal)Math.E;
                    return true;
                case "sqrt":
                case "abs":
                case "floor":
                case "ceil":
                case "round":
                    if (!TryConsume('(') || !TryParseExpression(out var argument))
                    {
                        value = 0m;
                        return false;
                    }

                    if (!TryConsume(')'))
                    {
                        value = 0m;
                        return false;
                    }

                    value = name switch
                    {
                        "sqrt" => checked((decimal)Math.Sqrt((double)argument)),
                        "abs" => Math.Abs(argument),
                        "floor" => Math.Floor(argument),
                        "ceil" => Math.Ceiling(argument),
                        "round" => Math.Round(argument, 0, MidpointRounding.AwayFromZero),
                        _ => argument
                    };
                    return true;
                default:
                    value = 0m;
                    return false;
            }
        }

        private void SkipWhitespace()
        {
            while (_pos < _text.Length && char.IsWhiteSpace(_text[_pos]))
            {
                _pos++;
            }
        }

        private bool TryConsume(char expected)
        {
            if (_pos < _text.Length && _text[_pos] == expected)
            {
                _pos++;
                return true;
            }

            return false;
        }
    }
}