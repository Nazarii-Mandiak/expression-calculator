# ADR 0003: Decimal arithmetic and a display rule

- **Status:** Accepted
- **Date:** 2026-11-21

## Context

Users type decimal numbers and expect `0.1 + 0.2` to print `0.3` on every machine. Three questions
form one decision, because every host must show identical results: which numeric type to use, what
to do with digits it cannot hold, and how to format the result.

## Decision

- **Type.** `System.Decimal` is used for numbers in the input, for intermediate values and for
  results. It is exact for decimal input, holds 28-29 significant digits and is part of .NET.
- **Exact input.** A number in the input is either used exactly or rejected. If `decimal` would round
  it, the error is `InvalidNumber`. An integer beyond the range is `Overflow`.
- **Rounded results.** `decimal` rounds results, and this is documented. `1/3` carries 28 threes, and
  a result below 1e-28 becomes `0`.
- **Display.** `NumberFormatter.Format` shows 25 significant digits, rounded half away from zero,
  with no exponent, no grouping and no trailing zeros. The extra internal digits act as guard digits,
  so `1/3*3` shows `1`. A non-zero value is never shown as `0`.
- **Culture.** `CultureInfo.InvariantCulture` is used everywhere; tests run under `uk-UA` and
  `de-DE`. REST and MCP return results as strings, so JavaScript clients do not round them through
  doubles.

## Consequences

### Benefits

- Correct for everyday decimal arithmetic, fast, and identical across cultures and hosts.

### Trade-offs

- Precision is bounded. Some quotients are inexact (`1/3`), tiny products become `0`, and the range
  ends at about 7.9e28, beyond which a located `Overflow` is reported.
- Moving to exact fractions later would be a breaking change, because `Evaluation.Value` is a
  `decimal`.

## Alternatives considered

| Option | Why not |
| ------ | ------- |
| `double` | `0.1 + 0.2` prints `0.30000000000000004` |
| Exact fractions with `BigInteger` | Values can grow without bound on hostile input, and the engine would have to own all the arithmetic |
| Big-decimal packages | A third-party dependency in the engine (see ADR 0005) |
| Rejecting every inexact result | `1/3` would be unusable |
