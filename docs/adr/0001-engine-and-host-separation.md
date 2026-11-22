# ADR 0001: Pure engine, thin hosts

- **Status:** Accepted
- **Date:** 2026-11-21

## Context

The same arithmetic must behave identically in a console app, a REST endpoint and an MCP tool. If
parsing, evaluation or formatting leaked into one of these hosts, the three would slowly drift
apart, for example in rounding or in error positions, and each would need its own tests.

## Decision

- `Calculator.Cli` and `Calculator.Api` depend on `Calculator.Core`, and Core depends on nothing. It
  has no I/O, no `Console`, no packages and no mutable static state, and it is marked
  `IsAotCompatible`.
- Core's public surface is small: `Expression.Parse` and `Evaluate`, `Evaluation`,
  `NumberFormatter.Format`, `NaturalLanguage.TryTranslate`, and `CalculationException` with
  `ErrorCode`. The lexer, parser and evaluator are `internal`. An `Expression` is immutable and
  thread-safe.
- Hosts only adapt the engine. They handle presentation, input limits (1,000 characters and 16 KB
  bodies in the API, 10,000-character lines in the console) and transport errors.
- `CliApp` receives its input and output streams as arguments. In the API, `ResponseMapper` is the
  only place that calls the engine.
- There is no `ICalculator` interface. There is one implementation, and the tests use the real
  engine. The only real seam is the language model, behind `IChatClient`.

## Consequences

### Benefits

- A fix to rounding or parsing lands once and applies to every host.
- Core can be tested without a console, a web host or a clock.
- The build enforces the separation: Core has no reference that could pull in I/O, so it does not
  depend on reviewers noticing.

### Trade-offs

- Presentation is deliberately different per host: carets in the console, Problem Details in REST.
  A change to the meaning of errors therefore touches each renderer.

## Alternatives considered

| Option | Why not |
| ------ | ------- |
| One project with folders | Nothing would stop I/O from leaking into the calculation code |
| `ICalculator` plus dependency injection | An interface with a single implementation adds indirection without benefit |
| The API calling the console app as a process | Slow and fragile, and errors would have to be parsed back from text |
