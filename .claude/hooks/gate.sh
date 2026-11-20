#!/usr/bin/env bash
# Hook wrapper around scripts/verify.sh. Exit code 2 = block: for PreToolUse it denies the tool call,
# for Stop/SubagentStop it keeps the agent working. stderr is shown to Claude as the reason.
# Usage: gate.sh [--fast|--full|--auto] [--only "<git subcommand>"]
#   --auto               --full on main (the squash-merged result), --fast on feature branches
#   --only "git commit"  gate only when the Bash command really runs that git subcommand
#                        (the settings `if` filter also fires for complex commands it cannot analyse).
mode="${1:---fast}"
input="$(cat)"
if [[ "${2:-}" == "--only" && -n "${3:-}" ]]; then
  cmd="$(jq -r '.tool_input.command // empty' <<<"$input" 2>/dev/null)"
  read -r tool sub <<<"$3"
  grep -qE "(^|[;&|[:space:]])${tool}[[:space:]]+${sub}([[:space:]]|$)" <<<"$cmd" || exit 0
fi

cd "${CLAUDE_PROJECT_DIR:-$(dirname "$0")/../..}" || exit 2
if [[ "$mode" == "--auto" ]]; then
  [[ "$(git branch --show-current)" == "main" ]] && mode="--full" || mode="--fast"
fi
if output="$(scripts/verify.sh "$mode" 2>&1)"; then
  exit 0
fi
{
  echo "Quality gate failed (scripts/verify.sh $mode). Fix the failures below; never weaken or edit tests to pass."
  echo "$output" | grep -E "error|Failed|FAIL|Assert|Expected|Actual" | head -40
  echo "--- last lines ---"
  echo "$output" | tail -15
} >&2
exit 2
