---
name: ship
description: Commit the current feature/chore branch and squash-merge it into main after the full quality gate passes, keeping the developer as the sole author. Use after /feature, or for docs/chore changes.
argument-hint: "<conventional commit title>"
---
# /ship $ARGUMENTS

Pushing is a separate, human-approved step (`/publish`); this skill never pushes.
Authorship policy: the developer is the only author. Never add `Co-Authored-By` trailers,
"Generated with" footers or any AI attribution (AI use is disclosed in `docs/AI_USAGE.md`).

1. Make sure you are on a feature/chore branch, not `main`.
2. Check `git status` (nothing from `private/`, no PDFs except `docs/DESIGN.pdf`, no secrets), then
   `git add -A` and commit with the title `$ARGUMENTS`. The commit hook runs the quality gate
   (build + tests); fix failures, never bypass it.
3. `git switch main` and `git merge --squash <branch>`.
4. Commit the squashed change on `main` (on `main` the commit hook runs the **full** gate: locked
   restore, build, tests, format, CLI smoke tests) with the title `$ARGUMENTS` and a short body:
   Summary (2–4 bullets), Requirements (SPEC IDs), Tests (counts).
5. Delete the branch (`git branch -D <branch>`). Report the new `main` commit (`git log -1 --stat`).
