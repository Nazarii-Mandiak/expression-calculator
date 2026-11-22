using System.ComponentModel;

using Calculator.Core;

using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Calculator.Api;

/// <summary>
/// MCP tools exposing the calculation engine to LLM hosts (Claude Code, Claude Desktop, IDE agents).
/// The host model translates the user's words into an expression; the engine does the arithmetic.
/// </summary>
[McpServerToolType]
public sealed class CalculatorTools
{
    /// <summary>Evaluates an arithmetic expression with standard precedence.</summary>
    /// <param name="expression">Expression using numbers and + - * / only, e.g. <c>10 - 2 * 6 / 4</c>.</param>
    /// <returns>The canonical expression, the exact result as a string, and every step.</returns>
    [McpServerTool(Name = "evaluate_expression", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Evaluates an arithmetic expression with 28-significant-digit decimal precision. Translate the user's question into an expression using only numbers and + - * / (no parentheses, no words); '.' decimals and a leading '-' are allowed. Then call this tool. Never compute the answer yourself; report the returned result. If the question needs parentheses, powers or percentages, tell the user it is unsupported; do not rearrange it or compute parts yourself.")]
    public static EvaluationResponse EvaluateExpression(
        [Description("Arithmetic expression, e.g. \"10 - 2 * 6 / 4\".")] string expression)
    {
        RejectInvalidInput(expression);
        try
        {
            return ResponseMapper.Evaluate(expression, explain: true);
        }
        catch (CalculationException ex)
        {
            // The SDK reports a thrown McpException's message as the tool error text, so code and location go in it.
            throw new McpException($"{ex.Code} at index {ex.Position} (length {ex.Length}, zero-based): {ex.Message}", ex);
        }
    }

    /// <summary>Checks whether an expression is valid without evaluating it.</summary>
    /// <param name="expression">Expression to validate.</param>
    /// <returns>Whether it is valid, its canonical form, or the located error.</returns>
    [McpServerTool(Name = "validate_expression", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Checks whether an arithmetic expression is valid without evaluating it. Returns the canonical form or the error with its position.")]
    public static ValidationResponse ValidateExpression(
        [Description("Arithmetic expression to check.")] string expression)
    {
        RejectInvalidInput(expression);
        return ResponseMapper.Validate(expression);
    }

    private static void RejectInvalidInput(string? expression)
    {
        if (expression is null)
        {
            throw new McpException("MissingExpression: the 'expression' argument is required.");
        }

        if (expression.Length > ResponseMapper.MaxExpressionLength)
        {
            throw new McpException($"InputTooLong at index {ResponseMapper.MaxExpressionLength} (length {expression.Length - ResponseMapper.MaxExpressionLength}, zero-based): {ResponseMapper.TooLongMessage}");
        }
    }
}
