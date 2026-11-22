# ADR 0004: One located exception type, shown per host

- **Status:** Accepted
- **Date:** 2026-11-21

## Context

People type expressions by hand, so errors are routine. An error must say what is wrong and where,
in a form that a terminal user, a REST client and an AI assistant can each act on. Error messages
also echo untrusted input, which makes them part of the attack surface.

## Decision

- **One exception type.** `CalculationException` carries a `Code` (an `ErrorCode`: `EmptyExpression`,
  `UnexpectedCharacter`, `InvalidNumber`, `MissingOperand`, `MissingOperator`, `DivisionByZero` or
  `Overflow`), a zero-based `Position` and a `Length` of at least 1.
- There are no magic return values. Codes that only a host can produce (`InputTooLong`,
  `MissingExpression`, `Uninterpretable`) live in the hosts.
- Clients act on the `Code`; the English message is for people. The leftmost error wins (see
  ADR 0002).
- **Console.** The format is `error[Code]: message`, then the input, then `^` repeated `Length`
  times, all on stderr, with exit codes 0, 1 and 2. The echoed input keeps one character per input
  character (a tab becomes a space, a control character becomes `?`). The caret therefore stays
  aligned, and a piped file cannot inject terminal escape sequences.
- **REST.** Errors use RFC 9457 `application/problem+json`. A 422 adds `errorCode`, `position` and
  `length`. `/validate` answers 200 with `valid: false`.
- **MCP.** Errors are tool results marked `isError` that name the code, the `index` and the `length`.
- **Safe messages.** A message quotes at most 20 characters of input, shows control characters as
  `U+XXXX`, and never contains configuration keys.

## Consequences

### Benefits

- Each host needs one `catch`, and the engine cannot produce a failure without a location.

### Trade-offs

- Exceptions cost more than return codes. This is acceptable because hosts cap the input size.
- Positions are UTF-16 indexes. They are exact for everything before the error, which is all a caret
  needs.
- Messages are in English only.

## Alternatives considered

| Option | Why not |
| ------ | ------- |
| A `Result<T, Error>` type | It would spread into every host's code |
| One exception type per error | More types to catch and map, for no extra information |
| Reporting all errors at once | Later errors are often consequences of the first one |
| HTTP 400 for invalid expressions | 400 means a malformed request; 422 means a well-formed request whose content is invalid |
