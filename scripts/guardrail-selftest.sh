#!/usr/bin/env bash
# Proves the Claude Code guardrail hooks do what CLAUDE.md claims, using simulated hook inputs.
# (The agent-level behaviour is additionally exercised live; results are recorded in docs/AI_USAGE.md.)
set -uo pipefail
cd "$(dirname "$0")/.."
export CLAUDE_PROJECT_DIR="$PWD"
H=.claude/hooks
pass=0; fail=0
check() { # check <name> <expected> <actual>
  if [[ "$2" == "$3" ]]; then echo "  PASS  $1"; pass=$((pass+1)); else echo "  FAIL  $1 (expected '$2', got '$3')"; fail=$((fail+1)); fi
}
edit() { jq -n --arg a "$1" --arg f "$PWD/$2" '{agent_type:$a, tool_input:{file_path:$f}}'; }
bash_cmd() { jq -n --arg c "$1" '{tool_input:{command:$c}}'; }
decision() { # no hook output means the hook did not object
  if [[ -z "$1" ]]; then echo allow; else jq -r '.hookSpecificOutput.permissionDecision // "allow"' <<<"$1"; fi
}

echo "Write lanes (path-guard.sh)"
check "implementer cannot edit tests/"            deny  "$(decision "$(edit implementer tests/Calculator.Core.Tests/X.cs | $H/path-guard.sh src/)")"
check "implementer can edit src/"                 allow "$(decision "$(edit implementer src/Calculator.Core/Lexer.cs | $H/path-guard.sh src/)")"
check "implementer cannot weaken build gates"     deny  "$(decision "$(edit implementer Directory.Build.props | $H/path-guard.sh src/)")"
check "implementer cannot edit guardrails"        deny  "$(decision "$(edit implementer .claude/settings.json | $H/path-guard.sh src/)")"
check "test-author cannot edit src/"              deny  "$(decision "$(edit test-author src/Calculator.Core/Expression.cs | $H/path-guard.sh tests/)")"
check "test-author can edit tests/"               allow "$(decision "$(edit test-author tests/Calculator.Core.Tests/X.cs | $H/path-guard.sh tests/)")"
check "doc-writer cannot edit src/"               deny  "$(decision "$(edit doc-writer src/Calculator.Cli/Program.cs | $H/path-guard.sh docs/ README.md MANUAL.txt)")"

echo "Dependency gate (dependency-gate.sh)"
check "editing Directory.Packages.props asks a human" ask   "$(decision "$(edit "" Directory.Packages.props | $H/dependency-gate.sh)")"
check "editing other files is unaffected"             allow "$(decision "$(edit "" src/Calculator.Core/Lexer.cs | $H/dependency-gate.sh)")"

echo "Commit gate (gate.sh)"
bash_cmd "ls -la" | $H/gate.sh --fast --only "git commit" >/dev/null 2>&1
check "non-commit commands are not gated"         0 "$?"
probe=tests/Calculator.Core.Tests/GuardrailProbe.cs
printf 'namespace Calculator.Core.Tests;\n\npublic class GuardrailProbe\n{\n    [Fact]\n    public void Fails() => Assert.Equal(1, 2);\n}\n' > "$probe"
bash_cmd "git commit -m probe" | $H/gate.sh --fast --only "git commit" >/dev/null 2>&1
check "commit is blocked while a test fails"      2 "$?"
rm -f "$probe"
bash_cmd "git commit -m probe" | $H/gate.sh --fast --only "git commit" >/dev/null 2>&1
check "commit is allowed when green"              0 "$?"

echo; echo "$pass passed, $fail failed"
[[ $fail == 0 ]]
