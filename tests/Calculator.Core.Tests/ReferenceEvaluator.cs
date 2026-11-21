using System.Globalization;

namespace Calculator.Core.Tests;

/// <summary>
/// Test-only second implementation (N-version programming): the engine parses into a tree, this
/// evaluator converts infix to Reverse Polish Notation with Dijkstra's shunting-yard algorithm and
/// runs it on an explicit stack. Two independent algorithms agreeing on thousands of inputs is
/// strong evidence that both are right.
/// Supports only what <see cref="ExpressionGenerator"/> emits: decimal literals, spaces, + - * /,
/// and unary minus written as a '-' directly attached to a number ("-5").
/// Division by zero throws <see cref="DivideByZeroException"/>.
/// </summary>
internal static class ReferenceEvaluator
{
    public static decimal Evaluate(string text) => EvaluateRpn(ToRpn(Tokenize(text)));

    private static List<string> Tokenize(string text)
    {
        var tokens = new List<string>();
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (c == ' ') { i++; continue; }

            // A '-' is unary (part of the number) at the start or right after another operator.
            var unaryMinus = c == '-' && (tokens.Count == 0 || IsOperator(tokens[^1]));
            if (char.IsDigit(c) || c == '.' || unaryMinus)
            {
                var start = i++;
                while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '.')) i++;
                tokens.Add(text[start..i]);
            }
            else
            {
                tokens.Add(c.ToString());
                i++;
            }
        }
        return tokens;
    }

    // Shunting-yard: operands go straight to the output; an operator first pops every stacked
    // operator of greater or equal precedence (>= gives left associativity), then is pushed.
    private static List<string> ToRpn(List<string> tokens)
    {
        var output = new List<string>();
        var operators = new Stack<string>();
        foreach (var token in tokens)
        {
            if (!IsOperator(token)) { output.Add(token); continue; }
            while (operators.Count > 0 && Precedence(operators.Peek()) >= Precedence(token))
                output.Add(operators.Pop());
            operators.Push(token);
        }
        while (operators.Count > 0) output.Add(operators.Pop());
        return output;
    }

    // RPN: push operands; an operator pops right then left operand and pushes the result.
    private static decimal EvaluateRpn(List<string> rpn)
    {
        var stack = new Stack<decimal>();
        foreach (var token in rpn)
        {
            if (!IsOperator(token))
            {
                stack.Push(decimal.Parse(token, NumberStyles.Number, CultureInfo.InvariantCulture));
                continue;
            }
            var right = stack.Pop();
            var left = stack.Pop();
            stack.Push(token switch
            {
                "+" => left + right,
                "-" => left - right,
                "*" => left * right,
                _ => right == 0m ? throw new DivideByZeroException() : left / right,
            });
        }
        return stack.Single();
    }

    private static bool IsOperator(string token) => token is "+" or "-" or "*" or "/";

    private static int Precedence(string op) => op is "*" or "/" ? 2 : 1;
}
