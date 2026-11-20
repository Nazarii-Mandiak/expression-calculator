---
name: verify
description: Run the project quality gate (build with warnings as errors, tests, format check, locked restore, CLI smoke tests) and summarise the result. Use before claiming any work is done.
argument-hint: "[--fast|--full]"
allowed-tools: Bash(scripts/verify.sh *)
---
Run `scripts/verify.sh ${ARGUMENTS:---full}` and report: pass/fail per step, test counts, and for
failures the exact error lines. Never report success without seeing `verify ... OK` in the output.
