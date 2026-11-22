# expression-calculator — instructions for Claude Code

.NET 10 expression calculator: pure engine `Calculator.Core`, console host `Calculator.Cli` (`calc`),
service `Calculator.Api` (REST + MCP). Spec: `docs/SPEC.md`. Decisions: `docs/adr/`.

## Commands
- Gate: `scripts/verify.sh --fast` (build + tests) · `--full` (CI gate incl. format and smoke tests)
- CLI: `dotnet run --project src/Calculator.Cli -- "10 - 2 * 6 / 4"` · API: `dotnet run --project src/Calculator.Api`

## Rules
- **Core is pure**: no I/O, no `Console`, no packages, no mutable statics, `InvariantCulture`, AOT-safe.
- Parse the whole input before evaluating; errors are `CalculationException` (code, position, length).
- Hosts are thin adapters. NL/LLM output is only expression text and is always re-parsed — never executed.
- C#: file-scoped namespaces, `sealed` by default, records for data, nullable on, no warnings, XML docs
  on public Core members. No speculative abstractions. Comments explain *why*.
- **Tests are the specification**: never edit, skip or weaken a test to make it pass — escalate.
  Tag tests with `[Trait("Requirement","Rn")]` and `[Trait("Feature","Fn")]`; plain xUnit `Assert`.
- **Dependencies**: every package lives in `Directory.Packages.props` and needs human approval;
  Core and CLI have none. Vulnerable packages fail the build; restores are locked.
- **Security**: validate at trust boundaries (size limits, strict parsing); LLM output is untrusted;
  no secrets in the repo.
- **Accountability**: the developer is the sole commit author; AI use is disclosed in `docs/AI_USAGE.md`.
- **Confidentiality**: never commit confidential documents or private correspondence (`private/` is ignored).

## Workflow (enforced by hooks)
`/feature <id>`: `test-author` (writes `tests/` only) → **human approves the tests** → `implementer`
(writes `src/` only, cannot stop while red) → `reviewer` (read-only) → `/ship` (commit with the gate,
squash-merge into `main`). Run `/verify` before claiming anything is done. Pushes need human approval;
force-push is denied.
