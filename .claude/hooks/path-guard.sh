#!/usr/bin/env bash
# Agent-scoped write lanes. Usage (from agent frontmatter): path-guard.sh <allowed-prefix>...
# Denies Edit/Write to any path outside the allowed prefixes (paths relative to the project root).
input="$(cat)"
file="$(jq -r '.tool_input.file_path // .tool_input.notebook_path // empty' <<<"$input")"
agent="$(jq -r '.agent_type // "main session"' <<<"$input")"
[[ -z "$file" ]] && exit 0

root="${CLAUDE_PROJECT_DIR:-$PWD}"
rel="${file#"$root"/}"
for prefix in "$@"; do
  [[ "$rel" == "$prefix"* ]] && exit 0
done

jq -n --arg reason "Guardrail: agent '$agent' may only write to: $* (attempted: $rel). Report back to the orchestrator instead of working around this." \
  '{hookSpecificOutput: {hookEventName: "PreToolUse", permissionDecision: "deny", permissionDecisionReason: $reason}}'
