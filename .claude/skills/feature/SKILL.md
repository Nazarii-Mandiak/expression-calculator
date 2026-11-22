---
name: feature
description: Deliver one docs/SPEC.md feature end to end with test-first separation of duties - test-author writes failing tests, the human approves them, implementer makes them pass, reviewer reviews, then /ship squash-merges it into main. Use for every product change (F1..F9).
argument-hint: "<feature-id, e.g. F1>"
---
# /feature $ARGUMENTS

Orchestrate feature **$ARGUMENTS** from `docs/SPEC.md`. You are the orchestrator: delegate code and
tests to the agents, do not write them yourself.

1. **Start clean**: `git switch main`, working tree clean, then
   `git switch -c feat/<id>-<short-slug>`.
2. **Contract check**: the public types/members the feature needs exist in `src/` as documented
   stubs. If not, add the signatures (bodies `throw new NotImplementedException()`) and get the
   human's OK before continuing; contracts are human decisions.
3. **Tests first**: delegate to the `test-author` agent with the feature ID, the SPEC section and
   the contract file paths. Then run `dotnet test` yourself and confirm the new tests fail for the
   right reason.
4. **Human checkpoint (mandatory)**: show the test names grouped by behaviour plus the file links,
   and wait for approval or changes. Loop back to step 3 for changes.
5. **Lock the tests**: `git add tests/` (the index is now the reference copy).
6. **Implement**: delegate to the `implementer` agent with the feature ID, SPEC section, contract
   and the list of locked test files.
7. **Audit the lock**: `git diff --quiet -- tests/` must succeed and
   `git ls-files --others --exclude-standard tests/` must be empty. If not, the implementer touched
   the tests (e.g. via Bash, which hooks cannot see): revert those changes and re-run step 6.
8. **Verify**: `scripts/verify.sh --full`.
9. **Review**: delegate to the `reviewer` agent with the feature ID and the diff against `main`
   (committed + staged). Triage every finding: accept (route to implementer for code, test-author
   for test gaps; new tests are reported to the human) or reject with a one-line reason.
   Re-verify after fixes.
10. **Record**: update `docs/AI_USAGE.md` with what was generated, the human decisions and
    refinements, and the verification evidence (test counts).
11. **Ship**: run `/ship` (squash merge into `main`) with a conventional commit title such as `feat(core): parse expressions`.
