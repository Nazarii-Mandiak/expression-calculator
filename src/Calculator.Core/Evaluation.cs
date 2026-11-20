namespace Calculator.Core;

/// <summary>The result of evaluating an <see cref="Expression"/>.</summary>
/// <param name="Value">The exact <see cref="decimal"/> result (format it with <see cref="NumberFormatter"/>).</param>
/// <param name="Steps">Each binary operation in the order it was performed.</param>
public sealed record Evaluation(decimal Value, IReadOnlyList<EvaluationStep> Steps);

/// <summary>One binary operation performed during evaluation, e.g. <c>2 * 6 = 12</c>.</summary>
/// <param name="Left">Left operand.</param>
/// <param name="Operator">Operator applied.</param>
/// <param name="Right">Right operand.</param>
/// <param name="Result">Result of the operation.</param>
public sealed record EvaluationStep(decimal Left, BinaryOperator Operator, decimal Right, decimal Result);

/// <summary>Supported binary operators.</summary>
public enum BinaryOperator
{
    /// <summary>Addition (<c>+</c>).</summary>
    Add,

    /// <summary>Subtraction (<c>-</c>).</summary>
    Subtract,

    /// <summary>Multiplication (<c>*</c>).</summary>
    Multiply,

    /// <summary>Division (<c>/</c>).</summary>
    Divide,
}
