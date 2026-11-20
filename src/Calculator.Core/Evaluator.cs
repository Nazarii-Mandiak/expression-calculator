using System.Diagnostics;

namespace Calculator.Core;

/// <summary>Walks a validated tree, performing each binary operation and recording it as a step.</summary>
internal static class Evaluator
{
    public static Evaluation Evaluate(Node root)
    {
        var steps = new List<EvaluationStep>();
        var value = Evaluate(root, steps);
        return new Evaluation(value, steps.ToArray());
    }

    private static decimal Evaluate(Node node, List<EvaluationStep> steps) => node switch
    {
        NumberNode number => number.Value,
        ChainNode chain => EvaluateChain(chain, steps),
        _ => throw new UnreachableException(),
    };

    private static decimal EvaluateChain(ChainNode chain, List<EvaluationStep> steps)
    {
        var result = Evaluate(chain.First, steps);
        foreach (var link in chain.Rest)
        {
            var right = Evaluate(link.Operand, steps);
            var next = Apply(result, link, right);
            steps.Add(new EvaluationStep(result, link.Operator, right, next));
            result = next;
        }

        return result;
    }

    private static decimal Apply(decimal left, ChainLink link, decimal right)
    {
        // Checked first because decimal would throw DivideByZeroException, which the overflow handler does not map.
        if (link.Operator == BinaryOperator.Divide && right == 0m)
        {
            throw new CalculationException(ErrorCode.DivisionByZero, "division by zero", link.OperatorPosition, 1);
        }

        try
        {
            return link.Operator switch
            {
                BinaryOperator.Add => left + right,
                BinaryOperator.Subtract => left - right,
                BinaryOperator.Multiply => left * right,
                BinaryOperator.Divide => left / right,
                _ => throw new UnreachableException(),
            };
        }
        catch (OverflowException)
        {
            throw new CalculationException(
                ErrorCode.Overflow, "the result is too large to calculate with", link.OperatorPosition, 1);
        }
    }
}
