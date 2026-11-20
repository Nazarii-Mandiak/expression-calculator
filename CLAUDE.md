# expression-calculator — instructions for Claude Code

A precise arithmetic expression calculator in .NET 10: a pure engine (`Calculator.Core`), a console
host (`Calculator.Cli`, binary `calc`) and a service PoC (`Calculator.Api`: REST + MCP).
Requirements and acceptance criteria live in `docs/SPEC.md`; decisions in `docs/adr/`.

## Commands
- Quality gate: `scripts/verify.sh --fast` (build + tests) · `scripts/verify.sh --full` (CI gate)
- Run CLI: `dotnet run --project src/Calculator.Cli -- "10 - 2 * 6 / 4"`
- Run API: `dotnet run --project src/Calculator.Api` (http://localhost:5080)

## Architecture rules
- `Calculator.Core` is pure: no I/O, no `Console`, no third-party packages, no static mutable state,
  `CultureInfo.InvariantCulture` for every parse/format. It must stay AOT/trim compatible.
- The whole input is parsed before anything is evaluated. Errors are `CalculationException`
  (`ErrorCode`, `Position`, `Length`) — never return magic values.
- Hosts (CLI, API) are thin adapters: argument/HTTP handling and presentation only.
- The natural-language translator and any LLM only produce expression *text*; it is always re-parsed
  by `Expression.Parse`. Nothing generated is ever executed as code.

## C# conventions
- File-scoped namespaces, `sealed` by default, records for data, nullable enabled, no warnings
  (warnings are errors), XML docs on every public Core member.
- No speculative abstractions: no interface with a single implementation, no options nobody sets.
- Clear names over comments; comments explain *why*, never *what*.

## Tests are the specification
- Never edit, skip, delete or weaken a test to make it pass. If a test seems wrong, stop and escalate
  to the human.
- Tests trace to requirements with `[Trait("Requirement", "Rn")]` and `[Trait("Feature", "Fn")]`.
- xUnit with plain `Assert`. Test the public behaviour, not implementation details.

## Workflow (enforced by `.claude/settings.json` hooks and agent hooks)
- Every product change goes through `/feature <id>`: `test-author` (writes `tests/` only) →
  **human approves the tests** → `implementer` (writes `src/` only, cannot stop while red) →
  `reviewer` (read-only) → `/ship`.
- Git is local until the end: `/ship` = commit on the branch (gate: build + tests) → local squash
  merge into `main` (gate: full verify). `/publish` (human-triggered) pushes once and checks CI.
  Every push asks the human; force-push is denied.
- Before claiming anything is done, run `/verify` and read its output.

## Dependency policy (supply chain)
- Core and CLI have zero third-party dependencies. Every package is declared in
  `Directory.Packages.props`; adding or upgrading one needs explicit human approval and a reason.
- Vulnerable packages fail the build (NuGet audit). Restores are locked (`packages.lock.json`).

## Security
- Validate at trust boundaries (HTTP, stdin, LLM output): size limits, strict grammar parsing.
- Treat LLM output as untrusted input. No dynamic code, no reflection-based evaluation.
- No secrets in the repo; configuration via environment/user-secrets.

## Accountability
- The developer is the sole author of every commit and PR; no AI co-author trailers or footers.
  AI assistance is disclosed in `docs/AI_USAGE.md`, updated per feature.

## Confidentiality
- Never commit confidential documents or private correspondence (keep them in the gitignored
  `private/` folder). Paraphrase requirements; public docs use public facts only.
