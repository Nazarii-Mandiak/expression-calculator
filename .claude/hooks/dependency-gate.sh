#!/usr/bin/env bash
# Any change to dependencies, package sources, audit settings or dev tools needs a human decision
# (supply-chain policy). Fails closed: without jq every edit asks.
ask() {
  printf '{"hookSpecificOutput":{"hookEventName":"PreToolUse","permissionDecision":"ask","permissionDecisionReason":"%s"}}\n' "$1"
  exit 0
}
command -v jq >/dev/null || ask "jq is missing, so the dependency gate cannot inspect this edit; please review it."

input="$(cat)"
file="$(jq -r '.tool_input.file_path // empty' <<<"$input")"
case "$file" in
  */Directory.Packages.props|*/packages.lock.json|*/Directory.Build.props|*/nuget.config|*/NuGet.Config|*.csproj|*/dotnet-tools.json|*/global.json)
    ask "Dependency or build-policy change ($(basename "$file")): a human must approve new or upgraded packages, package sources, audit settings, SDK or tools (see CLAUDE.md, Dependency policy)." ;;
esac
exit 0
