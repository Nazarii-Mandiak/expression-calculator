# ADR 0007: An agentic workflow with enforced guardrails

- **Status:** Accepted
- **Date:** 2026-11-21

## Context

The code was written with AI assistance (Claude Code). An instruction in a prompt is only a
suggestion: an agent told not to edit tests might still do so to get a passing build. The workflow
must make risky actions impossible, or at least visible. It must also keep a human in control and
record honestly what the AI did.

## Decision

- **One path for every change.** Each feature goes through `/feature <id>`, and hooks and permissions
  enforce the rules.
- **Separate areas.** `test-author` writes only in `tests/`, `implementer` only in `src/`, and
  `doc-writer` only in the documentation. `reviewer` cannot write at all.
- **Tests first, approved by a human.** The implementer cannot stop while any test fails. After each
  implementation, `git diff -- tests/` must be empty.
- **Gates.** Every commit must build and pass the tests, and the full gate runs before `main`. CI runs
  `verify.sh --full` on Ubuntu and Windows.
- **Independent review.** A reviewer on a stronger model checks every feature against a checklist
  before it is merged. Findings are fixed through the same test-first loop.
- **Human approval for risk.** Package changes always need a human (see ADR 0005). Force-push,
  `git reset --hard` and network tools such as `curl` are denied.
- **Accountability.** There is one human author and no AI trailers in commits. `docs/AI_USAGE.md`
  discloses the tools, the models, what was generated and how it was verified.

## Consequences

### Benefits

- A weakened test, an unreviewed package or a failing commit is blocked or detected, not merely
  discouraged.

### Trade-offs

- Three agents, a human checkpoint and a review per feature are slower than free-form prompting.

### Limits

- Shell commands can bypass the file-path hooks, so a change to locked tests is detected after the
  fact rather than prevented.
- The coordinating session is not restricted to one area.
- Branch protection on the remote repository is not configured yet.

### Next steps

- Make `tests/` read-only during implementation.
- Require CI and a review on the remote `main` branch.
- Run mutation testing in CI.

## Alternatives considered

| Option | Why not |
| ------ | ------- |
| Free-form prompting in one session | Rules would depend on the model following instructions |
| Rules written only in prompts | Prompts can be ignored; hooks cannot |
| No AI assistance | Slower delivery, and no opportunity to show disciplined AI use |
