#!/usr/bin/env bash
# Any change to the dependency manifest needs a human decision (supply-chain / CRA policy).
input="$(cat)"
file="$(jq -r '.tool_input.file_path // empty' <<<"$input")"
case "$file" in
  */Directory.Packages.props|*/packages.lock.json)
    jq -n '{hookSpecificOutput: {hookEventName: "PreToolUse", permissionDecision: "ask",
      permissionDecisionReason: "Dependency change: a human must approve every new or upgraded package (see CLAUDE.md, Dependency policy)."}}' ;;
esac
exit 0
