# ADR 0005: Dependency and supply-chain policy

- **Status:** Accepted
- **Date:** 2026-11-21

## Context

Every package is code that runs with the application's privileges and can change underneath it. A
calculator needs almost nothing beyond .NET itself; only the service's model and MCP features do.
Package restores must also be reproducible on every platform.

## Decision

- **Minimal dependencies.** The engine and the console app have no third-party runtime dependencies.
  The service uses three packages: `Microsoft.AspNetCore.OpenApi`, `ModelContextProtocol.AspNetCore`
  and `Microsoft.Extensions.AI.OpenAI`. Tests and tooling also use xUnit, `Mvc.Testing`, coverlet and
  Stryker.NET.
- **Central versions and locked restores.** All versions live in `Directory.Packages.props`.
  `verify.sh --full` and CI restore with `dotnet restore --locked-mode`, so the dependency graph must
  match the committed lock files.
- **Pinned toolchain in CI.** `global.json` accepts any .NET 10 SDK, so anyone can build. CI pins the
  exact SDK (10.0.302), because Core's lock file records the SDK-matched `Microsoft.NET.ILLink.Tasks`
  package. GitHub Actions are pinned by commit hash.
- **Vulnerabilities fail the build.** NuGet auditing covers transitive packages, and warnings are
  errors. The audit stopped the very first build because of an advisory for a transitive
  `Microsoft.OpenApi` package, and was resolved by upgrading `Microsoft.AspNetCore.OpenApi`.
- **A human approves every package.** `dotnet add package`, package updates and tool installs require
  approval, and a hook asks before any edit to `Directory.Packages.props`, lock files or
  `global.json`.

## Consequences

### Benefits

- The calculator's attack surface is the .NET runtime itself. The service adds three audited
  packages.

### Trade-offs

- A newly published advisory can stop the build without any code change. This is intended, and the
  fix is a reviewed upgrade. The README explains how to build anyway in that situation.
- Lock files add noise to diffs, and `--locked-mode` needs the advisory feed to be reachable.

## Alternatives considered

| Option | Why not |
| ------ | ------- |
| FluentAssertions | Later major versions need a commercial licence; plain `Assert` is enough |
| Dependabot or Renovate | Bot-authored pull requests conflict with a single, accountable author |
| Floating versions | Restores would not be reproducible |
| Big-decimal or parser packages | The standard library covers the need (see ADR 0003) |
