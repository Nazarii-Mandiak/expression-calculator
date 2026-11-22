# ADR 0002: Parse completely, then evaluate, over a flat syntax tree

- **Status:** Accepted
- **Date:** 2026-11-21

## Context

Input is untrusted. It can contain 50,000 operands, 10,000 unary signs or control characters. The
engine must report the leftmost problem with its exact position, must never evaluate half of an
invalid expression, and must never crash. In .NET, a stack overflow cannot be caught, so the design
must make one impossible.

## Decision

- Processing is a pipeline: `Lexer`, then `Parser`, then a tree of `Node` objects, then `Evaluator`.
- The lexer is lazy: it produces one token at a time, on demand. The parser therefore stops at the
  leftmost error, whether that error is in a token or in the grammar.
- The parser is a recursive-descent parser that mirrors the grammar in `docs/SPEC.md`. Each
  repetition in the grammar (`{ op term }`) is a `while` loop, so the call depth stays the same
  however long the input is. Unary signs are folded into the number they precede.
- The abstract syntax tree is flat and has at most three levels. A `ChainNode` holds all operands of
  one priority level, each `ChainLink` keeps its operator's position, and a `NumberNode` is a leaf.
  A conventional binary tree would be 50,000 levels deep for 50,000 operands.
- `Parse` finishes before anything is calculated, so a syntax error always wins over a calculation
  error. `Evaluate` can only fail with `DivisionByZero` or `Overflow`. It records each step in order,
  which powers `--explain` and the REST `steps` field.

## Consequences

### Benefits

- No input length can overflow the stack.
- Validation without evaluation is a real operation, offered as `/validate`.
- Operator priority and left-to-right order follow from the structure of the tree.

### Trade-offs

- The constant depth relies on the grammar having no parentheses. Adding them would make nesting
  recursive, and would require an explicit depth limit that fails with a located
  `CalculationException`.

## Alternatives considered

| Option | Why not |
| ------ | ------- |
| Shunting-yard algorithm | Reports errors late; used instead as the independent reference in the tests |
| Pratt parsing into a binary tree | Tree depth grows with the input |
| Evaluating while parsing | A syntax error late in the input would come after a calculation error |
| Tokenising everything first | Reports a later token error before an earlier grammar error |
