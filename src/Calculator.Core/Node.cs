namespace Calculator.Core;

/// <summary>Syntax tree of a validated expression: operator chains of numbers, at most three levels deep.</summary>
internal abstract record Node;

/// <summary>A literal with its unary signs already folded in.</summary>
internal sealed record NumberNode(decimal Value) : Node;

/// <summary>Operands joined by operators of one precedence level, applied left to right.</summary>
internal sealed record ChainNode(Node First, IReadOnlyList<ChainLink> Rest) : Node;

/// <summary>An operator (with its position in the text) and the operand to its right.</summary>
internal sealed record ChainLink(BinaryOperator Operator, int OperatorPosition, Node Operand);
