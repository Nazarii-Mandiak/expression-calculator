namespace Calculator.Core;

/// <summary>Presentation helpers for <see cref="BinaryOperator"/>, shared by every host.</summary>
public static class BinaryOperatorExtensions
{
    /// <summary>Returns the ASCII symbol of the operator: <c>+</c>, <c>-</c>, <c>*</c> or <c>/</c>.</summary>
    /// <param name="op">The operator.</param>
    /// <returns>The symbol.</returns>
    public static char ToSymbol(this BinaryOperator op) => op switch
    {
        BinaryOperator.Add => '+',
        BinaryOperator.Subtract => '-',
        BinaryOperator.Multiply => '*',
        BinaryOperator.Divide => '/',
        _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Undefined binary operator."),
    };
}
