# Architecture decision records

An architecture decision record (ADR) captures one significant design choice: the situation that
called for it, the decision, its consequences and the alternatives that were rejected. Together,
these records explain why the code looks the way it does.

| ADR | Decision | Status |
| --- | -------- | ------ |
| [0001](0001-engine-and-host-separation.md) | A pure calculation engine with thin console and API hosts, and no `ICalculator` interface | Accepted |
| [0002](0002-parse-then-evaluate-with-flat-ast.md) | Parse the whole input before evaluating, using a flat syntax tree whose depth does not grow with input | Accepted |
| [0003](0003-decimal-arithmetic-and-display-policy.md) | `System.Decimal` arithmetic with exact literals and a 25-digit display rule | Accepted |
| [0004](0004-error-model.md) | One located exception type, shown as carets, Problem Details or MCP tool errors | Accepted |
| [0005](0005-dependency-and-supply-chain-policy.md) | No runtime dependencies in the engine and console app; locked, audited, human-approved packages | Accepted |
| [0006](0006-natural-language-and-llm-safety.md) | Rules first, an optional model second; model output is only text that is parsed again | Accepted |
| [0007](0007-agentic-workflow-and-guardrails.md) | AI agents work in enforced areas, tests come first with human approval, and AI use is disclosed | Accepted |

Every record uses the same layout: **Context**, **Decision**, **Consequences** (benefits and
trade-offs) and **Alternatives considered**.
