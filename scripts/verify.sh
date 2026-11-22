#!/usr/bin/env bash
# Single quality gate used by Claude Code hooks, the /verify skill and CI.
#   --fast  build + tests                      (hooks: implementer stop, git commit)
#   --full  locked restore, build, tests with coverage, format check, CLI smoke tests (CI, /ship)
set -euo pipefail
cd "$(dirname "$0")/.."

mode="${1:---full}"
case "$mode" in --fast|--full) ;; *) echo "usage: scripts/verify.sh [--fast|--full]" >&2; exit 2 ;; esac
export DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1
step() { printf '\n==> %s\n' "$1"; }

if [[ "$mode" == "--full" ]]; then
  step "restore (locked mode: dependency graph must match committed lock files)"
  dotnet restore --locked-mode
fi

step "build (warnings are errors; analyzers + code style enforced)"
dotnet build -c Release --no-restore -nologo -clp:ErrorsOnly

step "test"
if [[ "$mode" == "--full" ]]; then
  dotnet test -c Release --no-build --nologo --collect:"XPlat Code Coverage" --results-directory artifacts/coverage
else
  dotnet test -c Release --no-build --nologo
fi

[[ "$mode" == "--fast" ]] && { echo; echo "verify --fast: OK"; exit 0; }

step "format (must already be formatted)"
dotnet format Calculator.slnx --verify-no-changes --no-restore

step "CLI smoke tests (real binary, golden outputs)"
# Windows writes CRLF; strip CR so comparisons are identical on every OS.
calc() { dotnet run --project src/Calculator.Cli -c Release --no-build -- "$@" | tr -d '\r'; }
{
  fail=0
  expect() { # expect <expected-stdout> <args...>
    local want="$1"; shift
    local got; got="$(calc "$@" 2>/dev/null || true)"
    if [[ "$got" == "$want" ]]; then echo "  ok   calc $*  ->  $got"
    else echo "  FAIL calc $*  ->  '$got' (expected '$want')"; fail=1; fi
  }
  expect "7"   "10-2*6/4"
  expect "7"   "10 - 2 * 6 / 4"
  expect "0.3" "0.1 + 0.2"
  expect "1"   "1/3*3"
  expect "2.5" "10/4"
  expect "7"   "What is 10 minus 2 multiplied by 6 divided by 4?"
  expect "calc 1.0.0" --version
  batch_out="$(printf '1+2\n# comment\n10/0\n' | dotnet run --project src/Calculator.Cli -c Release --no-build 2>/dev/null | tr -d '\r'; exit "${PIPESTATUS[1]}")" && batch_rc=0 || batch_rc=$?
  if [[ "$batch_out" == "3" && "$batch_rc" == "1" ]]; then echo "  ok   batch: stdout=3, exit=1"
  else echo "  FAIL batch: stdout='$batch_out', exit=$batch_rc (expected 3 / 1)"; fail=1; fi
  [[ $fail == 0 ]] || exit 1
}

echo; echo "verify --full: OK"
