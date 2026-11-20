---
name: submission-check
description: Final pre-release audit - requirement traceability, required documents, confidentiality, single-author history, clean-clone build, and the release zip. Use once before release.
---
Run every check, report a pass/fail table, and fix nothing silently.

1. **Traceability**: for each requirement R1–R9 in `docs/SPEC.md`, list the tests tagged
   `[Trait("Requirement", "Rn")]` (grep `tests/`) and the docs covering it. Any requirement without
   evidence fails.
2. **Required documents**: `MANUAL.txt` (plain text), `README.md`, `docs/AI_USAGE.md` answering all
   four questions (tools, AI-generated code, refinements, verification), `docs/DESIGN.md` with the
   five sections and <= ~2,000 words (`wc -w`), `docs/DESIGN.pdf`, ADRs.
3. **Confidentiality**: `git ls-files` contains no `*.pdf` except `docs/DESIGN.pdf`, nothing under
   `private/`, no transcript or email text; grep for "CONFIDENTIAL" and personal email addresses.
4. **Authorship**: `git log --format='%an <%ae>' | sort -u` shows exactly one author and
   `git log --format=%B | grep -ci 'co-authored-by'` is 0.
5. **Clean clone**: clone the repo into a temp dir and run `scripts/verify.sh --full` there.
6. **Package**: `git archive --format=zip -o private/expression-calculator.zip HEAD`.
